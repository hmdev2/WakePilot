import json
import sys
import tempfile
import unittest
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))

from configure_target import configure_target  # noqa: E402
from rwa_bridge import BridgeError, load_targets  # noqa: E402


class ConfigureTargetTests(unittest.TestCase):
    def test_creates_and_updates_closed_allowlist_atomically(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "config.json"
            first_id = str(uuid.uuid4())
            second_id = str(uuid.uuid4())

            configure_target(path, first_id, "AA:BB:CC:DD:EE:FF", "192.168.1.255", 9)
            configure_target(path, second_id, "02:00:00:00:00:01", "10.0.0.255", 7)
            configure_target(path, first_id, "AA:BB:CC:DD:EE:00", "192.168.1.255", 9)

            targets = load_targets(path)
            self.assertEqual(2, len(targets))
            self.assertEqual(bytes.fromhex("AABBCCDDEE00"), targets[first_id].mac)
            self.assertEqual(7, targets[second_id].port)
            self.assertEqual({"v", "targets"}, set(json.loads(path.read_text()).keys()))

    def test_rejects_invalid_input_without_replacing_existing_config(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "config.json"
            target_id = str(uuid.uuid4())
            configure_target(path, target_id, "AA:BB:CC:DD:EE:FF", "192.168.1.255", 9)
            baseline = path.read_bytes()

            for arguments in (
                (str(uuid.uuid4()).upper(), "AA:BB:CC:DD:EE:FF", "192.168.1.255", 9),
                (str(uuid.uuid4()), "aa:bb:cc:dd:ee:ff", "192.168.1.255", 9),
                (str(uuid.uuid4()), "AA:BB:CC:DD:EE:FF", "999.1.1.1", 9),
                (str(uuid.uuid4()), "AA:BB:CC:DD:EE:FF", "192.168.1.255", 0),
            ):
                with self.assertRaises((ValueError, BridgeError)):
                    configure_target(path, *arguments)
                self.assertEqual(baseline, path.read_bytes())

    def test_rejects_corrupted_existing_configuration(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "config.json"
            path.write_text('{"v":1,"targets":{},"unexpected":true}', encoding="utf-8")

            with self.assertRaises(BridgeError):
                configure_target(
                    path,
                    str(uuid.uuid4()),
                    "AA:BB:CC:DD:EE:FF",
                    "192.168.1.255",
                    9,
                )


if __name__ == "__main__":
    unittest.main()
