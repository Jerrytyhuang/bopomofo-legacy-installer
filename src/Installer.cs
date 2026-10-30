// ㄅ半舊注音安裝程式
// 以 Windows 內建的 .NET Framework csc.exe 編譯（C# 5 語法，不用字串插值等新語法）。
using System;
using System.Diagnostics;
using System.Drawing;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: System.Reflection.AssemblyTitle("ㄅ半舊注音安裝程式")]
[assembly: System.Reflection.AssemblyProduct("Bopomofo Legacy Installer")]
[assembly: System.Reflection.AssemblyVersion("1.0.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.0.0.0")]

namespace BopomofoLegacyInstaller
{
    static class Ime
    {
        // Microsoft 注音 TIP 與隱藏的「傳統注音 (Phonetic)」profile
        public const string TipClsid = "{B115690A-EA02-48D5-A231-E3578D2FDF80}";
        public const string PhoneticProfile = "{761309DE-317A-11D4-9B5D-0080C882687E}";
        public const string InputTip = "0404:" + TipClsid + PhoneticProfile;

        public const string HklmProfileKey =
            @"SOFTWARE\Microsoft\CTF\TIP\" + TipClsid + @"\LanguageProfile\0x00000404\" + PhoneticProfile;
        const string ImetcKey = @"Software\Microsoft\IME\15.0\IMETC";
        const string Imtc70Key = @"Software\Microsoft\Windows\CurrentVersion\IME\IMTC70";
        const string Tsf3OverrideKey = @"Software\Microsoft\Input\TSF\Tsf3Override\" + TipClsid;

        static RegistryKey Hklm64()
        {
            return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        }

        public static bool IsProfileRegistered()
        {
            using (var k = Hklm64().OpenSubKey(HklmProfileKey))
                return k != null;
        }

        public static void RegisterProfile()
        {
            using (var k = Hklm64().CreateSubKey(HklmProfileKey))
            {
                k.SetValue("Description", "Chinese (Traditional) - Phonetic", RegistryValueKind.String);
                k.SetValue("Display Description", @"@%SystemRoot%\SYSTEM32\input.dll,-5066", RegistryValueKind.ExpandString);
                k.SetValue("IconFile", @"%SystemRoot%\SYSTEM32\ime\IMETC\ImTCTip.DLL", RegistryValueKind.ExpandString);
                k.SetValue("IconIndex", 0, RegistryValueKind.DWord);
                k.SetValue("Enable", 0, RegistryValueKind.DWord);
                k.SetValue("ProfileFlags", 4, RegistryValueKind.DWord);
            }
        }

        public static void UnregisterProfile()
        {
            Hklm64().DeleteSubKeyTree(HklmProfileKey, false);
        }

        public static void EnableAssociatedWords()
        {
            using (var k = Registry.CurrentUser.CreateSubKey(ImetcKey))
                k.SetValue("Prompt Associate Phrase.Phonetic", 1, RegistryValueKind.DWord);
            using (var k = Registry.CurrentUser.CreateSubKey(Imtc70Key))
                k.SetValue("Phonetic.AssociatedWord", 1, RegistryValueKind.DWord);
        }

        public static bool AssociatedWordsEnabled()
        {
            using (var k = Registry.CurrentUser.OpenSubKey(ImetcKey))
            {
                object v = k == null ? null : k.GetValue("Prompt Associate Phrase.Phonetic");
                return v is int && (int)v == 1;
            }
        }

        // 「使用舊版的微軟注音」相容性開關。Windows 10 看 NoTsf3Override2，新版 Windows 11 看 NoTsf3Override5。
        public static void EnableLegacyCompat()
        {
            using (var k = Registry.CurrentUser.CreateSubKey(Tsf3OverrideKey))
            {
                k.SetValue("NoTsf3Override2", 1, RegistryValueKind.DWord);
                k.SetValue("NoTsf3Override5", 1, RegistryValueKind.DWord);
            }
        }

        public static void DisableLegacyCompat()
        {
            using (var k = Registry.CurrentUser.OpenSubKey(Tsf3OverrideKey, true))
            {
                if (k == null) return;
                k.DeleteValue("NoTsf3Override2", false);
                k.DeleteValue("NoTsf3Override5", false);
            }
        }

        public static void RestartImeProcess()
        {
            foreach (var p in Process.GetProcessesByName("ChtIME"))
            {
                try { p.Kill(); } catch { }
            }
        }

