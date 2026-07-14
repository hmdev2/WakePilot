#!/data/data/com.termux/files/usr/bin/python
"""Remote Wake bridge v1 forced-command wrapper. Standard library only."""

from __future__ import annotations

import argparse
import base64
import hashlib
import ipaddress
import json
import os
import re
import socket
import sys
import tempfile
import time
import uuid
from contextlib import contextmanager
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Callable, Iterator

VERSION = 1
MAX_PAYLOAD_BYTES = 4096
ENVELOPE_PREFIX = "rwa1:"
REQUEST_FIELDS = {"v", "requestId", "targetId", "issuedAt", "nonce", "action"}
CONFIG_FIELDS = {"v", "targets"}
TARGET_FIELDS = {"mac", "broadcast", "port"}
UUID_ZERO = "00000000-0000-0000-0000-000000000000"
NONCE_PATTERN = re.compile(r"^[A-Za-z0-9_-]{43}$")
UTC_PATTERN = re.compile(r"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?Z$")


class BridgeError(Exception):
    def __init__(self, code: str, exit_code: int, message: str, request_id: str = UUID_ZERO) -> None:
        super().__init__(message)
        self.code = code
        self.exit_code = exit_code
        self.request_id = request_id


@dataclass(frozen=True)
class WakeTarget:
    mac: bytes
    broadcast: str
    port: int


@dataclass(frozen=True)
class BridgeRequest:
    request_id: str
    target_id: str
    issued_at: datetime
    nonce: str
    action: str


def _closed_object(pairs: list[tuple[str, object]]) -> dict[str, object]:
    result: dict[str, object] = {}
    for key, value in pairs:
        if key in result:
            raise BridgeError("ERR012", 10, "duplicate JSON field")
        result[key] = value
    return result


def _canonical_uuid(value: object, field: str, request_id: str = UUID_ZERO) -> str:
    if not isinstance(value, str):
        raise BridgeError("ERR012", 10, f"invalid {field}", request_id)
    try:
        parsed = uuid.UUID(value)
    except ValueError as exception:
        raise BridgeError("ERR012", 10, f"invalid {field}", request_id) from exception
    if parsed.int == 0 or str(parsed) != value:
        raise BridgeError("ERR012", 10, f"non-canonical {field}", request_id)
    return value


def _parse_utc(value: object, request_id: str) -> datetime:
    if not isinstance(value, str) or not UTC_PATTERN.fullmatch(value):
        raise BridgeError("ERR011", 11, "invalid issuedAt", request_id)
    normalized = value[:-1]
    if "." in normalized:
        prefix, fraction = normalized.split(".", 1)
        normalized = f"{prefix}.{fraction[:6]}"
    try:
        return datetime.fromisoformat(normalized).replace(tzinfo=timezone.utc)
    except ValueError as exception:
        raise BridgeError("ERR011", 11, "invalid issuedAt", request_id) from exception


def decode_request(envelope: str, now: datetime) -> BridgeRequest:
    if not isinstance(envelope, str) or not envelope.startswith(ENVELOPE_PREFIX):
        raise BridgeError("ERR012", 10, "invalid envelope")
    encoded = envelope[len(ENVELOPE_PREFIX) :]
    if not encoded or len(encoded) > 5462 or re.search(r"[^A-Za-z0-9_-]", encoded):
        raise BridgeError("ERR012", 10, "invalid base64url")

    try:
        padded = encoded + ("=" * ((4 - len(encoded) % 4) % 4))
        payload = base64.b64decode(padded, altchars=b"-_", validate=True)
    except (ValueError, base64.binascii.Error) as exception:
        raise BridgeError("ERR012", 10, "invalid base64url") from exception

    canonical = base64.urlsafe_b64encode(payload).rstrip(b"=").decode("ascii")
    if canonical != encoded or not payload or len(payload) > MAX_PAYLOAD_BYTES:
        raise BridgeError("ERR012", 10, "invalid payload size or encoding")

    try:
        document = json.loads(payload.decode("utf-8"), object_pairs_hook=_closed_object)
    except BridgeError:
        raise
    except (UnicodeDecodeError, json.JSONDecodeError) as exception:
        raise BridgeError("ERR012", 10, "invalid JSON") from exception

    if not isinstance(document, dict):
        raise BridgeError("ERR012", 10, "request must be an object")

    raw_request_id = document.get("requestId")
    request_id = UUID_ZERO
    if isinstance(raw_request_id, str):
        try:
            request_id = _canonical_uuid(raw_request_id, "requestId")
        except BridgeError:
            pass

    if set(document) != REQUEST_FIELDS:
        raise BridgeError("ERR012", 10, "unknown or missing request field", request_id)
    if type(document["v"]) is not int or document["v"] != VERSION:
        raise BridgeError("ERR012", 10, "unsupported protocol version", request_id)

    request_id = _canonical_uuid(document["requestId"], "requestId")
    target_id = _canonical_uuid(document["targetId"], "targetId", request_id)
    issued_at = _parse_utc(document["issuedAt"], request_id)
    if abs((now - issued_at).total_seconds()) > 60:
        raise BridgeError("ERR011", 11, "request timestamp is outside the allowed window", request_id)

    nonce = document["nonce"]
    if not isinstance(nonce, str) or not NONCE_PATTERN.fullmatch(nonce):
        raise BridgeError("ERR011", 11, "invalid nonce", request_id)
    action = document["action"]
    if action not in ("health", "wake"):
        raise BridgeError("ERR012", 12, "unknown action", request_id)

    return BridgeRequest(request_id, target_id, issued_at, nonce, action)


