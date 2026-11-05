using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

// UI text in the Windows display language, falling back to English.
static class T
{
    public const int Autostart = 0, Uninstall = 1, Exit = 2, AlreadyRunning = 3, AskInstall = 4, Reinstall = 5, Installed = 6, InstallFailed = 7, AskRemove = 8, Removed = 9;

    static readonly Dictionary<string, string[]> Texts = new Dictionary<string, string[]>
    {
        { "en", new[] { "Start with Windows", "Uninstall", "Exit", "CursorLang is already running. The language label is next to the mouse and its icon is near the clock.", "Install CursorLang?", "CursorLang is already installed.\n\nYes: Reinstall\nNo: Uninstall\nCancel: Do nothing", "Installed. The language label is next to the mouse.\nTo remove it: Settings > Apps > CursorLang", "Installation failed:", "Uninstall CursorLang?", "CursorLang was removed." } },
        { "fa", new[] { "اجرا با شروع ویندوز", "حذف برنامه", "خروج", "برنامه از قبل در حال اجراست. برچسب زبان کنار موس است و آیکن آن کنار ساعت ویندوز قرار دارد.", "CursorLang نصب شود؟", "CursorLang قبلاً نصب شده است.\n\nYes: نصب دوباره\nNo: حذف برنامه\nCancel: انصراف", "نصب شد. برچسب زبان کنار موس است.\nبرای حذف: Settings > Apps > CursorLang", "نصب انجام نشد:", "CursorLang حذف شود؟", "برنامه حذف شد." } },
        { "ar", new[] { "التشغيل مع بدء Windows", "إلغاء التثبيت", "خروج", "البرنامج قيد التشغيل بالفعل. تظهر علامة اللغة بجانب الماوس وأيقونته قرب الساعة.", "هل تريد تثبيت CursorLang؟", "CursorLang مثبت بالفعل.\n\nYes: إعادة التثبيت\nNo: إلغاء التثبيت\nCancel: إلغاء", "تم التثبيت. تظهر علامة اللغة بجانب الماوس.\nللإزالة: Settings > Apps > CursorLang", "فشل التثبيت:", "هل تريد إلغاء تثبيت CursorLang؟", "تمت إزالة البرنامج." } },
        { "ru", new[] { "Запускать вместе с Windows", "Удалить", "Выход", "Программа уже запущена. Метка языка находится рядом с мышью, а значок — возле часов.", "Установить CursorLang?", "CursorLang уже установлен.\n\nYes: переустановить\nNo: удалить\nCancel: отмена", "Установлено. Метка языка находится рядом с мышью.\nУдаление: Settings > Apps > CursorLang", "Не удалось установить:", "Удалить CursorLang?", "Программа удалена." } },
        { "zh", new[] { "随 Windows 启动", "卸载", "退出", "程序已在运行。语言标签显示在鼠标旁，图标位于时钟附近。", "要安装 CursorLang 吗？", "CursorLang 已安装。\n\nYes：重新安装\nNo：卸载\nCancel：取消", "安装完成。语言标签显示在鼠标旁。\n卸载方法：Settings > Apps > CursorLang", "安装失败：", "要卸载 CursorLang 吗？", "程序已卸载。" } },
        { "ko", new[] { "Windows 시작 시 실행", "제거", "종료", "프로그램이 이미 실행 중입니다. 언어 표시는 마우스 옆에, 아이콘은 시계 옆에 있습니다.", "CursorLang을 설치할까요?", "CursorLang이 이미 설치되어 있습니다.\n\nYes: 다시 설치\nNo: 제거\nCancel: 취소", "설치되었습니다. 언어 표시는 마우스 옆에 있습니다.\n제거 방법: Settings > Apps > CursorLang", "설치 실패:", "CursorLang을 제거할까요?", "프로그램이 제거되었습니다." } },
        { "ja", new[] { "Windows の起動時に実行", "アンインストール", "終了", "すでに実行中です。言語ラベルはマウスの横に、アイコンは時計の近くにあります。", "CursorLang をインストールしますか？", "CursorLang はすでにインストールされています。\n\nYes: 再インストール\nNo: アンインストール\nCancel: キャンセル", "インストールしました。言語ラベルはマウスの横にあります。\n削除方法: Settings > Apps > CursorLang", "インストールに失敗しました:", "CursorLang をアンインストールしますか？", "削除しました。" } },
        { "de", new[] { "Mit Windows starten", "Deinstallieren", "Beenden", "Das Programm läuft bereits. Die Sprachanzeige ist neben der Maus, das Symbol neben der Uhr.", "CursorLang installieren?", "CursorLang ist bereits installiert.\n\nYes: Neu installieren\nNo: Deinstallieren\nCancel: Abbrechen", "Installiert. Die Sprachanzeige ist neben der Maus.\nEntfernen: Settings > Apps > CursorLang", "Installation fehlgeschlagen:", "CursorLang deinstallieren?", "Das Programm wurde entfernt." } },
        { "fr", new[] { "Lancer au démarrage de Windows", "Désinstaller", "Quitter", "Le programme est déjà lancé. L'indicateur de langue est près de la souris et son icône près de l'horloge.", "Installer CursorLang ?", "CursorLang est déjà installé.\n\nYes : réinstaller\nNo : désinstaller\nCancel : annuler", "Installé. L'indicateur de langue est près de la souris.\nPour le supprimer : Settings > Apps > CursorLang", "Échec de l'installation :", "Désinstaller CursorLang ?", "Le programme a été supprimé." } },
        { "es", new[] { "Iniciar con Windows", "Desinstalar", "Salir", "El programa ya se está ejecutando. La etiqueta de idioma está junto al ratón y el icono junto al reloj.", "¿Instalar CursorLang?", "CursorLang ya está instalado.\n\nYes: reinstalar\nNo: desinstalar\nCancel: cancelar", "Instalado. La etiqueta de idioma está junto al ratón.\nPara quitarlo: Settings > Apps > CursorLang", "Error de instalación:", "¿Desinstalar CursorLang?", "El programa se ha eliminado." } },
        { "tr", new[] { "Windows ile başlat", "Kaldır", "Çıkış", "Program zaten çalışıyor. Dil etiketi farenin yanında, simgesi saatin yanındadır.", "CursorLang yüklensin mi?", "CursorLang zaten yüklü.\n\nYes: Yeniden yükle\nNo: Kaldır\nCancel: İptal", "Yüklendi. Dil etiketi farenin yanındadır.\nKaldırmak için: Settings > Apps > CursorLang", "Yükleme başarısız:", "CursorLang kaldırılsın mı?", "Program kaldırıldı." } },
    };

