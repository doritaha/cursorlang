#!/bin/sh
# Installs CursorLang for the current user on macOS or Linux and launches it.
set -e

base="https://raw.githubusercontent.com/doritaha/cursorlang/main"
dir="$HOME/.local/share/cursorlang"
mkdir -p "$dir"

case "$(uname -s)" in
  Darwin)
    python3 -m venv "$dir/venv"
    "$dir/venv/bin/pip" install --quiet pyobjc-framework-Cocoa
    curl -fsSL "$base/v3/mac/cursorlang.py" -o "$dir/cursorlang.py"
    plist="$HOME/Library/LaunchAgents/com.cursorlang.plist"
    mkdir -p "$(dirname "$plist")"
    cat > "$plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
<key>Label</key><string>com.cursorlang</string>
<key>ProgramArguments</key><array><string>$dir/venv/bin/python</string><string>$dir/cursorlang.py</string></array>
<key>RunAtLoad</key><true/>
</dict></plist>
EOF
    launchctl unload "$plist" 2>/dev/null || true
    launchctl load "$plist"
    ;;
  Linux)
    python3 -c 'import tkinter' 2>/dev/null || { echo "Install tkinter first, e.g.: sudo apt install python3-tk x11-xkb-utils" >&2; exit 1; }
    command -v setxkbmap >/dev/null || { echo "Install setxkbmap first, e.g.: sudo apt install x11-xkb-utils" >&2; exit 1; }
    curl -fsSL "$base/v3/linux/cursorlang.py" -o "$dir/cursorlang.py"
    mkdir -p "$HOME/.config/autostart"
    cat > "$HOME/.config/autostart/cursorlang.desktop" <<EOF
[Desktop Entry]
Type=Application
Name=CursorLang
Exec=python3 $dir/cursorlang.py
EOF
    nohup python3 "$dir/cursorlang.py" >/dev/null 2>&1 &
    ;;
  *)
    echo "Unsupported system." >&2
    exit 1
    ;;
esac
echo "CursorLang installed and running."
