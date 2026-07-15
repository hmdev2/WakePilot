#!/data/data/com.termux/files/usr/bin/python
"""Atomically add or update one allowlisted wake target."""

from __future__ import annotations

import argparse
import ipaddress
import json
import os
import tempfile
import uuid
from pathlib import Path

from rwa_bridge import BridgeError, WakeTarget, load_targets


def _canonical_target_id(value: str) -> str:
    parsed = uuid.UUID(value)
    if parsed.int == 0 or str(parsed) != value:
        raise ValueError("target ID must be a canonical non-empty UUID")
    return value


def _target(mac: str, broadcast: str, port: int) -> WakeTarget:
    if len(mac) != 17 or any(
        character not in "0123456789ABCDEF:" for character in mac
    ):
        raise ValueError("target MAC is invalid")
    parts = mac.split(":")
    if len(parts) != 6 or any(len(part) != 2 for part in parts):
        raise ValueError("target MAC is invalid")
    address = ipaddress.IPv4Address(broadcast)
    if str(address) != broadcast:
        raise ValueError("target broadcast is not canonical")
    if type(port) is not int or port < 1 or port > 65535:
        raise ValueError("target port is invalid")
    return WakeTarget(bytes.fromhex(mac.replace(":", "")), broadcast, port)


def _serialize(targets: dict[str, WakeTarget]) -> bytes:
    document = {
        "v": 1,
        "targets": {
            target_id: {
                "mac": target.mac.hex(":").upper(),
                "broadcast": target.broadcast,
                "port": target.port,
            }
            for target_id, target in sorted(targets.items())
        },
    }
    return (json.dumps(document, separators=(",", ":")) + "\n").encode("utf-8")


def configure_target(
    config_path: Path,
    target_id: str,
    mac: str,
    broadcast: str,
    port: int,
) -> None:
    canonical_id = _canonical_target_id(target_id)
    target = _target(mac, broadcast, port)
    targets = load_targets(config_path) if config_path.exists() else {}
    if canonical_id not in targets and len(targets) >= 32:
        raise ValueError("target limit was reached")
    targets[canonical_id] = target

    config_path.parent.mkdir(parents=True, exist_ok=True, mode=0o700)
    descriptor, temporary_name = tempfile.mkstemp(prefix=".config-", dir=config_path.parent)
    try:
        with os.fdopen(descriptor, "wb") as stream:
            stream.write(_serialize(targets))
            stream.flush()
            os.fsync(stream.fileno())
        os.chmod(temporary_name, 0o600)
        os.replace(temporary_name, config_path)
    finally:
        try:
            os.unlink(temporary_name)
        except FileNotFoundError:
            pass


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--config", required=True, type=Path)
    parser.add_argument("--target-id", required=True)
    parser.add_argument("--mac", required=True)
    parser.add_argument("--broadcast", required=True)
    parser.add_argument("--port", required=True, type=int)
    arguments = parser.parse_args()
    try:
        configure_target(
            arguments.config,
            arguments.target_id,
            arguments.mac,
            arguments.broadcast,
            arguments.port,
        )
    except (BridgeError, OSError, ValueError):
        print("A configuração do target é inválida ou não pôde ser gravada.", file=os.sys.stderr)
        return 21
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