    static string Language { get { return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName; } }

    public static bool IsRightToLeft { get { return CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft; } }

    public static string Get(int id)
    {
        string[] texts;
        return (Texts.TryGetValue(Language, out texts) ? texts : Texts["en"])[id];
    }
}

// Shows the active keyboard language as a tiny label next to the mouse cursor.
static class Program
{
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();

    [STAThread]
    static void Main(string[] args)
    {
        bool quiet = Array.IndexOf(args, "--quiet") >= 0;
        if (Array.IndexOf(args, "--uninstall") >= 0)
        {
            Setup.RunUninstall(quiet);
            return;
        }
        if (!Setup.IsInstalledCopy())
        {
            Setup.RunInstaller(quiet);
            return;
        }

        bool isNew;
        using (new Mutex(true, "CursorLang.SingleInstance", out isNew))
        {
            if (!isNew)
            {
                MessageBox.Show(T.Get(T.AlreadyRunning), "CursorLang");
                return;
            }
            SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.Run(new Overlay());
        }
    }
}

// The same executable is the installer, the app and the uninstaller.
static class Setup
{
    const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\CursorLang";
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string Title = "CursorLang";

    static string InstallDir { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CursorLang"); } }
    static string InstalledExe { get { return Path.Combine(InstallDir, "CursorLang.exe"); } }
    static string ShortcutPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "CursorLang.lnk"); } }

    public static bool IsInstalledCopy()
    {
        return string.Equals(Path.GetFullPath(Application.ExecutablePath), Path.GetFullPath(InstalledExe), StringComparison.OrdinalIgnoreCase);
    }

    public static void RunInstaller(bool quiet)
    {
        if (!quiet)
        {
            if (File.Exists(InstalledExe))
            {
                DialogResult choice = MessageBox.Show(T.Get(T.Reinstall), Title, MessageBoxButtons.YesNoCancel);
                if (choice == DialogResult.Cancel) return;
                if (choice == DialogResult.No)
                {
                    Uninstall();
                    MessageBox.Show(T.Get(T.Removed), Title);
                    return;
                }
            }
            else if (MessageBox.Show(T.Get(T.AskInstall), Title, MessageBoxButtons.OKCancel) != DialogResult.OK) return;
        }

        try
        {
            Install();
        }
        catch (Exception e)
        {
            if (!quiet) MessageBox.Show(T.Get(T.InstallFailed) + "\n" + e.Message, Title);
            Environment.ExitCode = 1;
            return;
        }
        if (!quiet) MessageBox.Show(T.Get(T.Installed) + "\n\nCtrl+Alt+Z: sghl > سلام\nCtrl+Alt+F: salam > سلام", Title);
    }

    public static void RunUninstall(bool quiet)
    {
        if (!quiet && MessageBox.Show(T.Get(T.AskRemove), Title, MessageBoxButtons.OKCancel) != DialogResult.OK) return;
        Uninstall();
        if (!quiet) MessageBox.Show(T.Get(T.Removed), Title);
    }

    static void StopOtherCopies()
    {
        int self = Process.GetCurrentProcess().Id;
        foreach (Process p in Process.GetProcessesByName("CursorLang"))
        {
            if (p.Id == self) continue;
            p.Kill();
            p.WaitForExit(3000);
        }
    }

    static void Install()
    {
        StopOtherCopies();
        Directory.CreateDirectory(InstallDir);
        File.Copy(Application.ExecutablePath, InstalledExe, true);

        using (RegistryKey run = Registry.CurrentUser.CreateSubKey(RunKey))
            run.SetValue("CursorLang", "\"" + InstalledExe + "\"");
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(UninstallKey))
        {
            key.SetValue("DisplayName", "CursorLang");
            key.SetValue("DisplayVersion", "4.1");
            key.SetValue("DisplayIcon", InstalledExe);
            key.SetValue("InstallLocation", InstallDir);
            key.SetValue("UninstallString", "\"" + InstalledExe + "\" --uninstall");
            key.SetValue("NoModify", 1, RegistryValueKind.DWord);
            key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        }

        Type shellType = Type.GetTypeFromProgID("WScript.Shell");
        object shell = Activator.CreateInstance(shellType);
        object link = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { ShortcutPath });
        link.GetType().InvokeMember("TargetPath", BindingFlags.SetProperty, null, link, new object[] { InstalledExe });
        link.GetType().InvokeMember("Save", BindingFlags.InvokeMethod, null, link, null);

        Process.Start(InstalledExe);
    }

    static void Uninstall()
    {
        StopOtherCopies();
        using (RegistryKey run = Registry.CurrentUser.CreateSubKey(RunKey))
            run.DeleteValue("CursorLang", false);
        Registry.CurrentUser.DeleteSubKeyTree(UninstallKey, false);
        if (File.Exists(ShortcutPath)) File.Delete(ShortcutPath);

        // A running exe cannot delete itself, so a hidden shell removes the folder after this process exits.
        ProcessStartInfo cleanup = new ProcessStartInfo("cmd.exe", "/c ping -n 3 127.0.0.1 >nul & rmdir /s /q \"" + InstallDir + "\"");
        cleanup.CreateNoWindow = true;
        cleanup.UseShellExecute = false;
        cleanup.WorkingDirectory = Path.GetTempPath();
        Process.Start(cleanup);
    }
}