def _parse_mac(value: object) -> bytes:
    if not isinstance(value, str) or not re.fullmatch(r"(?:[0-9A-F]{2}:){5}[0-9A-F]{2}", value):
        raise BridgeError("ERR018", 21, "invalid target MAC")
    return bytes.fromhex(value.replace(":", ""))


def load_targets(config_path: Path) -> dict[str, WakeTarget]:
    try:
        if config_path.stat().st_size > 16_384:
            raise BridgeError("ERR018", 21, "configuration is too large")
        document = json.loads(config_path.read_text(encoding="utf-8"), object_pairs_hook=_closed_object)
    except BridgeError:
        raise
    except (OSError, UnicodeDecodeError, json.JSONDecodeError) as exception:
        raise BridgeError("ERR018", 21, "configuration cannot be read") from exception
    if (
        not isinstance(document, dict)
        or set(document) != CONFIG_FIELDS
        or type(document["v"]) is not int
        or document["v"] != VERSION
    ):
        raise BridgeError("ERR018", 21, "invalid configuration schema")
    raw_targets = document["targets"]
    if not isinstance(raw_targets, dict) or len(raw_targets) > 32:
        raise BridgeError("ERR018", 21, "invalid target allowlist")

    targets: dict[str, WakeTarget] = {}
    for target_id, raw_target in raw_targets.items():
        canonical_id = _canonical_uuid(target_id, "targetId")
        if not isinstance(raw_target, dict) or set(raw_target) != TARGET_FIELDS:
            raise BridgeError("ERR018", 21, "invalid target schema")
        broadcast = raw_target["broadcast"]
        try:
            parsed_address = ipaddress.ip_address(broadcast)
        except ValueError as exception:
            raise BridgeError("ERR018", 21, "invalid broadcast address") from exception
        port = raw_target["port"]
        if parsed_address.version != 4 or type(port) is not int or not 1 <= port <= 65535:
            raise BridgeError("ERR018", 21, "invalid broadcast endpoint")
        targets[canonical_id] = WakeTarget(_parse_mac(raw_target["mac"]), str(parsed_address), port)
    return targets


