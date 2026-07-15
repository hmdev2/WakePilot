#!/data/data/com.termux/files/usr/bin/bash
set -eu

umask 077

ROOT="$HOME/.remote-wake"
SCRIPT_DIR="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
APPLY=0
PUBLIC_KEY_FILE=""
KEY_ID=""
SSHD_PORT=8022
TARGET_ID=""
TARGET_MAC=""
TARGET_BROADCAST=""
WAKE_PORT=9

while [ "$#" -gt 0 ]; do
  case "$1" in
    --apply) APPLY=1 ;;
    --public-key-file) shift; PUBLIC_KEY_FILE="${1:-}" ;;
    --key-id) shift; KEY_ID="${1:-}" ;;
    --sshd-port) shift; SSHD_PORT="${1:-}" ;;
    --target-id) shift; TARGET_ID="${1:-}" ;;
    --target-mac) shift; TARGET_MAC="${1:-}" ;;
    --target-broadcast) shift; TARGET_BROADCAST="${1:-}" ;;
    --wake-port) shift; WAKE_PORT="${1:-}" ;;
    *) echo "Opção inválida." >&2; exit 2 ;;
  esac
  shift
done

echo "Plano: instalar Python/OpenSSH no Termux, criar sshd isolado na porta $SSHD_PORT e registrar uma chave Ed25519 restrita."
echo "Recuperação: execute $ROOT/uninstall.sh para parar o sshd e remover apenas os arquivos do bridge."

if [ "$APPLY" -ne 1 ]; then
  echo "Nenhuma alteração aplicada. Revise o plano e execute novamente com --apply."
  exit 0
fi

if [ -z "$PUBLIC_KEY_FILE" ] || [ -z "$KEY_ID" ]; then
  echo "--public-key-file e --key-id são obrigatórios com --apply." >&2
  exit 2
fi

case "$SSHD_PORT" in *[!0-9]*|'') echo "--sshd-port inválida." >&2; exit 2 ;; esac
if [ "$SSHD_PORT" -lt 1024 ] || [ "$SSHD_PORT" -gt 65535 ]; then
  echo "--sshd-port deve estar entre 1024 e 65535." >&2
  exit 2
fi

case "$WAKE_PORT" in *[!0-9]*|'') echo "--wake-port inválida." >&2; exit 2 ;; esac
if [ "$WAKE_PORT" -lt 1 ] || [ "$WAKE_PORT" -gt 65535 ]; then
  echo "--wake-port deve estar entre 1 e 65535." >&2
  exit 2
fi

if [ -n "$TARGET_ID$TARGET_MAC$TARGET_BROADCAST" ] && \
  { [ -z "$TARGET_ID" ] || [ -z "$TARGET_MAC" ] || [ -z "$TARGET_BROADCAST" ]; }; then
  echo "--target-id, --target-mac e --target-broadcast devem ser usados juntos." >&2
  exit 2
fi

MISSING_PACKAGES=""
if ! command -v python >/dev/null 2>&1; then
  MISSING_PACKAGES="$MISSING_PACKAGES python"
fi
if ! command -v sshd >/dev/null 2>&1; then
  MISSING_PACKAGES="$MISSING_PACKAGES openssh"
fi
if [ -n "$MISSING_PACKAGES" ]; then
  # The values above are fixed package names, never user-controlled input.
  pkg install -y $MISSING_PACKAGES
fi
mkdir -p "$ROOT/bin" "$ROOT/run" "$HOME/.ssh" "$HOME/.termux/boot"
install -m 700 "$SCRIPT_DIR/rwa_bridge.py" "$ROOT/bin/rwa_bridge.py"
install -m 700 "$SCRIPT_DIR/install_key.py" "$ROOT/bin/install_key.py"
install -m 700 "$SCRIPT_DIR/configure_target.py" "$ROOT/bin/configure_target.py"

if [ ! -f "$ROOT/config.json" ]; then
  printf '%s\n' '{"v":1,"targets":{}}' > "$ROOT/config.json"
  chmod 600 "$ROOT/config.json"
fi

if [ -n "$TARGET_ID" ]; then
  python "$ROOT/bin/configure_target.py" \
    --config "$ROOT/config.json" \
    --target-id "$TARGET_ID" \
    --mac "$TARGET_MAC" \
    --broadcast "$TARGET_BROADCAST" \
    --port "$WAKE_PORT"