static class Converter
{
    // Pairs of US keys and the Persian characters they type on the standard Windows layout.
    const string UsKeys = "qwertyuiop[]asdfghjkl;'zxcvbnm,\\`?\"HC1234567890";
    const string FaKeys = "ضصثقفغعهخحجچشسیبلاتنمکگظطزرذدئوپ÷؟؛آژ۱۲۳۴۵۶۷۸۹۰";

    static readonly Dictionary<char, char> ToFa = new Dictionary<char, char>();
    static readonly Dictionary<char, char> ToUs = new Dictionary<char, char>();

    // Longest spellings first, so "kh" wins over "k".
    static readonly string[][] Finglish =
    {
        new[] { "kh", "خ" }, new[] { "sh", "ش" }, new[] { "ch", "چ" }, new[] { "zh", "ژ" }, new[] { "gh", "ق" },
        new[] { "ph", "ف" }, new[] { "aa", "ا" }, new[] { "oo", "و" }, new[] { "ou", "و" }, new[] { "ee", "ی" },
        new[] { "ei", "ی" }, new[] { "ey", "ی" },
        new[] { "b", "ب" }, new[] { "p", "پ" }, new[] { "t", "ت" }, new[] { "j", "ج" }, new[] { "h", "ه" },
        new[] { "d", "د" }, new[] { "r", "ر" }, new[] { "z", "ز" }, new[] { "s", "س" }, new[] { "f", "ف" },
        new[] { "q", "ق" }, new[] { "k", "ک" }, new[] { "c", "ک" }, new[] { "g", "گ" }, new[] { "l", "ل" },
        new[] { "m", "م" }, new[] { "n", "ن" }, new[] { "v", "و" }, new[] { "w", "و" }, new[] { "y", "ی" },
        new[] { "x", "کس" }, new[] { "u", "و" }, new[] { "i", "ی" }, new[] { "a", "ا" },
    };

    // Common words spelled out in full, because rules alone cannot recover unwritten vowels.
    static readonly Dictionary<string, string> Words = new Dictionary<string, string>
    {
        { "salam", "سلام" }, { "khoobi", "خوبی" }, { "khubi", "خوبی" }, { "khoob", "خوب" }, { "khub", "خوب" }, { "chetori", "چطوری" }, { "chetor", "چطور" }, { "man", "من" }, { "to", "تو" }, { "ma", "ما" }, { "shoma", "شما" }, { "oon", "اون" }, { "un", "اون" }, { "in", "این" }, { "an", "آن" }, { "merci", "مرسی" }, { "mersi", "مرسی" }, { "mamnoon", "ممنون" }, { "mamnun", "ممنون" }, { "lotfan", "لطفا" }, { "tashakor", "تشکر" }, { "baleh", "بله" }, { "bale", "بله" }, { "na", "نه" }, { "are", "آره" }, { "areh", "آره" }, { "bashe", "باشه" }, { "basheh", "باشه" }, { "koja", "کجا" }, { "chi", "چی" }, { "chera", "چرا" }, { "kei", "کی" }, { "key", "کی" }, { "ke", "که" }, { "keh", "که" }, { "ba", "با" }, { "az", "از" }, { "be", "به" }, { "dar", "در" }, { "ham", "هم" }, { "ye", "یه" }, { "yek", "یک" }, { "va", "و" }, { "ya", "یا" }, { "vali", "ولی" }, { "ama", "اما" }, { "amma", "اما" }, { "pas", "پس" }, { "alan", "الان" }, { "kheili", "خیلی" }, { "kheyli", "خیلی" }, { "khyli", "خیلی" }, { "hast", "هست" }, { "nist", "نیست" }, { "daram", "دارم" }, { "dari", "داری" }, { "dare", "داره" }, { "mikonam", "میکنم" }, { "mikham", "میخوام" }, { "nemikham", "نمیخوام" }, { "midoonam", "میدونم" }, { "nemidoonam", "نمیدونم" }, { "mishe", "میشه" }, { "nemishe", "نمیشه" }, { "bia", "بیا" }, { "boro", "برو" }, { "kar", "کار" }, { "khone", "خونه" }, { "khoneh", "خونه" }, { "khane", "خانه" }, { "hal", "حال" }, { "dost", "دوست" }, { "doost", "دوست" }, { "dooset", "دوستت" }, { "emrooz", "امروز" }, { "emruz", "امروز" }, { "farda", "فردا" }, { "dirooz", "دیروز" }, { "diruz", "دیروز" }, { "shab", "شب" }, { "rooz", "روز" }, { "ruz", "روز" }, { "agha", "آقا" }, { "khanoom", "خانم" }, { "khanom", "خانم" }, { "baba", "بابا" }, { "maman", "مامان" }, { "jan", "جان" }, { "joon", "جون" }, { "azizam", "عزیزم" }, { "khodafez", "خدافظ" }, { "khodahafez", "خداحافظ" }, { "ali", "علی" },
    };

