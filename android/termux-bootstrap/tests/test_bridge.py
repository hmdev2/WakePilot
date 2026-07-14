from __future__ import annotations

import base64
import json
import sys
import tempfile
import unittest
import uuid
from datetime import datetime, timedelta, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))

from install_key import build_authorized_entry  # noqa: E402
from rwa_bridge import (  # noqa: E402
    BridgeError,
    BridgeRequest,
    MagicPacketSender,
    RequestPolicy,
    WakeTarget,
    decode_request,
    handle,
)


REQUEST_ID = "11111111-1111-4111-8111-111111111111"
TARGET_ID = "22222222-2222-4222-8222-222222222222"
KEY_ID = "33333333-3333-4333-8333-333333333333"
NONCE = "A" * 43
NOW = datetime(2026, 7, 14, 12, 0, 0, tzinfo=timezone.utc)


def envelope(document: dict[str, object] | str) -> str:
    raw = document if isinstance(document, str) else json.dumps(document, separators=(",", ":"))
    return "rwa1:" + base64.urlsafe_b64encode(raw.encode("utf-8")).rstrip(b"=").decode("ascii")


def request_document(**changes: object) -> dict[str, object]:
    document: dict[str, object] = {
        "v": 1,
        "requestId": REQUEST_ID,
        "targetId": TARGET_ID,
        "issuedAt": "2026-07-14T12:00:00.0000000Z",
        "nonce": NONCE,
        "action": "wake",
    }
    document.update(changes)
    return document


class FakeSocket:
    def __init__(self) -> None:
        self.options: list[tuple[int, int, int]] = []
        self.datagrams: list[tuple[bytes, tuple[str, int]]] = []

    def __enter__(self) -> "FakeSocket":
        return self

    def __exit__(self, *args: object) -> None:
        return None

    def setsockopt(self, level: int, option: int, value: int) -> None:
        self.options.append((level, option, value))

    def sendto(self, packet: bytes, endpoint: tuple[str, int]) -> int:
        self.datagrams.append((packet, endpoint))
        return len(packet)


class BridgeProtocolTests(unittest.TestCase):
    def test_decodes_closed_request(self) -> None:
        decoded = decode_request(envelope(request_document()), NOW)
        self.assertEqual(REQUEST_ID, decoded.request_id)
        self.assertEqual(TARGET_ID, decoded.target_id)
        self.assertEqual("wake", decoded.action)

    def test_rejects_unknown_and_duplicate_fields(self) -> None:
        with self.assertRaises(BridgeError):
            decode_request(envelope(request_document(extra=True)), NOW)
        duplicate = (
            '{"v":1,"v":1,"requestId":"' + REQUEST_ID + '","targetId":"' + TARGET_ID
            + '","issuedAt":"2026-07-14T12:00:00.0000000Z","nonce":"' + NONCE
            + '","action":"wake"}'
        )
        with self.assertRaises(BridgeError):
            decode_request(envelope(duplicate), NOW)

    def test_rejects_expired_version_action_nonce_and_oversize(self) -> None:
        invalid = [
            request_document(v=2),
            request_document(action="shell"),
            request_document(nonce="short"),
            request_document(issuedAt="2026-07-14T11:58:59.0000000Z"),
        ]
        for document in invalid:
            with self.subTest(document=document), self.assertRaises(BridgeError):
                decode_request(envelope(document), NOW)
        with self.assertRaises(BridgeError):
            decode_request("rwa1:" + ("A" * 5463), NOW)


class MagicPacketTests(unittest.TestCase):
    def test_builds_standard_packet(self) -> None:
        mac = bytes.fromhex("001122334455")
        packet = MagicPacketSender.build(mac)
        self.assertEqual(102, len(packet))
        self.assertEqual(b"\xff" * 6, packet[:6])
        self.assertEqual(mac * 16, packet[6:])

    def test_sends_three_packets_250ms_apart_without_real_network(self) -> None:
        fake = FakeSocket()
        delays: list[float] = []
        sender = MagicPacketSender(lambda *_: fake, delays.append)
        count = sender.send_burst(WakeTarget(bytes.fromhex("001122334455"), "192.0.2.255", 9))
        self.assertEqual(3, count)
        self.assertEqual(3, len(fake.datagrams))
        self.assertEqual([0.25, 0.25], delays)


