using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

static class Program
{
    public const string Version = "1.5.9";
    public static readonly string Root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".."));
    public static Action<string> Log = _ => { };
    static string FileAt(string relative) => Path.Combine(Root, relative);
    public static void ValidatePort(string port)
    {
        if (!Regex.IsMatch(port ?? "", @"\ACOM[1-9][0-9]*\z")) throw new ArgumentException("Seleziona una porta COM valida.");
    }
    public static string Run(string executable, string arguments)
    {
        Log(Path.GetFileName(executable) + " " + arguments);
        var text = new StringBuilder();
        using (var process = new Process { StartInfo = new ProcessStartInfo(executable, arguments)
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Root } })
        {
            DataReceivedEventHandler received = (_, e) => { if (e.Data != null) { lock (text) text.AppendLine(e.Data); Log(e.Data); } };
            process.OutputDataReceived += received; process.ErrorDataReceived += received;
            process.Start(); process.BeginOutputReadLine(); process.BeginErrorReadLine();
            if (!process.WaitForExit(60000)) { process.Kill(); process.WaitForExit(); throw new TimeoutException("Tempo massimo superato: " + executable); }
            process.WaitForExit();
            if (process.ExitCode != 0) throw new InvalidOperationException("Operazione non riuscita, codice " + process.ExitCode + ":\r\n" + text);
        }
        return text.ToString();
    }
    public static void VerifyBundle()
    {
        var manifest = new JavaScriptSerializer().Deserialize<Manifest>(File.ReadAllText(FileAt("bundle.json")));
        if (manifest.version != Version || manifest.files == null || manifest.files.Length < 8) throw new InvalidDataException("Pacchetto incompleto o versione errata.");
        foreach (var item in manifest.files)
        {
            string path = Path.GetFullPath(FileAt(item.path));
            if (!path.StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Percorso non valido nel manifest.");
            using (var sha = SHA256.Create()) using (var file = File.OpenRead(path))
                if (!BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "").Equals(item.sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("File danneggiato: " + item.path);
        }
        Log("Integrita dei file inclusi verificata.");
    }
    public static string FirmwareVersion(string port)
    {
        ValidatePort(port);
        using (var serial = new SerialPort(port, 76800) { ReadTimeout = 100, WriteTimeout = 500 })
        {
            serial.Open(); serial.DiscardInBuffer();
            var timer = Stopwatch.StartNew(); long next = 0;
            while (timer.ElapsedMilliseconds < 4000)
            {
                if (timer.ElapsedMilliseconds >= next) { serial.Write(new byte[] { 1 }, 0, 1); next = timer.ElapsedMilliseconds + 250; }
                try { if (serial.ReadByte() == 1) return serial.ReadLine().Trim(); } catch (TimeoutException) { }
            }
        }
        throw new TimeoutException("Il dispositivo non risponde al controllo della versione.");
    }
    static bool Contains(byte[] source, byte[] pattern)
    {
        for (int i = 0; i <= source.Length - pattern.Length; i++)
        { int j = 0; while (j < pattern.Length && source[i + j] == pattern[j]) j++; if (j == pattern.Length) return true; }
        return false;
    }
    public static string DetectVariant(byte[] flash)
    {
        bool full = Contains(flash, new byte[] {0,2,4,0,3,0,1,16,3,2,0,0,3,2,1,0,6,0,4,0,6,5,0,32,6,5,4,0});
        bool half = Contains(flash, new byte[] {3,2,1,0,35,0,1,0,19,2,0,0,3,5,4,0,3,3,4,16,3,5,3,32});
        if (full == half) throw new InvalidDataException("Encoder non identificato automaticamente. Seleziona full-step o half-step in base alla tua scheda.");
        return full ? "full-step" : "half-step";
    }
    public static void Flash(string port, string variant, bool oldBootloader)
    {
        ValidatePort(port); VerifyBundle();
        if (!new[] { "auto", "full-step", "half-step" }.Contains(variant)) throw new ArgumentException("Variante firmware non valida.");
        // The main app releases its serial connection while this tool is open.
        // If another app owns the port, the backup step fails before any flash write.
        string backup = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Maxmix", "backups", DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));
        Directory.CreateDirectory(backup);
        Log("Backup originale: " + backup);
        string exe = FileAt(@"Avrdude\avrdude.exe");
        string args = "-C\"" + FileAt(@"Avrdude\avrdude.conf") + "\" -p atmega328p -c arduino -P " + port + " -b " + (oldBootloader ? "57600" : "115200");
        string flash = Path.Combine(backup, "flash.bin");
        string eeprom = Path.Combine(backup, "eeprom.bin");
        File.WriteAllText(Path.Combine(backup, "backup.log"), Run(exe, args + " -n -U\"flash:r:" + flash + ":r\" -U\"eeprom:r:" + eeprom + ":r\""));
        if (variant == "auto") variant = DetectVariant(File.ReadAllBytes(flash));
        Log("Configurazione encoder: " + variant);
        string hex = FileAt(@"Firmware\Maxmix-" + Version + "-" + variant + ".hex");
        File.WriteAllText(Path.Combine(backup, "update.log"), Run(exe, args + " -D -U\"flash:w:" + hex + ":i\""));
        string version = FirmwareVersion(port);
        if (version != Version) throw new InvalidOperationException("Flash scritta, ma la versione rilevata e " + version + ". Controlla i log in " + backup);
        Log("Firmware " + version + " installato e verificato. Puoi riaprire Maxmix.");
    }
    static int InstallDriver(bool elevate)
    {
        VerifyBundle();
        if (elevate && !new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
        {
            using (var process = Process.Start(new ProcessStartInfo(Application.ExecutablePath, "--install-driver-elevated") { UseShellExecute = true, Verb = "runas" }))
            { process.WaitForExit(); return process.ExitCode; }
        }
        string system = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess ? Path.Combine(Environment.GetEnvironmentVariable("WINDIR"), "Sysnative") : Environment.SystemDirectory;
        string result = Run(Path.Combine(system, "pnputil.exe"), "/add-driver \"" + FileAt(@"Driver\CH341SER.INF") + "\" /install");
        string logs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Maxmix", "logs");
        Directory.CreateDirectory(logs); File.WriteAllText(Path.Combine(logs, "driver-install.log"), result);
        return 0;
    }
    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            if (args.Length > 0 && args[0] == "--install-driver") return InstallDriver(true);
            if (args.Length > 0 && args[0] == "--install-driver-elevated") return InstallDriver(false);
            if (args.Length == 2 && args[0] == "--self-test")
            {
                VerifyBundle(); ValidatePort("COM123");
                bool rejected = false; try { ValidatePort("COM3 -F"); } catch (ArgumentException) { rejected = true; }
                if (!rejected) throw new Exception("Port validation failed");
                File.WriteAllText(args[1], "PASS bundle hashes, offline payload and serial port validation"); return 0;
            }
            if (args.Length == 4 && args[0] == "--flash") { Flash(args[1], args[2], args[3] == "old"); return 0; }
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ToolsForm(args.Contains("--from-app"))); return 0;
        }
        catch (Exception e)
        {
            if (args.Length == 2 && args[0] == "--self-test") File.WriteAllText(args[1], "FAIL " + e.Message);
            else MessageBox.Show(e.Message, "Maxmix - operazione non completata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }
    public class Manifest { public string version { get; set; } public Entry[] files { get; set; } }
    public class Entry { public string path { get; set; } public string sha256 { get; set; } }
}

class ToolsForm : Form
{
    readonly ComboBox ports = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
    readonly ComboBox variant = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 175 };
    readonly CheckBox oldBoot = new CheckBox { Text = "Nano con vecchio bootloader", Checked = true, AutoSize = true };
    readonly TextBox log = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, Dock = DockStyle.Fill, BackColor = Color.FromArgb(16, 21, 32), ForeColor = Color.Gainsboro };
    bool busy;
    public ToolsForm(bool fromApp)
    {
        Text = "Maxmix 1.5.9 - aggiornamento firmware offline"; Width = 820; Height = 580; MinimumSize = new Size(760, 500);
        BackColor = Color.FromArgb(23,31,48); ForeColor = Color.WhiteSmoke; Font = new Font("Segoe UI", 10); StartPosition = FormStartPosition.CenterScreen;
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), RowCount = 5, ColumnCount = 1 };
        for (int row = 0; row < 4; row++) panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        panel.Controls.Add(new Label { Text = (fromApp ? "La connessione USB dell'app e sospesa fino alla chiusura di questa finestra." : "Collega il Nano ATmega328P e chiudi Maxmix con Esci.") + "\r\nBackup prima della scrittura e verifica finale. Una scheda nuova richiede il bootloader Arduino.", AutoSize = true, Padding = new Padding(0,0,0,15) });
        var config = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(0,0,0,10) };
        config.Controls.Add(new Label { Text = "Porta USB", AutoSize = true, Padding = new Padding(0,5,0,0) }); config.Controls.Add(ports);
        variant.Items.AddRange(new object[] { "Automatica dal backup", "Full-step", "Half-step" }); variant.SelectedIndex = 0; config.Controls.Add(variant); config.Controls.Add(oldBoot); panel.Controls.Add(config);
        var newBoard = new CheckBox { Text = "Scheda nuova: non contiene ancora firmware Maxmix (scegli full-step o half-step)", AutoSize = true, Margin = new Padding(0,0,0,14) };
        newBoard.CheckedChanged += (_, __) => { if (newBoard.Checked && variant.SelectedIndex == 0) variant.SelectedIndex = 1; };
        panel.Controls.Add(newBoard);
        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(0,0,0,15) };
        var refresh = new Button { Text = "Cerca porte", AutoSize = true }; refresh.Click += (_, __) => RefreshPorts(); actions.Controls.Add(refresh);
        var update = new Button { Text = "Aggiorna firmware", AutoSize = true }; actions.Controls.Add(update);
        var driver = new Button { Text = "Installa driver USB", AutoSize = true }; actions.Controls.Add(driver);
        foreach (Button button in actions.Controls) { button.BackColor = Color.FromArgb(45,66,108); button.ForeColor = Color.White; button.FlatStyle = FlatStyle.Flat; button.Padding = new Padding(8,5,8,5); }
        panel.Controls.Add(actions); panel.Controls.Add(log); Controls.Add(panel);
        Program.Log = line => { if (!IsDisposed && IsHandleCreated) BeginInvoke(new Action(() => log.AppendText(line + Environment.NewLine))); };
        update.Click += async (_, __) =>
        {
            if (ports.SelectedItem == null) { MessageBox.Show("Seleziona una porta USB."); return; }
            if (newBoard.Checked && variant.SelectedIndex == 0) { MessageBox.Show("Per una scheda nuova scegli full-step o half-step."); return; }
            string port = ports.Text; string mode = new[] { "auto", "full-step", "half-step" }[variant.SelectedIndex]; bool old = oldBoot.Checked;
            busy = true; actions.Enabled = config.Enabled = newBoard.Enabled = false;
            try { await Task.Run(() => Program.Flash(port, mode, old)); MessageBox.Show("Firmware 1.5.9 installato e verificato.", "Maxmix"); }
            catch (Exception e) { log.AppendText("ERRORE: " + e.Message + Environment.NewLine); MessageBox.Show(e.Message, "Aggiornamento non completato", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { busy = false; actions.Enabled = config.Enabled = newBoard.Enabled = true; }
        };
        driver.Click += async (_, __) => { driver.Enabled = false; try { using (var process = Process.Start(new ProcessStartInfo(Application.ExecutablePath, "--install-driver") { UseShellExecute = true })) { await Task.Run(() => process.WaitForExit()); if (process.ExitCode != 0) throw new Exception("Installazione driver non completata."); } RefreshPorts(); } catch (Exception e) { MessageBox.Show(e.Message); } finally { driver.Enabled = true; } };
        FormClosing += (_, e) => { if (busy) { e.Cancel = true; MessageBox.Show("Attendi il termine dell'aggiornamento prima di chiudere."); } };
        RefreshPorts();
    }
    void RefreshPorts() { ports.Items.Clear(); ports.Items.AddRange(SerialPort.GetPortNames().OrderBy(x => x).ToArray()); if (ports.Items.Count == 1) ports.SelectedIndex = 0; }
}