    static Converter()
    {
        for (int i = 0; i < UsKeys.Length; i++)
        {
            ToFa[UsKeys[i]] = FaKeys[i];
            ToUs[FaKeys[i]] = UsKeys[i];
        }
        ToUs['ي'] = 'd';
        ToUs['ك'] = ';';
        ToUs['،'] = ',';
    }

    static bool IsPersian(char c)
    {
        return c >= '؀' && c <= 'ۿ';
    }

    // Fixes text typed on the wrong layout, in whichever direction the text shows.
    public static string SwapLayout(string text)
    {
        bool toUs = false;
        foreach (char c in text)
            if (IsPersian(c) && char.IsLetter(c)) { toUs = true; break; }

        StringBuilder result = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            char mapped;
            if (toUs) result.Append(ToUs.TryGetValue(c, out mapped) ? mapped : c);
            else if (ToFa.TryGetValue(c, out mapped)) result.Append(mapped);
            else if (char.IsUpper(c) && ToFa.TryGetValue(char.ToLowerInvariant(c), out mapped)) result.Append(mapped);
            else result.Append(c);
        }
        return result.ToString();
    }

    // A best-effort reading of Latin-script Persian, since Finglish does not write most short vowels.
    public static string FromFinglish(string text)
    {
        StringBuilder result = new StringBuilder(text.Length);
        int i = 0;
        while (i < text.Length)
        {
            if (!char.IsLetter(text[i]) || text[i] > 'z')
            {
                char c = text[i++];
                result.Append(c == ',' ? '،' : c == '?' ? '؟' : c == ';' ? '؛' : c);
                continue;
            }
            int end = i;
            while (end < text.Length && char.IsLetter(text[end]) && text[end] <= 'z') end++;
            result.Append(FinglishWord(text.Substring(i, end - i).ToLowerInvariant()));
            i = end;
        }
        return result.ToString();
    }

    static string FinglishWord(string word)
    {
        string known;
        if (Words.TryGetValue(word, out known)) return known;

        StringBuilder result = new StringBuilder(word.Length);
        int i = 0;
        if (word.StartsWith("aa")) { result.Append('آ'); i = 2; }
        else if (word[0] == 'a') { result.Append('ا'); i = 1; }
        else if (word[0] == 'e' || word[0] == 'o') { result.Append('ا'); i = 1; }
        else if (word[0] == 'u' || word.StartsWith("oo")) { result.Append("او"); i = word[0] == 'u' ? 1 : 2; }
        else if (word[0] == 'i') { result.Append("ای"); i = 1; }

        while (i < word.Length)
        {
            string[] match = null;
            foreach (string[] rule in Finglish)
                if (string.CompareOrdinal(word, i, rule[0], 0, rule[0].Length) == 0) { match = rule; break; }

            if (match != null)
            {
                result.Append(match[1]);
                i += match[0].Length;
            }
            else
            {
                // Short e and o are not written, except at the end of a word where e becomes ه.
                if (word[i] == 'e' && i == word.Length - 1) result.Append('ه');
                else if (word[i] == 'o' && i == word.Length - 1) result.Append('و');
                i++;
            }
        }
        return result.ToString();
    }
}

class Overlay : Form
{
    const int HotkeyLayout = 1, HotkeyFinglish = 2;
    const uint WM_HOTKEY = 0x312, CF_UNICODETEXT = 13;
    const uint MOD_ALT = 1, MOD_CONTROL = 2, MOD_NOREPEAT = 0x4000;
    const byte VK_CONTROL = 0x11, VK_SHIFT = 0x10, VK_MENU = 0x12, VK_Z = 0x5A, VK_F = 0x46, VK_C = 0x43, VK_V = 0x56;
    const uint KEYEVENTF_KEYUP = 2;
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunValue = "CursorLang";
    const int OffsetX = 11, OffsetY = 7;
    const int RecolorDistance = 150;

    readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
    readonly NotifyIcon tray = new NotifyIcon();
    readonly float scale;
    IntPtr memDc, hBitmap, trayIconHandle;
    // A second label sits next to the text caret, with its own colors.
    CaretLabel caretLabel;
    IntPtr caretDc, caretBitmap;
    Color caretFill = Color.White, caretChip = Color.Black;
    Point caretPlaced;
    bool caretShown;
    volatile object caretRect;
    static readonly int OwnProcessId = Process.GetCurrentProcess().Id;
    Size labelSize;
    string lang = "";
    Point lastPos = Point.Empty;
    Point placed;
    Color fill = Color.White, chip = Color.Black;
    int tick;