        // 語言清單只能透過 PowerShell 的 International 模組改（它會同步更新 CTF 與設定 App 看到的清單）
        public static string AddToLanguageList(bool setDefault)
        {
            string script =
                "$ErrorActionPreference='Stop';" +
                "$tip='" + InputTip + "';" +
                "$l=Get-WinUserLanguageList;" +
                "$zh=$l|?{$_.LanguageTag -eq 'zh-Hant-TW'}|Select -First 1;" +
                "if(-not $zh){$l.Add('zh-Hant-TW');$zh=$l|?{$_.LanguageTag -eq 'zh-Hant-TW'}|Select -First 1;'已新增繁體中文 (台灣) 語言'};" +
                "if($zh.InputMethodTips -notcontains $tip){$zh.InputMethodTips.Add($tip);Set-WinUserLanguageList $l -Force;'已將 ㄅ半舊注音 加入鍵盤清單'}else{'鍵盤清單裡已經有 ㄅ半舊注音'};" +
                (setDefault ? "Set-WinDefaultInputMethodOverride -InputTip $tip;'已設為預設輸入法';" : "");
            return RunPowerShell(script);
        }

        public static string RemoveFromLanguageList()
        {
            string script =
                "$ErrorActionPreference='Stop';" +
                "$tip='" + InputTip + "';" +
                "$l=Get-WinUserLanguageList;$changed=$false;" +
                "foreach($x in $l){if($x.InputMethodTips -contains $tip){[void]$x.InputMethodTips.Remove($tip);$changed=$true}};" +
                "if($changed){Set-WinUserLanguageList $l -Force;'已從鍵盤清單移除 ㄅ半舊注音'}else{'鍵盤清單裡沒有 ㄅ半舊注音'};" +
                "$d=Get-WinDefaultInputMethodOverride;if($d -and $d.InputMethodTip -eq $tip){Set-WinDefaultInputMethodOverride -InputTip '';'已清除預設輸入法設定'}";
            return RunPowerShell(script);
        }

        public static bool IsInLanguageList()
        {
            string o = RunPowerShell("if((Get-WinUserLanguageList).InputMethodTips -contains '" + InputTip + "'){'YES'}else{'NO'}");
            return o.Trim().EndsWith("YES");
        }

