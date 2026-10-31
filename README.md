# CursorLang

**Always know your keyboard language, right next to your mouse.**
A tiny `E` or `FA` label follows the cursor, so you stop typing in the wrong language.

[فارسی](#فارسی) · [English](#english)

---

## فارسی

<div dir="rtl">

### این برنامه چیست؟

اگر زیاد پیش آمده که متنی را با زبان اشتباه تایپ کنید، CursorLang برای شماست. زبان فعلی کیبورد با یک برچسب خیلی کوچک کنار نشانگر موس نمایش داده می‌شود: `E` برای انگلیسی و `FA` برای فارسی.

### امکانات

- برچسب ریز و همیشه همراه موس
- پشتیبانی از زبان‌های نصب‌شده روی سیستم، مثل `AR` عربی، `RU` روسی، `ZH` چینی، `KO` کره‌ای، `JA` ژاپنی، `DE` آلمانی، `FR` فرانسوی، `ES` اسپانیایی و `TR` ترکی
- تغییر رنگ حرف بر اساس پس‌زمینه، تا روی هر صفحه‌ای دیده شود (ویندوز)
- بدون مزاحمت برای کلیک و تایپ
- اجرای خودکار با روشن شدن ویندوز

### نصب در ویندوز

1. فایل [CursorLangSetup.exe](https://github.com/doritaha/cursorlang/raw/main/CursorLangSetup.exe) را دانلود کنید.
2. روی آن دوبار کلیک کنید و OK را بزنید.
3. اگر هشدار آبی ویندوز آمد، **More info** و بعد **Run anyway** را بزنید. این هشدار به این دلیل است که فایل امضای دیجیتال ندارد.

### حذف در ویندوز

از **Settings > Apps > CursorLang** گزینهٔ **Uninstall** را بزنید. یا روی آیکن برنامه کنار ساعت راست‌کلیک کنید و **حذف برنامه** را بزنید.

### مک و لینوکس (آزمایشی)

در ترمینال اجرا کنید:

</div>

```bash
curl -fsSL https://raw.githubusercontent.com/doritaha/cursorlang/main/install.sh | sh
```

<div dir="rtl">

این دو نسخه هنوز روی سیستم واقعی تست نشده‌اند و رنگ متضاد ندارند. نسخهٔ لینوکس فقط روی X11 کار می‌کند، نه Wayland.

</div>

---

## English

### What is it?

If you keep typing in the wrong language, CursorLang is for you. It shows the active keyboard language as a tiny label next to the mouse cursor: `E` for English and `FA` for Persian.

### Features

- Tiny label that always follows the mouse
- Supports the languages installed on your system, such as `AR` Arabic, `RU` Russian, `ZH` Chinese, `KO` Korean, `JA` Japanese, `DE` German, `FR` French, `ES` Spanish and `TR` Turkish
- Letter color adapts to the background, so it stays visible (Windows)
- Never gets in the way of clicking or typing
- Starts automatically with Windows

### Install on Windows

1. Download [CursorLangSetup.exe](https://github.com/doritaha/cursorlang/raw/main/CursorLangSetup.exe).
2. Double-click it and press OK.
3. If a blue Windows warning appears, choose **More info**, then **Run anyway**. It appears because the file has no digital signature.

### Uninstall on Windows

Go to **Settings > Apps > CursorLang** and choose **Uninstall**. Or right-click the icon near the clock and choose **Uninstall**.

### macOS and Linux (experimental)

Run in a terminal:

```bash
curl -fsSL https://raw.githubusercontent.com/doritaha/cursorlang/main/install.sh | sh
```

These versions have not been tested on real systems yet and do not adapt their color. Linux works on X11 only, not Wayland.

---

## Versions / نسخه‌ها

| Version | Changes | تغییرات |
|---|---|---|
| [3.0](https://github.com/doritaha/cursorlang/releases/tag/v3.0) | More languages, menus in the Windows display language | زبان‌های بیشتر، منو به زبان ویندوز |
| [2.0](https://github.com/doritaha/cursorlang/releases/tag/v2.0) | One-file installer and uninstaller | نصب و حذف با یک فایل |
| [1.0](https://github.com/doritaha/cursorlang/releases/tag/v1.0) | Standalone exe with no installer | فایل اجرایی ساده بدون نصب |

## Project layout

| Path | Contents |
|---|---|
| `CursorLangSetup.exe` | Windows installer and uninstaller |
| `install.sh` | macOS and Linux installer |
| `install.ps1` | Windows install from source |
| `windows/` | Windows source and build script |
| `mac/` | macOS source |
| `linux/` | Linux source |

To build on Windows, run `windows\build.bat`. No extra tools are needed.