    public Overlay()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) scale = g.DpiX / 96f;

        IntPtr screen = GetDC(IntPtr.Zero);
        memDc = CreateCompatibleDC(screen);
        caretDc = CreateCompatibleDC(screen);
        ReleaseDC(IntPtr.Zero, screen);

        ContextMenuStrip menu = new ContextMenuStrip();
        menu.RightToLeft = T.IsRightToLeft ? RightToLeft.Yes : RightToLeft.No;
        ToolStripMenuItem autostart = new ToolStripMenuItem(T.Get(T.Autostart));
        autostart.Checked = IsAutostart();
        autostart.Click += delegate { SetAutostart(!autostart.Checked); autostart.Checked = IsAutostart(); };
        menu.Items.Add(autostart);
        menu.Items.Add(T.Get(T.Uninstall), null, delegate { Process.Start(Application.ExecutablePath, "--uninstall"); });
        menu.Items.Add(T.Get(T.Exit), null, delegate { Close(); });
        tray.ContextMenuStrip = menu;
        tray.Text = "CursorLang";

        timer.Interval = 15;
        timer.Tick += OnTick;
    }

    protected override bool ShowWithoutActivation { get { return true; } }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TOPMOST;
            return cp;
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        caretLabel = new CaretLabel();
        caretLabel.Show();
        UpdateLanguage();
        Thread watcher = new Thread(WatchCaret);
        watcher.IsBackground = true;
        watcher.Start();
        tray.Visible = true;
        timer.Start();
        bool layout = RegisterHotKey(Handle, HotkeyLayout, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, VK_Z);
        bool finglish = RegisterHotKey(Handle, HotkeyFinglish, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, VK_F);
        Log("registered layout=" + layout + " finglish=" + finglish);
        if (!layout) tray.ShowBalloonTip(5000, "CursorLang", "Ctrl+Alt+Z is used by another program.", ToolTipIcon.Warning);
        if (!finglish) tray.ShowBalloonTip(5000, "CursorLang", "Ctrl+Alt+F is used by another program.", ToolTipIcon.Warning);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == (int)WM_HOTKEY) ConvertSelection(m.WParam.ToInt32() == HotkeyFinglish);
        base.WndProc(ref m);
    }

    // Replaces the selection through the clipboard, then puts the user's clipboard back.
    void ConvertSelection(bool finglish)
    {
        Log("hotkey foreground=" + GetForegroundWindow());
        WaitForKeysUp();
        Log("keys up");
        DataObject saved = SnapshotClipboard();
        try
        {
            Clipboard.Clear();
            PressWithControl(VK_C);
            string text = WaitForClipboardText();
            Log("copied length=" + (text == null ? -1 : text.Length));
            if (string.IsNullOrEmpty(text)) return;

            string converted = finglish ? Converter.FromFinglish(text) : Converter.SwapLayout(text);
            Clipboard.SetDataObject(converted, true, 5, 100);
            PressWithControl(VK_V);
            Log("pasted length=" + converted.Length);
            // The target app reads the clipboard asynchronously after the paste keystroke.
            Thread.Sleep(500);
        }
        catch (ExternalException e) { Log("error " + e.Message); }
        finally
        {
            try { Clipboard.SetDataObject(saved, true, 5, 100); }
            catch (ExternalException) { }
            // Presses made while converting would otherwise run on whatever is selected next.
            NativeMessage pending;
            while (PeekMessage(out pending, Handle, WM_HOTKEY, WM_HOTKEY, 1)) { }
        }
    }

    // Records only the steps and text lengths, never the text itself.
    static void Log(string message)
    {
        try { File.AppendAllText(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "log.txt"), DateTime.Now.ToString("HH:mm:ss.fff ") + message + Environment.NewLine); }
        catch (IOException) { }
    }

    static void WaitForKeysUp()
    {
        byte[] keys = { VK_CONTROL, VK_MENU, VK_SHIFT, VK_Z, VK_F };
        for (int waited = 0; waited < 2000; waited += 20)
        {
            bool anyDown = false;
            foreach (byte key in keys)
                if ((GetAsyncKeyState(key) & 0x8000) != 0) anyDown = true;
            if (!anyDown) return;
            Thread.Sleep(20);
        }
    }

    // Only the common formats are kept, because reading every format can block for seconds.
    static DataObject SnapshotClipboard()
    {
        DataObject copy = new DataObject();
        try
        {
            if (Clipboard.ContainsText()) copy.SetText(Clipboard.GetText());
            if (Clipboard.ContainsImage()) copy.SetImage(Clipboard.GetImage());
            if (Clipboard.ContainsFileDropList()) copy.SetFileDropList(Clipboard.GetFileDropList());
        }
        catch (ExternalException) { }
        return copy;
    }

    static string WaitForClipboardText()
    {
        for (int waited = 0; waited < 600; waited += 30)
        {
            Thread.Sleep(30);
            string text = ReadClipboardText();
            if (text != null) return text;
        }
        return null;
    }

    // Reads the clipboard directly, because the managed API can wait seconds on a busy owner.
    static string ReadClipboardText()
    {
        if (!OpenClipboard(IntPtr.Zero)) return null;
        try
        {
            IntPtr handle = GetClipboardData(CF_UNICODETEXT);
            if (handle == IntPtr.Zero) return null;
            IntPtr data = GlobalLock(handle);
            if (data == IntPtr.Zero) return null;
            try { return Marshal.PtrToStringUni(data); }
            finally { GlobalUnlock(handle); }
        }
        finally { CloseClipboard(); }
    }

    static void PressWithControl(byte key)
    {
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        keybd_event(key, 0, 0, UIntPtr.Zero);
        keybd_event(key, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        UnregisterHotKey(Handle, HotkeyLayout);
        UnregisterHotKey(Handle, HotkeyFinglish);
        timer.Stop();
        tray.Visible = false;
        tray.Dispose();
        base.OnFormClosed(e);
    }

    void OnTick(object sender, EventArgs e)
    {
        // The cursor is tracked every tick; the language and z-order about every 100 ms.
        if (++tick % 7 == 0)
        {
            UpdateLanguage();
            SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            SetWindowPos(caretLabel.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }
        TrackCursor();
        TrackCaret();
        if (tick % 30 == 0)
        {
            UpdateColors();
            if (caretShown) UpdateCaretColors(false);
        }
    }

    void TrackCaret()
    {
        object box = caretRect;
        if (box == null)
        {
            if (caretShown)
            {
                caretShown = false;
                Push(caretLabel.Handle, caretDc, caretPlaced, 0);
            }
            return;
        }

        // Above the caret, so the label does not cover the text being typed.
        Rectangle caret = (Rectangle)box;
        Rectangle area = Screen.FromPoint(caret.Location).Bounds;
        int x = caret.Right + (int)(2 * scale);
        int y = caret.Top - labelSize.Height - (int)(1 * scale);
        if (y < area.Top) y = caret.Bottom + (int)(1 * scale);
        if (x + labelSize.Width > area.Right) x = caret.Left - labelSize.Width - (int)(2 * scale);
        Point at = new Point(x, y);
        if (caretShown && at == caretPlaced) return;

        caretPlaced = at;
        if (!caretShown)
        {
            // The hidden label is fully transparent, so the spot it will cover can be sampled directly.
            caretShown = true;
            UpdateCaretColors(true);
        }
        Push(caretLabel.Handle, caretDc, caretPlaced, 255);
    }

    void UpdateCaretColors(bool atLabel)
    {
        Point spot = atLabel ? caretPlaced : new Point(caretPlaced.X, caretPlaced.Y - labelSize.Height - 1);
        Color sample = SampleBackground(spot);
        int distance = Math.Abs(sample.R - caretChip.R) + Math.Abs(sample.G - caretChip.G) + Math.Abs(sample.B - caretChip.B);
        if (!atLabel && distance < RecolorDistance) return;
        caretChip = sample;
        caretFill = ContrastFor(caretChip);
        Render();
        PlaceLabel();
        if (caretShown) Push(caretLabel.Handle, caretDc, caretPlaced, 255);
    }

    // Caret lookups are cross-process calls that can stall on a busy app, so they run off the UI thread.
    void WatchCaret()
    {
        while (true)
        {
            object found = null;
            try { found = ReadCaret(); }
            catch (Exception) { }
            caretRect = found;
            Thread.Sleep(50);
        }
    }

    // Classic apps report a system caret; browsers and Electron apps report it through accessibility.
    static object ReadCaret()
    {
        IntPtr foreground = GetForegroundWindow();
        if (foreground == IntPtr.Zero) return null;
        uint pid;
        GetWindowThreadProcessId(foreground, out pid);
        if (pid == OwnProcessId) return null;

        GUITHREADINFO info = new GUITHREADINFO();
        info.cbSize = Marshal.SizeOf(typeof(GUITHREADINFO));
        if (!GetGUIThreadInfo(0, ref info)) return null;
        if (info.hwndCaret != IntPtr.Zero && info.rcCaret.Bottom > info.rcCaret.Top)
        {
            POINT p = new POINT { X = info.rcCaret.Left, Y = info.rcCaret.Top };
            ClientToScreen(info.hwndCaret, ref p);
            return new Rectangle(p.X, p.Y, Math.Max(1, info.rcCaret.Right - info.rcCaret.Left), info.rcCaret.Bottom - info.rcCaret.Top);
        }

        IntPtr hwnd = info.hwndFocus != IntPtr.Zero ? info.hwndFocus : foreground;
        Guid iid = IID_IAccessible;
        object found;
        if (AccessibleObjectFromWindow(hwnd, OBJID_CARET, ref iid, out found) != 0 || found == null) return null;
        try
        {
            int left, top, width, height;
            ((Accessibility.IAccessible)found).accLocation(out left, out top, out width, out height, 0);
            if (height <= 0 || (left == 0 && top == 0)) return null;
            return new Rectangle(left, top, Math.Max(1, width), height);
        }
        finally { Marshal.ReleaseComObject(found); }
    }

    void TrackCursor()
    {
        POINT cursor;
        if (!GetCursorPos(out cursor)) return;
        Point pos = new Point(cursor.X, cursor.Y);
        if (pos != lastPos)
        {
            lastPos = pos;
            placed = LabelOrigin();
            PlaceLabel();
        }
    }

    // The screen capture includes the label itself, so the strip just below it is sampled instead.
    // Small differences are ignored to keep the color steady over text.
    void UpdateColors()
    {
        int stripY = placed.Y + labelSize.Height + 1;
        if (stripY + labelSize.Height > Screen.FromPoint(placed).Bounds.Bottom) stripY = placed.Y - labelSize.Height - 1;
        Color sample = SampleBackground(new Point(placed.X, stripY));
        int distance = Math.Abs(sample.R - chip.R) + Math.Abs(sample.G - chip.G) + Math.Abs(sample.B - chip.B);
        if (distance < RecolorDistance) return;
        chip = sample;
        fill = ContrastFor(chip);
        Render();
        PlaceLabel();
    }

    void UpdateLanguage()
    {
        string current = ReadLanguage();
        if (current == null || current == lang) return;
        lang = current;
        Render();
        UpdateTrayIcon();
        PlaceLabel();
        if (caretShown) Push(caretLabel.Handle, caretDc, caretPlaced, 255);
    }

    // The focused control's thread is used so that UWP apps report their real layout.
    static string ReadLanguage()
    {
        IntPtr target = GetForegroundWindow();
        if (target == IntPtr.Zero) return null;
        GUITHREADINFO info = new GUITHREADINFO();
        info.cbSize = Marshal.SizeOf(typeof(GUITHREADINFO));
        if (GetGUIThreadInfo(0, ref info) && info.hwndFocus != IntPtr.Zero) target = info.hwndFocus;
        uint pid;
        uint thread = GetWindowThreadProcessId(target, out pid);
        int langId = (int)(GetKeyboardLayout(thread).ToInt64() & 0xFFFF);
        if (langId == 0) return null;
        return Label(langId);
    }

    static string Label(int langId)
    {
        int primary = langId & 0x3FF;
        if (primary == 0x09) return "E";
        if (primary == 0x29) return "FA";
        try { return new CultureInfo(langId).TwoLetterISOLanguageName.ToUpperInvariant(); }
        catch (ArgumentException) { return "?"; }
    }

    static Color LabelColor(string label)
    {
        if (label == "E") return Color.FromArgb(125, 211, 252);
        if (label == "FA") return Color.FromArgb(251, 191, 36);
        return Color.FromArgb(226, 232, 240);
    }

    static double Luminance(Color c)
    {
        return 0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B;
    }

    // Uses the inverted color, falling back to black or white on mid-tones where inverting barely changes it.
    static Color ContrastFor(Color background)
    {
        Color inverted = Color.FromArgb(255 - background.R, 255 - background.G, 255 - background.B);
        if (Math.Abs(Luminance(inverted) - Luminance(background)) >= 100) return inverted;
        return Luminance(background) > 127 ? Color.Black : Color.White;
    }

    Color SampleBackground(Point origin)
    {
        int width = Math.Max(labelSize.Width, 1), height = Math.Max(labelSize.Height, 1);
        using (Bitmap bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppRgb))
        {
            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr dst = g.GetHdc();
                IntPtr screen = GetDC(IntPtr.Zero);
                BitBlt(dst, 0, 0, width, height, screen, origin.X, origin.Y, SRCCOPY);
                ReleaseDC(IntPtr.Zero, screen);
                g.ReleaseHdc(dst);
            }
            long r = 0, g2 = 0, b = 0;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    r += c.R; g2 += c.G; b += c.B;
                }
            long count = width * height;
            return Color.FromArgb((int)(r / count), (int)(g2 / count), (int)(b / count));
        }
    }

    void Render()
    {
        using (GraphicsPath path = new GraphicsPath())
        using (FontFamily family = new FontFamily("Segoe UI"))
        {
            path.AddString(lang, family, (int)FontStyle.Bold, 11f * scale, Point.Empty, StringFormat.GenericTypographic);
            RectangleF bounds = path.GetBounds();
            int pad = (int)Math.Ceiling(3f * scale);
            using (Matrix shift = new Matrix(1, 0, 0, 1, pad - bounds.X, pad - bounds.Y)) path.Transform(shift);
            labelSize = new Size((int)Math.Ceiling(bounds.Width) + pad * 2, (int)Math.Ceiling(bounds.Height) + pad * 2);

            PaintLabel(path, memDc, ref hBitmap, chip, fill);
            PaintLabel(path, caretDc, ref caretBitmap, caretChip, caretFill);
        }
    }

    void PaintLabel(GraphicsPath path, IntPtr dc, ref IntPtr bitmap, Color back, Color fore)
    {
        using (Bitmap bmp = new Bitmap(labelSize.Width, labelSize.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
        using (Graphics g = Graphics.FromImage(bmp))
        using (SolidBrush backBrush = new SolidBrush(back))
        using (SolidBrush foreBrush = new SolidBrush(fore))
        {
            // An opaque chip keeps the letter readable over text in any color.
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.FillRectangle(backBrush, 0, 0, labelSize.Width, labelSize.Height);
            g.FillPath(foreBrush, path);

            IntPtr fresh = bmp.GetHbitmap(Color.FromArgb(0));
            SelectObject(dc, fresh);
            if (bitmap != IntPtr.Zero) DeleteObject(bitmap);
            bitmap = fresh;
        }
    }

    void PlaceLabel()
    {
        Push(Handle, memDc, placed, 255);
    }

    void Push(IntPtr hwnd, IntPtr dc, Point at, byte alpha)
    {
        POINT dst = new POINT { X = at.X, Y = at.Y };
        POINT src = new POINT();
        SIZE size = new SIZE { cx = labelSize.Width, cy = labelSize.Height };
        BLENDFUNCTION blend = new BLENDFUNCTION { BlendOp = 0, SourceConstantAlpha = alpha, AlphaFormat = 1 };
        UpdateLayeredWindow(hwnd, IntPtr.Zero, ref dst, ref size, dc, ref src, 0, ref blend, 2);
    }

    // The caret label's own click-through window; Overlay draws into it.
    class CaretLabel : Form
    {
        public CaretLabel()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TOPMOST;
                return cp;
            }
        }
    }

    Point LabelOrigin()
    {
        Point pos = lastPos;

        // Flip to the other side of the cursor near the screen edges.
        Rectangle area = Screen.FromPoint(pos).Bounds;
        int x = pos.X + (int)(OffsetX * scale);
        int y = pos.Y + (int)(OffsetY * scale);
        if (x + labelSize.Width > area.Right) x = pos.X - labelSize.Width - (int)(4 * scale);
        if (y + labelSize.Height > area.Bottom) y = pos.Y - labelSize.Height - (int)(4 * scale);
        return new Point(x, y);
    }

    void UpdateTrayIcon()
    {
        using (Bitmap bmp = new Bitmap(32, 32))
        using (Graphics g = Graphics.FromImage(bmp))
        using (Font font = new Font("Segoe UI", lang.Length > 1 ? 14f : 18f, FontStyle.Bold, GraphicsUnit.Pixel))
        using (SolidBrush back = new SolidBrush(Color.FromArgb(15, 23, 42)))
        using (SolidBrush fore = new SolidBrush(LabelColor(lang)))
        using (StringFormat center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
        {
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.FillRectangle(back, 0, 0, 32, 32);
            g.DrawString(lang, font, fore, new RectangleF(0, 0, 32, 32), center);
            IntPtr fresh = bmp.GetHicon();
            using (Icon icon = Icon.FromHandle(fresh)) tray.Icon = (Icon)icon.Clone();
            if (trayIconHandle != IntPtr.Zero) DestroyIcon(trayIconHandle);
            trayIconHandle = fresh;
        }
    }

    static bool IsAutostart()
    {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey))
            return key != null && key.GetValue(RunValue) != null;
    }

    static void SetAutostart(bool enable)
    {
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKey))
        {
            if (enable) key.SetValue(RunValue, "\"" + Application.ExecutablePath + "\"");
            else key.DeleteValue(RunValue, false);
        }
    }

    const int WS_EX_TOPMOST = 0x8, WS_EX_TRANSPARENT = 0x20, WS_EX_TOOLWINDOW = 0x80, WS_EX_LAYERED = 0x80000, WS_EX_NOACTIVATE = 0x8000000;
    const uint SWP_NOSIZE = 0x1, SWP_NOMOVE = 0x2, SWP_NOACTIVATE = 0x10;
    const int SRCCOPY = 0xCC0020;
    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    const uint OBJID_CARET = 0xFFFFFFF8;
    static readonly Guid IID_IAccessible = new Guid("618736E0-3C3D-11CF-810C-00AA00389B71");

    [StructLayout(LayoutKind.Sequential)] struct POINT { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] struct NativeMessage { public IntPtr hwnd; public uint message; public IntPtr wParam, lParam; public uint time; public POINT pt; }
    [StructLayout(LayoutKind.Sequential)] struct SIZE { public int cx, cy; }
    [StructLayout(LayoutKind.Sequential)] struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential, Pack = 1)] struct BLENDFUNCTION { public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }
    [StructLayout(LayoutKind.Sequential)]
    struct GUITHREADINFO
    {
        public int cbSize, flags;
        public IntPtr hwndActive, hwndFocus, hwndCapture, hwndMenuOwner, hwndMoveSize, hwndCaret;
        public RECT rcCaret;
    }

    [DllImport("user32.dll")] static extern bool PeekMessage(out NativeMessage message, IntPtr hwnd, uint first, uint last, uint remove);
    [DllImport("user32.dll")] static extern bool OpenClipboard(IntPtr owner);
    [DllImport("user32.dll")] static extern bool CloseClipboard();
    [DllImport("user32.dll")] static extern IntPtr GetClipboardData(uint format);
    [DllImport("kernel32.dll")] static extern IntPtr GlobalLock(IntPtr handle);
    [DllImport("kernel32.dll")] static extern bool GlobalUnlock(IntPtr handle);
    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hwnd, int id);
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
    [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT p);
    [DllImport("user32.dll")] static extern bool ClientToScreen(IntPtr hwnd, ref POINT p);
    [DllImport("oleacc.dll")] static extern int AccessibleObjectFromWindow(IntPtr hwnd, uint id, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out object accessible);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
    [DllImport("user32.dll")] static extern IntPtr GetKeyboardLayout(uint thread);
    [DllImport("user32.dll")] static extern bool GetGUIThreadInfo(uint thread, ref GUITHREADINFO info);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT dst, ref SIZE size, IntPtr hdcSrc, ref POINT src, int key, ref BLENDFUNCTION blend, int flags);
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);
    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr icon);
    [DllImport("gdi32.dll")] static extern bool BitBlt(IntPtr dst, int x, int y, int w, int h, IntPtr src, int sx, int sy, int op);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
}
