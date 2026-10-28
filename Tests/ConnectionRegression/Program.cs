using MaxMix.Services.Audio;
using MaxMix.Services.Communication;
using MaxMix.ViewModels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

class Program
{
    static int failures;
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    static void Test(string name, Action test)
    {
        try { test(); Console.WriteLine("PASS " + name); }
        catch (Exception e) { failures++; Console.WriteLine("FAIL " + name + ": " + e); }
    }
    static void Set(object target, string field, object value) => target.GetType()
        .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    static object Get(object target, string field) => target.GetType()
        .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);

    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--preview") return Preview();
        Test("RGB color keeps the blue channel", () =>
        {
            var color = new MaxMix.Services.Communication.Color(12, 34, 56);
            Check(color.r == 12 && color.g == 34 && color.b == 56, "blue channel overwritten by green");
        });
        Test("four user LED colors preserve RGB order in the serial payload", () =>
        {
            var colors = new LightingSettings {
                inputColor = new MaxMix.Services.Communication.Color(1,2,3),
                applicationColor = new MaxMix.Services.Communication.Color(4,5,6),
                muteColor = new MaxMix.Services.Communication.Color(7,8,9),
                disconnectedColor = new MaxMix.Services.Communication.Color(10,11,12)
            };
            var stream = new MemoryStream(); colors.GetBytes(stream);
            Check(stream.ToArray().SequenceEqual(Enumerable.Range(1,12).Select(x => (byte)x)), "RGB colors were reordered or padded");
            var decoded = new LightingSettings(); decoded.SetBytes(stream.ToArray());
            Check(decoded.muteColor.b == 9 && decoded.disconnectedColor.r == 10, "color round trip failed");
            Check(FirmwareVersions.SupportsLightingSettings("1.5.9") && !FirmwareVersions.SupportsLightingSettings("1.5.8"), "legacy firmware received an unsupported color command");
        });
        Test("selected LED color survives settings reload and reaches firmware RGB", () =>
        {
            var model = new SettingsViewModel();
            uint original = model.InputColor;
            try
            {
                model.InputColor = 0xFF563412;
                MaxMix.Properties.Settings.Default.Reload();
                var restored = new SettingsViewModel();
                Check(restored.InputColor == 0xFF563412, "selected color was not saved");
                var rgb = restored.ToLightingSettings().inputColor;
                Check(rgb.r == 0x12 && rgb.g == 0x34 && rgb.b == 0x56, "UI color did not reach firmware in RGB order");
            }
            finally { model.InputColor = original; }
        });
        Test("double-click interval uses two little-endian bytes", () =>
        {
            var stream = new MemoryStream();
            new ButtonSettings { doubleTapTime = 350 }.GetBytes(stream);
            Check(stream.ToArray().SequenceEqual(new byte[] { 94, 1 }), "incorrect button settings payload");
            Check(FirmwareVersions.SupportsButtonSettings("1.5.8") && !FirmwareVersions.SupportsButtonSettings("1.5.7"), "legacy firmware capability detection failed");
        });
        Test("default device change sends two-byte volume payloads", () =>
        {
            var service = new CommunicationService(() => new string[0], _ => null, new InlineContext());
            var vm = (MainViewModel)FormatterServices.GetUninitializedObject(typeof(MainViewModel));
            var sessions = new SessionData[4];
            sessions[2].data.id = 0;
            sessions[3].data.id = 1;
            sessions[3].data.isDefault = true;
            Set(vm, "_communicationService", service);
            Set(vm, "m_Sessions", sessions);
            Set(vm, "m_IndexToId", new Dictionary<int, int> { { 0, 42 }, { 1, 43 } });
            vm.IsConnected = true;
            typeof(MainViewModel).GetMethod("OnDefaultDeviceChanged", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(vm, new object[] { null, 42, DeviceFlow.Output });
            var queue = (CircularBuffer<KeyValuePair<Command, IMessage>>)Get(service, "m_MessageQueue");
            Check(queue.Count == 2, "expected updates for old and new defaults");
            for (int i = 0; i < queue.Count; i++)
            {
                var stream = new MemoryStream();
                queue[i].Value.GetBytes(stream);
                Check(stream.Length == 2, "volume payload was " + stream.Length + " bytes, expected 2");
            }
        });
        Test("UI only enqueues; serial writes belong to worker", () =>
        {
            var fake = new FakeSerial();
            var service = new CommunicationService(() => new string[0], _ => fake, new InlineContext());
            Set(service, "m_SerialPort", fake);
            Set(service, "m_DeviceConnected", true);
            Set(service, "m_DeviceReady", true);
            Set(service, "m_LastMessageWrite", DateTime.Now);
            service.SendMessage(Command.SETTINGS, DeviceSettings.Default());
            Check(fake.Writes.Count == 0, "SendMessage wrote on the calling thread");
        });
        Test("Start is idempotent", () =>
        {
            var service = new CommunicationService(() => new string[0], _ => null, new InlineContext());
            try
            {
                service.Start();
                var first = Get(service, "m_Thread");
                service.Start();
                Check(ReferenceEquals(first, Get(service, "m_Thread")), "Start created a second serial worker");
            }
            finally { service.Stop(); }
        });
        Test("handshake survives a delayed boot reply", () =>
        {
            var fake = new FakeSerial { BootTimeouts = 2 };
            var service = new CommunicationService(() => new[] { "FAKE" }, _ => fake, new InlineContext());
            using (var connected = new ManualResetEventSlim())
            {
                service.OnDeviceConnected += () => connected.Set();
                try { service.Start(); Check(connected.Wait(1500), "boot timeout closed the port before the firmware answered"); }
                finally { service.Stop(); }
            }
        });
        Test("mismatched payload is rejected before entering the queue", () =>
        {
            var service = new CommunicationService(() => new string[0], _ => null, new InlineContext());
            bool rejected = false;
            try { service.SendMessage(Command.VOLUME_CURR_CHANGE, SessionData.Default()); }
            catch (ArgumentException) { rejected = true; }
            Check(rejected, "invalid payload accepted");
        });
        Test("device ERROR triggers reconnect and clean session", () =>
        {
            var first = new FakeSerial();
            var second = new FakeSerial();
            int opens = 0, connections = 0;
            var service = new CommunicationService(() => new[] { "FAKE" }, _ => Interlocked.Increment(ref opens) == 1 ? first : second, new InlineContext());
            using (var reconnected = new ManualResetEventSlim())
            using (var disconnected = new ManualResetEventSlim())
            {
                service.OnDeviceConnected += () =>
                {
                    if (Interlocked.Increment(ref connections) == 1) first.Inject(0xff);
                    else reconnected.Set();
                };
                service.OnDeviceDisconnected += () => disconnected.Set();
                try
                {
                    service.Start();
                    Check(reconnected.Wait(2000), "device error did not reconnect");
                    Check(disconnected.IsSet && !first.IsOpen, "previous connection was not closed and reported");
                }
                finally { service.Stop(); }
                Check(!second.IsOpen, "Stop left serial port open");
            }
        });
        Test("partial device payload reconnects without publishing damaged data", () =>
        {
            var first = new FakeSerial();
            int opens = 0, connections = 0, received = 0;
            var service = new CommunicationService(() => new[] { "FAKE" }, _ => Interlocked.Increment(ref opens) == 1 ? first : new FakeSerial(), new InlineContext());
            using (var reconnected = new ManualResetEventSlim())
            {
                service.OnMessageRecieved += (_, __) => Interlocked.Increment(ref received);
                service.OnDeviceConnected += () =>
                {
                    if (Interlocked.Increment(ref connections) == 1) first.Inject((byte)Command.SESSION_INFO, 1, 2);
                    else reconnected.Set();
                };
                try { service.Start(); Check(reconnected.Wait(2000), "partial frame did not reconnect"); Check(received == 0, "partial frame reached the application"); }
                finally { service.Stop(); }
            }
        });
        Test("service restarts cleanly after suspension", () =>
        {
            var ports = new List<FakeSerial>();
            var service = new CommunicationService(() => new[] { "FAKE" }, _ => { var port = new FakeSerial(); ports.Add(port); return port; }, new InlineContext());
            using (var connected = new AutoResetEvent(false))
            {
                service.OnDeviceConnected += () => connected.Set();
                try
                {
                    service.Start(); Check(connected.WaitOne(1500), "first connection failed");
                    service.Stop();
                    Check(!(bool)Get(service, "m_DeviceConnected") && !(bool)Get(service, "m_DeviceReady"), "stale connection flags after stop");
                    service.Start(); Check(connected.WaitOne(1500), "resume did not reconnect");
                }
                finally { service.Stop(); }
                Check(ports.Count == 2 && !ports[0].IsOpen && !ports[1].IsOpen, "serial resources not released across suspend/resume");
            }
        });
        Test("firmware tool owns USB until it closes, including power resume", () =>
        {
            var ports = new List<FakeSerial>();
            var service = new CommunicationService(() => new[] { "FAKE" }, _ => { var port = new FakeSerial(); ports.Add(port); return port; }, new InlineContext());
            using (var connected = new AutoResetEvent(false))
            {
                service.OnDeviceConnected += () => connected.Set();
                try
                {
                    service.Start(); Check(connected.WaitOne(1500), "initial connection failed");
                    service.PauseForFirmware();
                    Check(!ports[0].IsOpen, "port still owned during firmware upload");
                    service.Start();
                    Check(Get(service, "m_Thread") == null, "power resume reopened USB while flashing");
                    service.ResumeAfterFirmware(); Check(connected.WaitOne(1500), "connection was not restored");
                    Check(ports.Count == 2, "unexpected serial workers after firmware tool closed");
                }
                finally { service.Stop(); }
            }
        });
        return failures == 0 ? 0 : 1;
    }

    static int Preview()
    {

        var app = new MaxMix.App();
        app.InitializeComponent(); // Load the real compiled resources without running App startup.
        app.Startup -= (StartupEventHandler)Delegate.CreateDelegate(typeof(StartupEventHandler), app,
            typeof(MaxMix.App).GetMethod("ApplicationStartup", BindingFlags.NonPublic | BindingFlags.Instance));
        var errors = new StringWriter();
        System.Diagnostics.PresentationTraceSources.DataBindingSource.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(errors));
        System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Error;
        var vm = new MainViewModel { IsActive = true };
        var window = new MaxMix.MainWindow { DataContext = vm, Title = "Maxmix 1.5.9" };
        window.ShowActivated = false;
        window.ShowInTaskbar = false;
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = -10000; window.Top = -10000;
        window.WindowTransitionsEnabled = false;
        window.Show();
        foreach (int width in new[] { 940, 820 })
        {
            window.Width = width;
            window.Height = width == 820 ? 680 : 860;
            window.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(() => { }));
            window.UpdateLayout();
            Check(((TextBlock)window.FindName("ConnectionStatusText")).Text == vm.ConnectionStatus, "connection status binding failed");
            var image = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            image.Render(window);
            var png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(image));
            string path = Path.GetFullPath("bin/preview-" + width + ".png");
            using (var file = File.Create(path)) png.Save(file);
            Console.WriteLine("Preview: " + path);
        }
        window.taskbarIcon.Dispose();
        MaxMix.MainWindow.SetCloseCommand(window, null);
        window.Close();
        Check(errors.ToString().Length == 0, errors.ToString());
        Console.WriteLine("PASS real WPF window and bindings at default and minimum size");
        return 0;
    }

    class InlineContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback callback, object state) => callback(state);
    }

    class FakeSerial : ISerialConnection
    {
        public readonly ConcurrentQueue<byte[]> Writes = new ConcurrentQueue<byte[]>();
        readonly BlockingCollection<byte> incoming = new BlockingCollection<byte>();
        public int BootTimeouts;
        public bool IsOpen { get; private set; }
        public int BytesToRead => incoming.Count;
        public int ReadTimeout { get; set; }
        public int WriteTimeout { get; set; }
        public void Open() { IsOpen = true; }
        public void Close() { IsOpen = false; }
        public void Dispose() { }
        public void DiscardInBuffer() { while (incoming.TryTake(out _)) { } }
        public void DiscardOutBuffer() { }
        public void Inject(params byte[] values) { foreach (var value in values) incoming.Add(value); }
        public int ReadByte()
        {
            if (BootTimeouts > 0) { BootTimeouts--; throw new TimeoutException("booting"); }
            if (!incoming.TryTake(out byte value, Math.Max(1, ReadTimeout))) throw new TimeoutException();
            return value;
        }
        public string ReadLine() => "1.5.6\r";
        public int Read(byte[] buffer, int offset, int count)
        {
            int read = 0;
            while (read < count && incoming.TryTake(out byte value, Math.Max(1, ReadTimeout))) buffer[offset + read++] = value;
            if (read == 0) throw new TimeoutException();
            return read;
        }
        public void Write(byte[] buffer, int offset, int count)
        {
            var copy = new byte[count];
            Array.Copy(buffer, offset, copy, 0, count);
            Writes.Enqueue(copy);
            if (copy[0] == (byte)Command.TEST) incoming.Add((byte)Command.TEST);
            incoming.Add((byte)Command.OK);
        }
    }
}
