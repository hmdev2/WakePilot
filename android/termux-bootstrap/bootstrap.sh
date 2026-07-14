#!/data/data/com.termux/files/usr/bin/bash
set -eu

umask 077

ROOT="$HOME/.remote-wake"
SCRIPT_DIR="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
APPLY=0
PUBLIC_KEY_FILE=""
KEY_ID=""

while [ "$#" -gt 0 ]; do
  case "$1" in
    --apply) APPLY=1 ;;
    --public-key-file) shift; PUBLIC_KEY_FILE="${1:-}" ;;
    --key-id) shift; KEY_ID="${1:-}" ;;
    *) echo "Opção inválida." >&2; exit 2 ;;
  esac
  shift
done

echo "Plano: instalar Python/OpenSSH no Termux, criar sshd isolado na porta 8022 e registrar uma chave Ed25519 restrita."
echo "Recuperação: execute $ROOT/uninstall.sh para parar o sshd e remover apenas os arquivos do bridge."

if [ "$APPLY" -ne 1 ]; then
  echo "Nenhuma alteração aplicada. Revise o plano e execute novamente com --apply."
  exit 0
fi

if [ -z "$PUBLIC_KEY_FILE" ] || [ -z "$KEY_ID" ]; then
  echo "--public-key-file e --key-id são obrigatórios com --apply." >&2
  exit 2
fi

pkg install -y python openssh
mkdir -p "$ROOT/bin" "$ROOT/run" "$HOME/.ssh" "$HOME/.termux/boot"
install -m 700 "$SCRIPT_DIR/rwa_bridge.py" "$ROOT/bin/rwa_bridge.py"
install -m 700 "$SCRIPT_DIR/install_key.py" "$ROOT/bin/install_key.py"

if [ ! -f "$ROOT/config.json" ]; then
  printf '%s\n' '{"v":1,"targets":{}}' > "$ROOT/config.json"
  chmod 600 "$ROOT/config.json"
fi

if [ ! -f "$ROOT/ssh_host_ed25519_key" ]; then
  ssh-keygen -q -t ed25519 -N '' -f "$ROOT/ssh_host_ed25519_key"
fi

USER_NAME="$(id -un)"
cat > "$ROOT/sshd_config" <<EOF
Port 8022
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
PID_FILE="$ROOT/run/sshd.pid"
if [ -r "\$PID_FILE" ]; then
  PID="\$(sed -n '1p' "\$PID_FILE")"
  case "\$PID" in
    *[!0-9]*|'') : ;;
    *) if kill -0 "\$PID" 2>/dev/null; then exit 0; fi ;;
  esac
fi
"$PREFIX/bin/sshd" -f "$ROOT/sshd_config"
EOF
chmod 700 "$HOME/.termux/boot/remote-wake-bridge"

cat > "$ROOT/uninstall.sh" <<EOF
#!/data/data/com.termux/files/usr/bin/bash
set -eu
if [ -r "$ROOT/run/sshd.pid" ]; then
  PID="\$(sed -n '1p' "$ROOT/run/sshd.pid")"
  case "\$PID" in *[!0-9]*|'') : ;; *) kill "\$PID" 2>/dev/null || true ;; esac
fi
rm -f "$HOME/.termux/boot/remote-wake-bridge"
rm -rf "$ROOT"
EOF
chmod 700 "$ROOT/uninstall.sh"

"$HOME/.termux/boot/remote-wake-bridge"
echo "Bridge instalado. Cadastre um target no config.json antes do primeiro wake."
