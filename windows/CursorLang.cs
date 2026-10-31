using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

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
                MessageBox.Show("برنامه از قبل در حال اجراست. برچسب زبان کنار موس است و آیکن آن کنار ساعت ویندوز قرار دارد.", "CursorLang");
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
                DialogResult choice = MessageBox.Show("CursorLang قبلاً نصب شده است.\n\nYes: نصب دوباره\nNo: حذف برنامه\nCancel: انصراف", Title, MessageBoxButtons.YesNoCancel);
                if (choice == DialogResult.Cancel) return;
                if (choice == DialogResult.No)
                {
                    Uninstall();
                    MessageBox.Show("برنامه حذف شد.", Title);
                    return;
                }
            }
            else if (MessageBox.Show("CursorLang نصب شود؟", Title, MessageBoxButtons.OKCancel) != DialogResult.OK) return;
        }

        try
        {
            Install();
        }
        catch (Exception e)
        {
            if (!quiet) MessageBox.Show("نصب انجام نشد:\n" + e.Message, Title);
            Environment.ExitCode = 1;
            return;
        }
        if (!quiet) MessageBox.Show("نصب شد. برچسب زبان کنار موس است.\nبرای حذف: Settings > Apps > CursorLang", Title);
    }

    public static void RunUninstall(bool quiet)
    {
        if (!quiet && MessageBox.Show("CursorLang حذف شود؟", Title, MessageBoxButtons.OKCancel) != DialogResult.OK) return;
        Uninstall();
        if (!quiet) MessageBox.Show("برنامه حذف شد.", Title);
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
            key.SetValue("DisplayVersion", "3.0");
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

class Overlay : Form
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunValue = "CursorLang";
    const int OffsetX = 11, OffsetY = 7;
    const int RecolorDistance = 150;

    readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
    readonly NotifyIcon tray = new NotifyIcon();
    readonly float scale;
    IntPtr memDc, hBitmap, oldBitmap, trayIconHandle;
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
        ReleaseDC(IntPtr.Zero, screen);

        ContextMenuStrip menu = new ContextMenuStrip();
        menu.RightToLeft = RightToLeft.Yes;
        ToolStripMenuItem autostart = new ToolStripMenuItem("اجرا با شروع ویندوز");
        autostart.Checked = IsAutostart();
        autostart.Click += delegate { SetAutostart(!autostart.Checked); autostart.Checked = IsAutostart(); };
        menu.Items.Add(autostart);
        menu.Items.Add("حذف برنامه", null, delegate { Process.Start(Application.ExecutablePath, "--uninstall"); });
        menu.Items.Add("خروج", null, delegate { Close(); });
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
        UpdateLanguage();
        tray.Visible = true;
        timer.Start();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
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
        }
        TrackCursor();
        if (tick % 30 == 0) UpdateColors();
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

            using (Bitmap bmp = new Bitmap(labelSize.Width, labelSize.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (SolidBrush back = new SolidBrush(chip))
            using (SolidBrush brush = new SolidBrush(fill))
            {
                // An opaque chip keeps the letter readable over text in any color.
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.FillRectangle(back, 0, 0, labelSize.Width, labelSize.Height);
                g.FillPath(brush, path);

                IntPtr fresh = bmp.GetHbitmap(Color.FromArgb(0));
                IntPtr previous = SelectObject(memDc, fresh);
                if (hBitmap == IntPtr.Zero) oldBitmap = previous; else DeleteObject(hBitmap);
                hBitmap = fresh;
            }
        }
    }

    void PlaceLabel()
    {
        POINT dst = new POINT { X = placed.X, Y = placed.Y };
        POINT src = new POINT();
        SIZE size = new SIZE { cx = labelSize.Width, cy = labelSize.Height };
        BLENDFUNCTION blend = new BLENDFUNCTION { BlendOp = 0, SourceConstantAlpha = 255, AlphaFormat = 1 };
        UpdateLayeredWindow(Handle, IntPtr.Zero, ref dst, ref size, memDc, ref src, 0, ref blend, 2);
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

    [StructLayout(LayoutKind.Sequential)] struct POINT { public int X, Y; }
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

    [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT p);
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
