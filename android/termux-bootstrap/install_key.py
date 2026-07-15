#!/data/data/com.termux/files/usr/bin/python
"""Install one restricted Ed25519 launcher public key."""

from __future__ import annotations

import argparse
import base64
import os
import re
import tempfile
import uuid
from pathlib import Path


def build_authorized_entry(public_key: str, key_id: str, wrapper_path: Path) -> str:
    parsed_id = uuid.UUID(key_id)
    if parsed_id.int == 0 or str(parsed_id) != key_id:
        raise ValueError("key ID must be a canonical non-empty UUID")
    parts = public_key.strip().split()
    if len(parts) not in (2, 3) or parts[0] != "ssh-ed25519":
        raise ValueError("only one plain Ed25519 public key is accepted")
    if not re.fullmatch(r"[A-Za-z0-9+/]+={0,2}", parts[1]):
        raise ValueError("public key encoding is invalid")
    decoded = base64.b64decode(parts[1], validate=True)
    if len(decoded) < 32:
        raise ValueError("public key is invalid")
    path = str(wrapper_path.resolve())
    if any(character in path for character in ('"', "\n", "\r")):
        raise ValueError("wrapper path is invalid")
    restrictions = (
        f'restrict,command="{path} --key-id {key_id}",'
        "no-agent-forwarding,no-port-forwarding,no-X11-forwarding,no-pty,no-user-rc"
    )
    return f"{restrictions} ssh-ed25519 {parts[1]} remote-wake-{key_id}\n"


def install(public_key_path: Path, key_id: str, wrapper_path: Path, authorized_keys_path: Path) -> None:
    entry = build_authorized_entry(public_key_path.read_text(encoding="utf-8"), key_id, wrapper_path)
    authorized_keys_path.parent.mkdir(parents=True, exist_ok=True, mode=0o700)
    existing = authorized_keys_path.read_text(encoding="utf-8").splitlines() if authorized_keys_path.exists() else []
    marker = f"remote-wake-{key_id}"
    retained = [line for line in existing if not line.endswith(marker)]
    retained.append(entry.rstrip("\n"))
    descriptor, temporary_name = tempfile.mkstemp(prefix=".authorized-", dir=authorized_keys_path.parent)
    try:
        with os.fdopen(descriptor, "w", encoding="utf-8") as stream:
            stream.write("\n".join(retained) + "\n")
            stream.flush()
            os.fsync(stream.fileno())
        os.chmod(temporary_name, 0o600)
        os.replace(temporary_name, authorized_keys_path)
    finally:
        try:
            os.unlink(temporary_name)
        except FileNotFoundError:
            pass


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--public-key-file", required=True, type=Path)
    parser.add_argument("--key-id", required=True)
    parser.add_argument("--wrapper", required=True, type=Path)
    parser.add_argument("--authorized-keys", required=True, type=Path)
    arguments = parser.parse_args()
    install(arguments.public_key_file, arguments.key_id, arguments.wrapper, arguments.authorized_keys)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
