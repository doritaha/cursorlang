#!/usr/bin/env python3
"""Shows the active keyboard language as a tiny label next to the mouse cursor (Linux, X11)."""
import ctypes
import ctypes.util
import os
import subprocess
import sys
import tkinter as tk

OFFSET_X, OFFSET_Y = 11, 7
TICK_MS = 16
XKB_USE_CORE_KBD = 0x0100


class XkbStateRec(ctypes.Structure):
    _fields_ = [
        ("group", ctypes.c_ubyte), ("locked_group", ctypes.c_ubyte),
        ("base_group", ctypes.c_ushort), ("latched_group", ctypes.c_ushort),
        ("mods", ctypes.c_ubyte), ("base_mods", ctypes.c_ubyte),
        ("latched_mods", ctypes.c_ubyte), ("locked_mods", ctypes.c_ubyte),
        ("compat_state", ctypes.c_ubyte), ("grab_mods", ctypes.c_ubyte),
        ("compat_grab_mods", ctypes.c_ubyte), ("lookup_mods", ctypes.c_ubyte),
        ("compat_lookup_mods", ctypes.c_ubyte),
        ("ptr_buttons", ctypes.c_ushort),
    ]


x11 = ctypes.CDLL(ctypes.util.find_library("X11"))
x11.XOpenDisplay.restype = ctypes.c_void_p
x11.XOpenDisplay.argtypes = [ctypes.c_char_p]
x11.XkbGetState.argtypes = [ctypes.c_void_p, ctypes.c_uint, ctypes.POINTER(XkbStateRec)]


def read_layouts():
    output = subprocess.run(["setxkbmap", "-query"], capture_output=True, text=True).stdout
    for line in output.splitlines():
        if line.startswith("layout:"):
            return line.split(":", 1)[1].strip().split(",")
    return []


LAYOUT_LABELS = {
    "us": "E", "gb": "E", "uk": "E", "ir": "FA", "ara": "AR", "cn": "ZH", "tw": "ZH",
    "kr": "KO", "jp": "JA", "se": "SV", "dk": "DA", "ua": "UK", "gr": "EL", "il": "HE",
    "br": "PT", "cz": "CS", "at": "DE", "ch": "DE", "be": "FR", "ca": "FR",
}


def label_for(layout):
    return LAYOUT_LABELS.get(layout, layout.upper())


def color_for(label):
    return {"E": "#7dd3fc", "FA": "#fbbf24"}.get(label, "#e2e8f0")


class Overlay:
    def __init__(self):
        self.display = x11.XOpenDisplay(None)
        if not self.display:
            sys.exit("Could not open the X display.")
        self.layouts = read_layouts()
        self.root = tk.Tk()
        self.root.overrideredirect(True)
        self.root.attributes("-topmost", True)
        self.label = tk.Label(self.root, font=("Sans", 9, "bold"), bg="#0f172a", padx=3, pady=0)
        self.label.pack()
        self.current = None
        self.ticks = 0
        self.update_language()
        self.tick()

    def update_language(self):
        state = XkbStateRec()
        if x11.XkbGetState(self.display, XKB_USE_CORE_KBD, ctypes.byref(state)) != 0:
            return
        if state.group >= len(self.layouts):
            self.layouts = read_layouts()
        if state.group >= len(self.layouts):
            return
        label = label_for(self.layouts[state.group])
        if label != self.current:
            self.current = label
            self.label.config(text=label, fg=color_for(label))

    def tick(self):
        self.ticks += 1
        if self.ticks % 6 == 0:
            self.update_language()
        if self.ticks % 60 == 0:
            self.root.lift()
        x, y = self.root.winfo_pointerxy()
        width, height = self.root.winfo_reqwidth(), self.root.winfo_reqheight()
        left, top = x + OFFSET_X, y + OFFSET_Y
        # Flip to the other side of the cursor near the screen edges.
        if left + width > self.root.winfo_screenwidth():
            left = x - width - 4
        if top + height > self.root.winfo_screenheight():
            top = y - height - 4
        self.root.geometry(f"+{left}+{top}")
        self.root.after(TICK_MS, self.tick)


def main():
    if os.environ.get("XDG_SESSION_TYPE") == "wayland":
        print("Warning: Wayland limits global cursor and layout access; use an X11 session.", file=sys.stderr)
    Overlay().root.mainloop()


if __name__ == "__main__":
    main()