        static string RunPowerShell(string script)
        {
            string prelude = "[Console]::OutputEncoding=[Text.Encoding]::UTF8;$ProgressPreference='SilentlyContinue';";
            string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(prelude + script));
            var psi = new ProcessStartInfo("powershell.exe",
                "-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand " + encoded);
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.StandardOutputEncoding = Encoding.UTF8;
            psi.StandardErrorEncoding = Encoding.UTF8;
            using (var p = Process.Start(psi))
            {
                string output = p.StandardOutput.ReadToEnd();
                string error = p.StandardError.ReadToEnd();
                p.WaitForExit();
                if (p.ExitCode != 0)
                    throw new Exception("PowerShell 執行失敗：" + (error.Length > 0 ? error : output));
                return output.Trim();
            }
        }
    }

    class MainForm : Form
    {
        readonly CheckBox chkDefault = new CheckBox();
        readonly CheckBox chkAssoc = new CheckBox();
        readonly CheckBox chkCompat = new CheckBox();
        readonly Label lblStatus = new Label();
        readonly TextBox txtLog = new TextBox();
        readonly Button btnInstall = new Button();
        readonly Button btnFixAssoc = new Button();
        readonly Button btnUninstall = new Button();

        public MainForm()
        {
            Text = "ㄅ半舊注音安裝程式";
            Font = new Font("Microsoft JhengHei UI", 10f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(560, 500);
            AutoScaleMode = AutoScaleMode.Dpi;

            var title = new Label();
            title.Text = "Windows 10 / 11 傳統ㄅ半舊注音（每個字手動選字、固定字序）";
            title.Font = new Font(Font.FontFamily, 11f, FontStyle.Bold);
            title.SetBounds(20, 16, 520, 26);

            var user = new Label();
            user.Text = "套用到使用者：" + WindowsIdentity.GetCurrent().Name;
            user.ForeColor = Color.DimGray;
            user.SetBounds(20, 46, 520, 22);

            lblStatus.SetBounds(20, 72, 520, 44);

            var options = new GroupBox();
            options.Text = "安裝選項";
            options.SetBounds(20, 120, 520, 122);
            chkDefault.Text = "設為預設輸入法";
            chkDefault.Checked = true;
            chkDefault.SetBounds(16, 26, 480, 26);
            chkAssoc.Text = "開啟聯想字（選完字後跳出建議的下一個字）";
            chkAssoc.Checked = true;
            chkAssoc.SetBounds(16, 54, 480, 26);
            chkCompat.Text = "開啟「使用舊版的微軟注音」相容性（建議）";
            chkCompat.Checked = true;
            chkCompat.SetBounds(16, 82, 480, 26);
            options.Controls.AddRange(new Control[] { chkDefault, chkAssoc, chkCompat });

            btnInstall.Text = "安裝";
            btnInstall.SetBounds(20, 254, 160, 36);
            btnInstall.Click += delegate { Run(Install); };
            btnFixAssoc.Text = "修復聯想字";
            btnFixAssoc.SetBounds(200, 254, 160, 36);
            btnFixAssoc.Click += delegate { Run(FixAssoc); };
            btnUninstall.Text = "解除安裝";
            btnUninstall.SetBounds(380, 254, 160, 36);
            btnUninstall.Click += delegate { Run(Uninstall); };

            txtLog.Multiline = true;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.BackColor = SystemColors.Window;
            txtLog.SetBounds(20, 302, 520, 150);

            var note = new Label();
            note.Text = "完成後請登出再登入。若第一次使用後聯想字沒出現，按「修復聯想字」。";
            note.ForeColor = Color.DimGray;
            note.SetBounds(20, 460, 520, 30);

            Controls.AddRange(new Control[] { title, user, lblStatus, options, btnInstall, btnFixAssoc, btnUninstall, txtLog, note });
            Shown += delegate { RefreshStatus(); };
        }

        void Log(string s)
        {
            if (string.IsNullOrEmpty(s)) return;
            txtLog.AppendText(s.Replace("\r\n", "\n").Replace("\n", Environment.NewLine) + Environment.NewLine);
        }

        void Run(Action action)
        {
            UseWaitCursor = true;
            btnInstall.Enabled = btnFixAssoc.Enabled = btnUninstall.Enabled = false;
            Application.DoEvents();
            try { action(); }
            catch (Exception ex) { Log("錯誤：" + ex.Message); }
            finally
            {
                btnInstall.Enabled = btnFixAssoc.Enabled = btnUninstall.Enabled = true;
                UseWaitCursor = false;
                RefreshStatus();
            }
        }

        void RefreshStatus()
        {
            try
            {
                bool reg = Ime.IsProfileRegistered();
                bool inList = Ime.IsInLanguageList();
                bool assoc = Ime.AssociatedWordsEnabled();
                lblStatus.Text = "目前狀態：系統註冊 " + (reg ? "✔" : "✘") +
                                 "　鍵盤清單 " + (inList ? "✔" : "✘") +
                                 "　聯想字 " + (assoc ? "開" : "關");
                lblStatus.ForeColor = reg && inList ? Color.ForestGreen : Color.Black;
            }
            catch (Exception ex) { lblStatus.Text = "無法讀取狀態：" + ex.Message; }
        }

        void Install()
        {
            Ime.RegisterProfile();
            Log("已註冊 ㄅ半舊注音（傳統注音 Phonetic）");
            Log(Ime.AddToLanguageList(chkDefault.Checked));
            if (chkAssoc.Checked) { Ime.EnableAssociatedWords(); Log("已開啟聯想字"); }
            if (chkCompat.Checked) { Ime.EnableLegacyCompat(); Log("已開啟「使用舊版的微軟注音」相容性"); }
            Ime.RestartImeProcess();
            Log("完成。請登出再登入，右下角輸入法圖示中「左上角缺角的方框」就是 ㄅ半舊注音。");
        }

        void FixAssoc()
        {
            Ime.EnableAssociatedWords();
            Ime.RestartImeProcess();
            Log("已重新開啟聯想字。請關閉並重新開啟正在打字的程式。");
        }

        void Uninstall()
        {
            if (MessageBox.Show(this, "要移除 ㄅ半舊注音 嗎？原本的微軟注音（新注音）不受影響。", Text,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            Log(Ime.RemoveFromLanguageList());
            Ime.UnregisterProfile();
            Log("已移除系統註冊");
            Ime.DisableLegacyCompat();
            Log("已關閉「使用舊版的微軟注音」相容性");
            Ime.RestartImeProcess();
            Log("完成。請登出再登入。");
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