class RequestPolicyTests(unittest.TestCase):
    def test_replay_cooldown_and_rate_limit_are_persistent(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            state = Path(directory) / "state.json"
            policy = RequestPolicy(state)

            first = BridgeRequest(REQUEST_ID, TARGET_ID, NOW, NONCE, "wake")
            policy.reserve(first, KEY_ID, NOW)
            with self.assertRaises(BridgeError) as replay:
                policy.reserve(first, KEY_ID, NOW + timedelta(seconds=1))
            self.assertEqual(11, replay.exception.exit_code)

            cooldown_request = BridgeRequest(
                str(uuid.uuid4()), TARGET_ID, NOW, "B" * 43, "wake"
            )
            with self.assertRaises(BridgeError) as cooldown:
                policy.reserve(cooldown_request, KEY_ID, NOW + timedelta(seconds=14))
            self.assertEqual(13, cooldown.exception.exit_code)
            with self.assertRaises(BridgeError):
                policy.reserve(cooldown_request, KEY_ID, NOW + timedelta(seconds=15))

            for offset, nonce in ((15, "C" * 43), (30, "D" * 43)):
                policy.reserve(
                    BridgeRequest(str(uuid.uuid4()), TARGET_ID, NOW, nonce, "wake"),
                    KEY_ID,
                    NOW + timedelta(seconds=offset),
                )
            with self.assertRaises(BridgeError) as rate:
                policy.reserve(
                    BridgeRequest(str(uuid.uuid4()), TARGET_ID, NOW, "E" * 43, "wake"),
                    KEY_ID,
                    NOW + timedelta(seconds=45),
                )
            self.assertEqual(13, rate.exception.exit_code)


class EndToEndWrapperTests(unittest.TestCase):
    def test_health_and_wake_return_strict_receipts(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            config = root / "config.json"
            state = root / "state.json"
            config.write_text(
                json.dumps(
                    {
                        "v": 1,
                        "targets": {
                            TARGET_ID: {"mac": "00:11:22:33:44:55", "broadcast": "192.0.2.255", "port": 9}
                        },
                    }
                ),
                encoding="utf-8",
            )
            fake = FakeSocket()
            sender = MagicPacketSender(lambda *_: fake, lambda _: None)

            health = request_document(action="health")
            output, exit_code = handle(envelope(health), KEY_ID, config, state, NOW, sender)
            receipt = json.loads(output)
            self.assertEqual(0, exit_code)
            self.assertEqual({"v", "requestId", "status", "code", "packetCount", "serverTime"}, set(receipt))
            self.assertEqual(0, receipt["packetCount"])

            wake = request_document(requestId=str(uuid.uuid4()), nonce="F" * 43)
            output, exit_code = handle(envelope(wake), KEY_ID, config, state, NOW, sender)
            receipt = json.loads(output)
            self.assertEqual(0, exit_code)
            self.assertEqual("accepted", receipt["status"])
            self.assertEqual(3, receipt["packetCount"])
            self.assertEqual(3, len(fake.datagrams))

    def test_non_allowlisted_target_never_sends(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            config = root / "config.json"
            config.write_text('{"v":1,"targets":{}}', encoding="utf-8")
            fake = FakeSocket()
            output, exit_code = handle(
                envelope(request_document()),
                KEY_ID,
                config,
                root / "state.json",
                NOW,
                MagicPacketSender(lambda *_: fake, lambda _: None),
            )
            self.assertEqual(12, exit_code)
            self.assertEqual("rejected", json.loads(output)["status"])
            self.assertEqual([], fake.datagrams)


class AuthorizedKeyTests(unittest.TestCase):
    def test_entry_forces_wrapper_and_disables_escape_capabilities(self) -> None:
        key_blob = base64.b64encode(b"x" * 51).decode("ascii")
        entry = build_authorized_entry(
            f"ssh-ed25519 {key_blob} fixture",
            KEY_ID,
            Path("/data/data/com.termux/files/home/.remote-wake/bin/rwa_bridge.py"),
        )
        for restriction in (
            "restrict",
            "command=",
            "no-agent-forwarding",
            "no-port-forwarding",
            "no-X11-forwarding",
            "no-pty",
            "no-user-rc",
        ):
            self.assertIn(restriction, entry)
        self.assertNotIn("fixture", entry)


if __name__ == "__main__":
    unittest.main()
