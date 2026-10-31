#!/usr/bin/env python3
"""Shows the active keyboard language as a tiny label next to the mouse cursor (macOS)."""
import ctypes
import ctypes.util

import AppKit
import Foundation
import objc

OFFSET_X, OFFSET_Y = 11, 7
WIDTH, HEIGHT = 30, 18

carbon = ctypes.CDLL("/System/Library/Frameworks/Carbon.framework/Carbon")
cf = ctypes.CDLL(ctypes.util.find_library("CoreFoundation"))
for func, res, args in (
    (carbon.TISCopyCurrentKeyboardInputSource, ctypes.c_void_p, []),
    (carbon.TISGetInputSourceProperty, ctypes.c_void_p, [ctypes.c_void_p, ctypes.c_void_p]),
    (cf.CFArrayGetCount, ctypes.c_long, [ctypes.c_void_p]),
    (cf.CFArrayGetValueAtIndex, ctypes.c_void_p, [ctypes.c_void_p, ctypes.c_long]),
    (cf.CFStringGetCString, ctypes.c_bool, [ctypes.c_void_p, ctypes.c_char_p, ctypes.c_long, ctypes.c_uint32]),
    (cf.CFRelease, None, [ctypes.c_void_p]),
):
    func.restype, func.argtypes = res, args
LANGUAGES_KEY = ctypes.c_void_p.in_dll(carbon, "kTISPropertyInputSourceLanguages")
UTF8 = 0x08000100


def read_language():
    source = carbon.TISCopyCurrentKeyboardInputSource()
    if not source:
        return None
    try:
        languages = carbon.TISGetInputSourceProperty(source, LANGUAGES_KEY)
        if not languages or cf.CFArrayGetCount(languages) == 0:
            return None
        buffer = ctypes.create_string_buffer(32)
        if not cf.CFStringGetCString(cf.CFArrayGetValueAtIndex(languages, 0), buffer, 32, UTF8):
            return None
        return buffer.value.decode()
    finally:
        cf.CFRelease(source)


def label_for(language):
    code = language.split("-")[0].lower()
    if code == "en":
        return "E"
    return code.upper()


def color_for(label):
    if label == "E":
        return AppKit.NSColor.colorWithSRGBRed_green_blue_alpha_(0.49, 0.83, 0.99, 1)
    if label == "FA":
        return AppKit.NSColor.colorWithSRGBRed_green_blue_alpha_(0.98, 0.75, 0.14, 1)
    return AppKit.NSColor.whiteColor()


class Controller(Foundation.NSObject):
    def init(self):
        self = objc.super(Controller, self).init()
        self.label = None
        rect = Foundation.NSMakeRect(0, 0, WIDTH, HEIGHT)
        self.window = AppKit.NSPanel.alloc().initWithContentRect_styleMask_backing_defer_(
            rect, AppKit.NSWindowStyleMaskBorderless | AppKit.NSWindowStyleMaskNonactivatingPanel,
            AppKit.NSBackingStoreBuffered, False)
        self.window.setOpaque_(False)
        self.window.setBackgroundColor_(AppKit.NSColor.clearColor())
        self.window.setIgnoresMouseEvents_(True)
        self.window.setHasShadow_(False)
        self.window.setLevel_(AppKit.NSStatusWindowLevel)
        self.window.setCollectionBehavior_(
            AppKit.NSWindowCollectionBehaviorCanJoinAllSpaces
            | AppKit.NSWindowCollectionBehaviorStationary
            | AppKit.NSWindowCollectionBehaviorFullScreenAuxiliary)

        self.field = AppKit.NSTextField.labelWithString_("")
        self.field.setFrame_(rect)
        self.field.setFont_(AppKit.NSFont.boldSystemFontOfSize_(12))
        shadow = AppKit.NSShadow.alloc().init()
        shadow.setShadowBlurRadius_(2)
        shadow.setShadowOffset_(Foundation.NSMakeSize(0, 0))
        shadow.setShadowColor_(AppKit.NSColor.blackColor())
        self.field.setWantsLayer_(True)
        self.field.setShadow_(shadow)
        self.window.contentView().addSubview_(self.field)
        self.window.orderFrontRegardless()

        self.status = AppKit.NSStatusBar.systemStatusBar().statusItemWithLength_(
            AppKit.NSVariableStatusItemLength)
        menu = AppKit.NSMenu.alloc().init()
        menu.addItemWithTitle_action_keyEquivalent_("Quit CursorLang", "terminate:", "q")
        self.status.setMenu_(menu)

        self.ticks = 0
        self.updateLanguage()
        Foundation.NSTimer.scheduledTimerWithTimeInterval_target_selector_userInfo_repeats_(
            1 / 60, self, "tick:", None, True)
        return self

    def updateLanguage(self):
        language = read_language()
        if not language:
            return
        label = label_for(language)
        if label != self.label:
            self.label = label
            self.field.setStringValue_(label)
            self.field.setTextColor_(color_for(label))
            self.status.button().setTitle_(label)

    def tick_(self, _timer):
        self.ticks += 1
        if self.ticks % 6 == 0:
            self.updateLanguage()
        pos = AppKit.NSEvent.mouseLocation()
        x, y = pos.x + OFFSET_X, pos.y - OFFSET_Y - HEIGHT
        # Flip to the other side of the cursor near the screen edges.
        for screen in AppKit.NSScreen.screens():
            frame = screen.frame()
            if Foundation.NSPointInRect(pos, frame):
                if x + WIDTH > frame.origin.x + frame.size.width:
                    x = pos.x - WIDTH - 4
                if y < frame.origin.y:
                    y = pos.y + 4
                break
        self.window.setFrameOrigin_(Foundation.NSMakePoint(x, y))


def main():
    app = AppKit.NSApplication.sharedApplication()
    app.setActivationPolicy_(AppKit.NSApplicationActivationPolicyAccessory)
    controller = Controller.alloc().init()  # noqa: F841 - kept alive for the app's lifetime
    app.run()


if __name__ == "__main__":
    main()