@contextmanager
def _exclusive_lock(lock_path: Path, timeout_seconds: float = 2.0) -> Iterator[None]:
    deadline = time.monotonic() + timeout_seconds
    while True:
        try:
            descriptor = os.open(lock_path, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
            os.close(descriptor)
            break
        except FileExistsError:
            try:
                if time.time() - lock_path.stat().st_mtime > 30:
                    lock_path.unlink()
                    continue
            except FileNotFoundError:
                continue
            if time.monotonic() >= deadline:
                raise BridgeError("ERR012", 21, "state lock is unavailable")
            time.sleep(0.02)
    try:
        yield
    finally:
        try:
            lock_path.unlink()
        except FileNotFoundError:
            pass


class RequestPolicy:
    def __init__(self, state_path: Path) -> None:
        self._state_path = state_path

    def reserve(self, request: BridgeRequest, key_id: str, now: datetime) -> None:
        _canonical_uuid(key_id, "keyId", request.request_id)
        now_epoch = now.timestamp()
        with _exclusive_lock(Path(f"{self._state_path}.lock")):
            state = self._read_state()
            nonces = [entry for entry in state["nonces"] if now_epoch - entry["seenAt"] <= 600]
            nonce_hash = hashlib.sha256(request.nonce.encode("ascii")).hexdigest()
            if any(entry["hash"] == nonce_hash for entry in nonces):
                raise BridgeError("ERR011", 11, "nonce replayed", request.request_id)
            nonces.append({"hash": nonce_hash, "seenAt": now_epoch})
            state["nonces"] = nonces

            if request.action == "wake":
                rate_key = f"{key_id}|{request.target_id}"
                events = [value for value in state["wakes"].get(rate_key, []) if now_epoch - value <= 300]
                policy_error = None
                if events and now_epoch - events[-1] < 15:
                    policy_error = BridgeError("ERR011", 13, "wake cooldown active", request.request_id)
                elif len(events) >= 3:
                    policy_error = BridgeError("ERR011", 13, "wake rate limit exceeded", request.request_id)
                else:
                    events.append(now_epoch)
                state["wakes"][rate_key] = events

            self._write_state(state)
            if request.action == "wake" and policy_error is not None:
                raise policy_error

    def _read_state(self) -> dict[str, object]:
        try:
            document = json.loads(self._state_path.read_text(encoding="utf-8"))
        except FileNotFoundError:
            return {"v": VERSION, "nonces": [], "wakes": {}}
        except (OSError, UnicodeDecodeError, json.JSONDecodeError) as exception:
            raise BridgeError("ERR012", 21, "state cannot be read") from exception
        if (
            not isinstance(document, dict)
            or set(document) != {"v", "nonces", "wakes"}
            or document["v"] != VERSION
            or not isinstance(document["nonces"], list)
            or not isinstance(document["wakes"], dict)
        ):
            raise BridgeError("ERR012", 21, "invalid state schema")
        return document

    def _write_state(self, state: dict[str, object]) -> None:
        self._state_path.parent.mkdir(parents=True, exist_ok=True, mode=0o700)
        descriptor, temporary_name = tempfile.mkstemp(prefix=".state-", dir=self._state_path.parent)
        try:
            with os.fdopen(descriptor, "w", encoding="utf-8") as stream:
                json.dump(state, stream, separators=(",", ":"), sort_keys=True)
                stream.flush()
                os.fsync(stream.fileno())
            os.chmod(temporary_name, 0o600)
            os.replace(temporary_name, self._state_path)
        finally:
            try:
                os.unlink(temporary_name)
            except FileNotFoundError:
                pass


class MagicPacketSender:
    def __init__(
        self,
        socket_factory: Callable[..., socket.socket] = socket.socket,
        sleeper: Callable[[float], None] = time.sleep,
    ) -> None:
        self._socket_factory = socket_factory
        self._sleeper = sleeper

    @staticmethod
    def build(mac: bytes) -> bytes:
        if len(mac) != 6:
            raise ValueError("MAC must contain six bytes")
        return (b"\xff" * 6) + (mac * 16)

    def send_burst(self, target: WakeTarget) -> int:
        packet = self.build(target.mac)
        with self._socket_factory(socket.AF_INET, socket.SOCK_DGRAM) as datagram:
            datagram.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)
            for index in range(3):
                sent = datagram.sendto(packet, (target.broadcast, target.port))
                if sent != len(packet):
                    raise OSError("partial datagram")
                if index < 2:
                    self._sleeper(0.25)
        return 3


def _server_time(now: datetime) -> str:
    utc = now.astimezone(timezone.utc)
    return utc.strftime("%Y-%m-%dT%H:%M:%S.") + f"{utc.microsecond:06d}0Z"


def response(request_id: str, status: str, code: str, packet_count: int, now: datetime) -> str:
    return json.dumps(
        {
            "v": VERSION,
            "requestId": request_id,
            "status": status,
            "code": code,
            "packetCount": packet_count,
            "serverTime": _server_time(now),
        },
        separators=(",", ":"),
    )


def handle(
    envelope: str,
    key_id: str,
    config_path: Path,
    state_path: Path,
    now: datetime,
    sender: MagicPacketSender,
) -> tuple[str, int]:
    try:
        request = decode_request(envelope, now)
        targets = load_targets(config_path)
        target = targets.get(request.target_id)
        if target is None:
            raise BridgeError("ERR012", 12, "target is not allowlisted", request.request_id)
        RequestPolicy(state_path).reserve(request, key_id, now)
        packet_count = sender.send_burst(target) if request.action == "wake" else 0
        return response(request.request_id, "accepted", "OK", packet_count, now), 0
    except BridgeError as exception:
        return response(exception.request_id, "rejected", exception.code, 0, now), exception.exit_code
    except OSError:
        request_id = request.request_id if "request" in locals() else UUID_ZERO
        return response(request_id, "error", "ERR012", 0, now), 20
    except Exception:
        request_id = request.request_id if "request" in locals() else UUID_ZERO
        return response(request_id, "error", "ERR020", 0, now), 21


def main() -> int:
    default_root = Path.home() / ".remote-wake"
    parser = argparse.ArgumentParser(add_help=False)
    parser.add_argument("--key-id", required=True)
    parser.add_argument("--config", type=Path, default=default_root / "config.json")
    parser.add_argument("--state", type=Path, default=default_root / "state.json")
    arguments = parser.parse_args()
    now = datetime.now(timezone.utc)
    output, exit_code = handle(
        os.environ.get("SSH_ORIGINAL_COMMAND", ""),
        arguments.key_id,
        arguments.config,
        arguments.state,
        now,
        MagicPacketSender(),
    )
    sys.stdout.write(output + "\n")
    return exit_code


if __name__ == "__main__":
    raise SystemExit(main())