fi

if [ ! -f "$ROOT/ssh_host_ed25519_key" ]; then
  ssh-keygen -q -t ed25519 -N '' -f "$ROOT/ssh_host_ed25519_key"
fi

USER_NAME="$(id -un)"
cat > "$ROOT/sshd_config" <<EOF
Port $SSHD_PORT
ListenAddress 0.0.0.0
Protocol 2
HostKey $ROOT/ssh_host_ed25519_key
PidFile $ROOT/run/sshd.pid
AuthorizedKeysFile $HOME/.ssh/authorized_keys
AllowUsers $USER_NAME
PubkeyAuthentication yes
PasswordAuthentication no
KbdInteractiveAuthentication no
PermitEmptyPasswords no
AllowAgentForwarding no
AllowTcpForwarding no
X11Forwarding no
PermitTunnel no
PermitUserRC no
PermitTTY no
GatewayPorts no
MaxAuthTries 3
LoginGraceTime 20
EOF
chmod 600 "$ROOT/sshd_config"

python "$ROOT/bin/install_key.py" \
  --public-key-file "$PUBLIC_KEY_FILE" \
  --key-id "$KEY_ID" \
  --wrapper "$ROOT/bin/rwa_bridge.py" \
  --authorized-keys "$HOME/.ssh/authorized_keys"

cat > "$HOME/.termux/boot/remote-wake-bridge" <<EOF
#!/data/data/com.termux/files/usr/bin/bash
set -eu
termux-wake-lock >/dev/null 2>&1 || true
PID_FILE="$ROOT/run/sshd.pid"
if [ -r "\$PID_FILE" ]; then
  PID="\$(sed -n '1p' "\$PID_FILE")"
  case "\$PID" in
    *[!0-9]*|'') : ;;
    *)
      if kill -0 "\$PID" 2>/dev/null && [ -r "/proc/\$PID/cmdline" ] && \
        tr '\000' ' ' < "/proc/\$PID/cmdline" | grep -F -- "$ROOT/sshd_config" >/dev/null; then
        exit 0
      fi
      ;;
  esac
  rm -f "\$PID_FILE"
fi
"$PREFIX/bin/sshd" -f "$ROOT/sshd_config"
EOF
chmod 700 "$HOME/.termux/boot/remote-wake-bridge"

cat > "$ROOT/uninstall.sh" <<EOF
#!/data/data/com.termux/files/usr/bin/bash
set -eu
if [ -r "$ROOT/run/sshd.pid" ]; then
  PID="\$(sed -n '1p' "$ROOT/run/sshd.pid")"
  case "\$PID" in
    *[!0-9]*|'') : ;;
    *)
      if kill -0 "\$PID" 2>/dev/null && [ -r "/proc/\$PID/cmdline" ] && \
        tr '\000' ' ' < "/proc/\$PID/cmdline" | grep -F -- "$ROOT/sshd_config" >/dev/null; then
        kill "\$PID" 2>/dev/null || true
      fi
      ;;
  esac
fi
if [ -f "$HOME/.ssh/authorized_keys" ]; then
  sed -i '\|remote-wake-$KEY_ID$|d' "$HOME/.ssh/authorized_keys"
fi
rm -f "$HOME/.termux/boot/remote-wake-bridge"
rm -rf "$ROOT"
EOF
chmod 700 "$ROOT/uninstall.sh"

if [ -r "$ROOT/run/sshd.pid" ]; then
  EXISTING_PID="$(sed -n '1p' "$ROOT/run/sshd.pid")"
  case "$EXISTING_PID" in
    *[!0-9]*|'') : ;;
    *)
      if kill -0 "$EXISTING_PID" 2>/dev/null && [ -r "/proc/$EXISTING_PID/cmdline" ] && \
        tr '\000' ' ' < "/proc/$EXISTING_PID/cmdline" | grep -F -- "$ROOT/sshd_config" >/dev/null; then
        kill "$EXISTING_PID"
      fi
      ;;
  esac
  rm -f "$ROOT/run/sshd.pid"
fi
"$HOME/.termux/boot/remote-wake-bridge"
if [ -n "$TARGET_ID" ]; then
  echo "Bridge instalado e target cadastrado."
else
  echo "Bridge instalado. Cadastre um target antes do primeiro wake."
fi
