using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32.SafeHandles;


[assembly: AssemblyTitle("Legacy Aura Control")]
[assembly: AssemblyDescription("Standalone RGB control for legacy ASUS ROG Aura Core keyboards")]
[assembly: AssemblyCompany("Legacy Aura Control Community")]
[assembly: AssemblyProduct("Legacy Aura Control")]
[assembly: AssemblyCopyright("Copyright 2026")]
[assembly: AssemblyVersion("1.2.0.0")]
[assembly: AssemblyFileVersion("1.2.0.0")]

namespace LegacyAuraControl
{
    public class HidDeviceInfo
    {
        public string Path;
        public ushort VendorId;
        public ushort ProductId;
        public ushort VersionNumber;
        public int FeatureReportLength;
        public int OutputReportLength;
        public int InputReportLength;
        public ushort UsagePage;
        public ushort Usage;

        public override string ToString()
        {
            return string.Format(
                "ASUS Aura Core  VID_{0:X4}&PID_{1:X4}  | OUT {2}  FEATURE {3}",
                VendorId, ProductId, OutputReportLength, FeatureReportLength);
        }
    }

    public static class AuraHid
    {
        private const uint DIGCF_PRESENT = 0x00000002;
        private const uint DIGCF_DEVICEINTERFACE = 0x00000010;
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;

        public const ushort ASUS_VID = 0x0B05;
        private static readonly ushort[] SupportedPids = new ushort[] { 0x1854, 0x1866, 0x1869 };

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public int Flags;
            public UIntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HIDD_ATTRIBUTES
        {
            public int Size;
            public ushort VendorID;
            public ushort ProductID;
            public ushort VersionNumber;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HIDP_CAPS
        {
            public ushort Usage;
            public ushort UsagePage;
            public ushort InputReportByteLength;
            public ushort OutputReportByteLength;
            public ushort FeatureReportByteLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            public ushort[] Reserved;
            public ushort NumberLinkCollectionNodes;
            public ushort NumberInputButtonCaps;
            public ushort NumberInputValueCaps;
            public ushort NumberInputDataIndices;
            public ushort NumberOutputButtonCaps;
            public ushort NumberOutputValueCaps;
            public ushort NumberOutputDataIndices;
            public ushort NumberFeatureButtonCaps;
            public ushort NumberFeatureValueCaps;
            public ushort NumberFeatureDataIndices;
        }

        [DllImport("hid.dll")]
        private static extern void HidD_GetHidGuid(out Guid HidGuid);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_GetAttributes(SafeFileHandle HidDeviceObject, ref HIDD_ATTRIBUTES Attributes);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_SetFeature(SafeFileHandle HidDeviceObject, byte[] ReportBuffer, int ReportBufferLength);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_GetPreparsedData(SafeFileHandle HidDeviceObject, out IntPtr PreparsedData);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_FreePreparsedData(IntPtr PreparsedData);

        [DllImport("hid.dll")]
        private static extern int HidP_GetCaps(IntPtr PreparsedData, out HIDP_CAPS Capabilities);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, uint Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, IntPtr DeviceInfoData, ref Guid InterfaceClassGuid, uint MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, int DeviceInterfaceDetailDataSize, out int RequiredSize, IntPtr DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteFile(SafeFileHandle hFile, byte[] lpBuffer, int nNumberOfBytesToWrite, out int lpNumberOfBytesWritten, IntPtr lpOverlapped);

        private static bool IsSupportedPid(ushort pid)
        {
            for (int i = 0; i < SupportedPids.Length; i++)
                if (SupportedPids[i] == pid) return true;
            return false;
        }

        public static List<HidDeviceInfo> FindSupportedDevices()
        {
            List<HidDeviceInfo> result = new List<HidDeviceInfo>();
            Guid hidGuid;
            HidD_GetHidGuid(out hidGuid);
            IntPtr infoSet = SetupDiGetClassDevs(ref hidGuid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
            if (infoSet == new IntPtr(-1)) return result;

            try
            {
                uint index = 0;
                while (true)
                {
                    SP_DEVICE_INTERFACE_DATA ifData = new SP_DEVICE_INTERFACE_DATA();
                    ifData.cbSize = Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA));
                    bool ok = SetupDiEnumDeviceInterfaces(infoSet, IntPtr.Zero, ref hidGuid, index, ref ifData);
                    if (!ok)
                    {
                        int err = Marshal.GetLastWin32Error();
                        if (err == 259) break; // ERROR_NO_MORE_ITEMS
                        index++;
                        continue;
                    }

                    int required;
                    SetupDiGetDeviceInterfaceDetail(infoSet, ref ifData, IntPtr.Zero, 0, out required, IntPtr.Zero);
                    IntPtr detail = Marshal.AllocHGlobal(required);
                    try
                    {
                        Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 4 + Marshal.SystemDefaultCharSize);
                        if (SetupDiGetDeviceInterfaceDetail(infoSet, ref ifData, detail, required, out required, IntPtr.Zero))
                        {
                            // DevicePath begins immediately after the DWORD cbSize field.
                            IntPtr pPath = new IntPtr(detail.ToInt64() + 4);
                            string path = Marshal.PtrToStringAuto(pPath);

                            if (!string.IsNullOrEmpty(path))
                            {
                                using (SafeFileHandle handle = CreateFile(path, 0, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero))
                                {
                                    if (!handle.IsInvalid)
                                    {
                                        HIDD_ATTRIBUTES attrs = new HIDD_ATTRIBUTES();
                                        attrs.Size = Marshal.SizeOf(typeof(HIDD_ATTRIBUTES));

                                        if (HidD_GetAttributes(handle, ref attrs) &&
                                            attrs.VendorID == ASUS_VID &&
                                            IsSupportedPid(attrs.ProductID))
                                        {
                                            int featureLen = 0;
                                            int outputLen = 0;
                                            int inputLen = 0;
                                            ushort usage = 0;
                                            ushort usagePage = 0;

                                            IntPtr prep;
                                            if (HidD_GetPreparsedData(handle, out prep))
                                            {
                                                try
                                                {
                                                    HIDP_CAPS caps;
                                                    int status = HidP_GetCaps(prep, out caps);
                                                    if (status >= 0)
                                                    {
                                                        featureLen = caps.FeatureReportByteLength;
                                                        outputLen = caps.OutputReportByteLength;
                                                        inputLen = caps.InputReportByteLength;
                                                        usage = caps.Usage;
                                                        usagePage = caps.UsagePage;
                                                    }
                                                }
                                                finally { HidD_FreePreparsedData(prep); }
                                            }

                                            // Keep only interfaces that can plausibly carry an Aura report.
                                            if (featureLen >= 17 || outputLen >= 17)
                                            {
                                                HidDeviceInfo d = new HidDeviceInfo();
                                                d.Path = path;
                                                d.VendorId = attrs.VendorID;
                                                d.ProductId = attrs.ProductID;
                                                d.VersionNumber = attrs.VersionNumber;
                                                d.FeatureReportLength = featureLen;
                                                d.OutputReportLength = outputLen;
                                                d.InputReportLength = inputLen;
                                                d.Usage = usage;
                                                d.UsagePage = usagePage;
                                                result.Add(d);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    finally { Marshal.FreeHGlobal(detail); }

                    index++;
                }
            }
            finally { SetupDiDestroyDeviceInfoList(infoSet); }

            // On these ASUS keyboards the Aura interface normally exposes an OUTPUT
            // report. Put those interfaces first instead of selecting an arbitrary
            // HID collection with the same VID/PID.
            result.Sort(delegate(HidDeviceInfo a, HidDeviceInfo b)
            {
                int ao = a.OutputReportLength >= 17 ? 1 : 0;
                int bo = b.OutputReportLength >= 17 ? 1 : 0;
                if (ao != bo) return bo.CompareTo(ao);
                if (a.OutputReportLength != b.OutputReportLength)
                    return b.OutputReportLength.CompareTo(a.OutputReportLength);
                return b.FeatureReportLength.CompareTo(a.FeatureReportLength);
            });

            return result;
        }

        private static SafeFileHandle OpenForWrite(string path)
        {
            SafeFileHandle handle = CreateFile(
                path,
                GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                0,
                IntPtr.Zero);

            if (handle.IsInvalid)
            {
                handle.Close();
                handle = CreateFile(
                    path,
                    GENERIC_WRITE,
                    FILE_SHARE_READ | FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    OPEN_EXISTING,
                    0,
                    IntPtr.Zero);
            }

            return handle;
        }

        private static void SendAuraReport(SafeFileHandle handle, HidDeviceInfo d, byte[] data)
        {
            Exception outputError = null;
            Exception featureError = null;

            // ASUS Aura Core under Windows normally uses HID OUTPUT reports for 0x5D.
            if (d.OutputReportLength >= 17)
            {
                try
                {
                    byte[] report = new byte[d.OutputReportLength];
                    Array.Copy(data, report, Math.Min(data.Length, report.Length));

                    int written;
                    bool ok = WriteFile(handle, report, report.Length, out written, IntPtr.Zero);
                    if (!ok)
                        throw new Win32Exception(Marshal.GetLastWin32Error(), "WriteFile/OUTPUT report failed");
                    if (written != report.Length)
                        throw new IOException("OUTPUT Report unvollständig geschrieben (" + written.ToString() + "/" + report.Length.ToString() + " Bytes).");

                    return;
                }
                catch (Exception ex) { outputError = ex; }
            }

            // Some older firmware/collections accept the same packet as a Feature report.
            if (d.FeatureReportLength >= 17)
            {
                try
                {
                    byte[] report = new byte[d.FeatureReportLength];
                    Array.Copy(data, report, Math.Min(data.Length, report.Length));

                    if (!HidD_SetFeature(handle, report, report.Length))
                        throw new Win32Exception(Marshal.GetLastWin32Error(), "HidD_SetFeature failed");

                    return;
                }
                catch (Exception ex) { featureError = ex; }
            }

            string msg = "This HID interface does not accept the Aura report.";
            if (outputError != null) msg += " OUTPUT: " + outputError.Message + ".";
            if (featureError != null) msg += " FEATURE: " + featureError.Message + ".";
            throw new IOException(msg);
        }

        private static byte SpeedByte(int speedIndex)
        {
            if (speedIndex <= 0) return 0xE1;
            if (speedIndex >= 2) return 0xF5;
            return 0xEB;
        }

        private static void AuraHandshake(SafeFileHandle handle, HidDeviceInfo d)
        {
            // "Unlock"/standard-mode handshake used by the legacy Aura controller.
            SendAuraReport(handle, d, new byte[] { 0x5D, 0xB9 });
            Thread.Sleep(5);

            SendAuraReport(handle, d, new byte[] {
                0x5D, 0x41, 0x53, 0x55, 0x53, 0x20, 0x54, 0x65,
                0x63, 0x68, 0x2E, 0x49, 0x6E, 0x63, 0x2E
            });
            Thread.Sleep(5);

            SendAuraReport(handle, d, new byte[] { 0x5D, 0x05, 0x20, 0x31, 0x00, 0x20 });
            Thread.Sleep(10);
        }

        private static List<HidDeviceInfo> CandidateInterfaces(HidDeviceInfo selected)
        {
            List<HidDeviceInfo> all = FindSupportedDevices();
            List<HidDeviceInfo> candidates = new List<HidDeviceInfo>();

            // Try the selected path first.
            if (selected != null) candidates.Add(selected);

            for (int i = 0; i < all.Count; i++)
            {
                HidDeviceInfo d = all[i];
                if (selected != null && string.Equals(d.Path, selected.Path, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (selected == null || d.ProductId == selected.ProductId)
                    candidates.Add(d);
            }

            return candidates;
        }

        public static void Initialize(HidDeviceInfo selected)
        {
            List<HidDeviceInfo> candidates = CandidateInterfaces(selected);
            string errors = "";

            for (int i = 0; i < candidates.Count; i++)
            {
                HidDeviceInfo d = candidates[i];
                try
                {
                    using (SafeFileHandle handle = OpenForWrite(d.Path))
                    {
                        if (handle.IsInvalid)
                            throw new Win32Exception(Marshal.GetLastWin32Error(), "Aura HID could not be opened.");

                        AuraHandshake(handle, d);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    errors += string.Format(
                        "\r\nPID_{0:X4} OUT={1} FEATURE={2}: {3}",
                        d.ProductId, d.OutputReportLength, d.FeatureReportLength, ex.Message);
                }
            }

            throw new IOException("None of the detected ASUS HID interfaces accepted the Aura command." + errors);
        }

        private static void SendFeatureReport(SafeFileHandle handle, HidDeviceInfo d, byte[] data)
        {
            if (d.FeatureReportLength <= 0)
                throw new IOException("This HID interface has no feature report.");

            if (data.Length > d.FeatureReportLength)
                throw new IOException("Feature-Report ist kleiner als der zu sendende Befehl.");

            byte[] report = new byte[d.FeatureReportLength];
            Array.Copy(data, report, data.Length);

            if (!HidD_SetFeature(handle, report, report.Length))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "HidD_SetFeature failed");
        }

        private static void BrightnessHandshake(SafeFileHandle handle, HidDeviceInfo d)
        {
            // Legacy ASUS keyboard handshake: "ASUS Tech.Inc."
            SendFeatureReport(handle, d, new byte[] {
                0x5A, 0x41, 0x53, 0x55, 0x53, 0x20, 0x54, 0x65,
                0x63, 0x68, 0x2E, 0x49, 0x6E, 0x63, 0x2E, 0x00
            });
            Thread.Sleep(5);
        }

        public static void SetBrightness(HidDeviceInfo selected, int level)
        {
            if (level < 0) level = 0;
            if (level > 3) level = 3;

            List<HidDeviceInfo> candidates = CandidateInterfaces(selected);
            string errors = "";

            for (int i = 0; i < candidates.Count; i++)
            {
                HidDeviceInfo d = candidates[i];
                if (d.FeatureReportLength < 17) continue;

                try
                {
                    using (SafeFileHandle handle = OpenForWrite(d.Path))
                    {
                        if (handle.IsInvalid)
                            throw new Win32Exception(Marshal.GetLastWin32Error(), "HID interface could not be opened.");

                        BrightnessHandshake(handle, d);
                        SendFeatureReport(handle, d, new byte[] {
                            0x5A, 0xBA, 0xC5, 0xC4, (byte)level
                        });
                        return;
                    }
                }
                catch (Exception ex)
                {
                    errors += string.Format(
                        "\r\nPID_{0:X4} FEATURE={1}: {2}",
                        d.ProductId, d.FeatureReportLength, ex.Message);
                }
            }

            throw new IOException("Brightness could not be sent to any compatible ASUS HID interface." + errors);
        }

        private static void SendSingleEffect(
            SafeFileHandle handle, HidDeviceInfo d, int hwEffect, byte speed,
            Color color1, Color color2)
        {
            byte[] m = new byte[17];
            m[0] = 0x5D;
            m[1] = 0xB3;
            m[2] = 0;
            m[3] = (byte)hwEffect;
            m[4] = color1.R;
            m[5] = color1.G;
            m[6] = color1.B;
            m[7] = speed;

            if (hwEffect == 1)
            {
                m[9] = 1;
                m[10] = color2.R;
                m[11] = color2.G;
                m[12] = color2.B;
            }

            if (hwEffect == 2 || hwEffect == 3)
            {
                // Aura firmware expects the enable byte here for cycle/rainbow effects.
                m[4] = 0xFF;
            }

            SendAuraReport(handle, d, m);
        }

        private static void SendFourZoneEffect(
            SafeFileHandle handle, HidDeviceInfo d, int hwEffect, byte speed, Color[] colors)
        {
            for (int i = 0; i < 4; i++)
            {
                byte[] m = new byte[17];
                m[0] = 0x5D;
                m[1] = 0xB3;
                m[2] = (byte)(i + 1);
                m[3] = (byte)hwEffect;
                m[4] = colors[i].R;
                m[5] = colors[i].G;
                m[6] = colors[i].B;
                m[7] = speed;
                SendAuraReport(handle, d, m);
            }
        }

        private static void ApplyOnInterface(
            HidDeviceInfo d, int effect, int speedIndex, bool fourZones,
            Color[] colors, Color breathingSecond)
        {
            using (SafeFileHandle handle = OpenForWrite(d.Path))
            {
                if (handle.IsInvalid)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Aura HID could not be opened.");

                AuraHandshake(handle, d);
                byte speed = SpeedByte(speedIndex);

                // UI effect mapping:
                // 0 = Static
                // 1 = Breathing
                // 2 = Color Cycle
                // 3 = Rainbow Cycle
                // 4 = Rainbow Zones
                if (effect == 4)
                {
                    Color[] rainbow = new Color[] {
                        Color.FromArgb(255, 0, 0),
                        Color.FromArgb(255, 255, 0),
                        Color.FromArgb(0, 255, 255),
                        Color.FromArgb(255, 0, 255)
                    };
                    SendFourZoneEffect(handle, d, 0, speed, rainbow);
                }
                else if (effect == 2)
                {
                    SendSingleEffect(handle, d, 2, speed, colors[0], breathingSecond);
                }
                else if (effect == 3)
                {
                    SendSingleEffect(handle, d, 3, speed, colors[0], breathingSecond);
                }
                else if (fourZones)
                {
                    SendFourZoneEffect(handle, d, effect, speed, colors);
                }
                else
                {
                    SendSingleEffect(handle, d, effect, speed, colors[0], breathingSecond);
                }

                SendAuraReport(handle, d, new byte[] { 0x5D, 0xB5 });
                Thread.Sleep(5);
                SendAuraReport(handle, d, new byte[] { 0x5D, 0xB4 });
            }
        }

        public static void Apply(
            HidDeviceInfo selected, int effect, int speedIndex, bool fourZones,
            Color[] colors, Color breathingSecond)
        {
            List<HidDeviceInfo> candidates = CandidateInterfaces(selected);
            string errors = "";

            for (int i = 0; i < candidates.Count; i++)
            {
                HidDeviceInfo d = candidates[i];
                try
                {
                    ApplyOnInterface(d, effect, speedIndex, fourZones, colors, breathingSecond);
                    return;
                }
                catch (Exception ex)
                {
                    errors += string.Format(
                        "\r\nPID_{0:X4} OUT={1} FEATURE={2}: {3}",
                        d.ProductId, d.OutputReportLength, d.FeatureReportLength, ex.Message);
                }
            }

            throw new IOException(
                "Aura device detected, but no HID interface accepted the color commands." +
                errors);
        }
    }

    public class MainForm : Form
    {
        private ComboBox deviceBox;
        private ComboBox effectBox;
        private ComboBox speedBox;
        private ComboBox brightnessBox;
        private CheckBox fourZoneCheck;
        private CheckBox autoApplyCheck;
        private Button[] zoneButtons = new Button[4];
        private Panel[] zonePanels = new Panel[4];
        private Button breathSecondButton;
        private Panel breathSecondPanel;
        private Button applyButton;
        private Button refreshButton;
        private Button initializeButton;
        private Button aboutButton;
        private Button copyDiagButton;
        private Label statusLabel;
        private Label modeHintLabel;
        private Color[] colors = new Color[] { Color.Red, Color.Red, Color.Red, Color.Red };
        private Color breathSecond = Color.Black;
        private List<HidDeviceInfo> devices = new List<HidDeviceInfo>();
        private string settingsPath;
        private string startupPath;
        private bool loadingSettings;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private bool reallyExit;
        private bool startingHidden;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        private Icon LoadAppIcon()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string[] icoPaths = new string[] {
                    Path.Combine(baseDir, "LegacyAura.ico"),
                    Path.Combine(baseDir, "assets", "LegacyAura.ico")
                };

                for (int i = 0; i < icoPaths.Length; i++)
                {
                    if (File.Exists(icoPaths[i]))
                        return new Icon(icoPaths[i]);
                }

                string[] pngPaths = new string[] {
                    Path.Combine(baseDir, "LegacyAura.png"),
                    Path.Combine(baseDir, "assets", "LegacyAura.png")
                };

                for (int i = 0; i < pngPaths.Length; i++)
                {
                    if (!File.Exists(pngPaths[i])) continue;

                    using (Bitmap bmp = new Bitmap(pngPaths[i]))
                    {
                        IntPtr hIcon = bmp.GetHicon();
                        try
                        {
                            using (Icon temp = Icon.FromHandle(hIcon))
                                return (Icon)temp.Clone();
                        }
                        finally
                        {
                            DestroyIcon(hIcon);
                        }
                    }
                }

                return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                try { return Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
                catch { return null; }
            }
        }

        public MainForm()
        {
            // Use the icon embedded into LegacyAura.exe for the window/title bar
            // and the taskbar. WinForms otherwise falls back to the generic app icon.
            try
            {
                Icon appIcon = LoadAppIcon();
                if (appIcon != null)
                {
                    this.Icon = appIcon;
                    this.ShowIcon = true;
                }
            }
            catch { }

            settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LegacyAuraControl", "settings.ini");

            startupPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                "LegacyAuraControl.cmd");

            BuildTray();
            BuildUi();
            LoadSettings();
            RefreshDevices();
            UpdateModeUi();
        }

        private Label MakeLabel(string text, int x, int y, int w)
        {
            Label l = new Label();
            l.Text = text;
            l.Left = x;
            l.Top = y;
            l.Width = w;
            l.Height = 22;
            l.ForeColor = Color.Gainsboro;
            l.BackColor = Color.Transparent;
            return l;
        }

        private Button MakeDarkButton(string text, int x, int y, int w, int h)
        {
            Button b = new Button();
            b.Text = text;
            b.Left = x;
            b.Top = y;
            b.Width = w;
            b.Height = h;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 95);
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(58, 58, 64);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(68, 68, 74);
            b.BackColor = Color.FromArgb(43, 43, 47);
            b.ForeColor = Color.White;
            return b;
        }

        private GroupBox MakeGroup(string title, int x, int y, int w, int h)
        {
            GroupBox g = new GroupBox();
            g.Text = title;
            g.Left = x;
            g.Top = y;
            g.Width = w;
            g.Height = h;
            g.ForeColor = Color.Gainsboro;
            g.BackColor = Color.Transparent;
            return g;
        }

        private void BuildTray()
        {
            trayMenu = new ContextMenuStrip();

            ToolStripMenuItem openItem = new ToolStripMenuItem("Open");
            openItem.Font = new Font(openItem.Font, FontStyle.Bold);
            openItem.Click += delegate { ShowFromTray(); };

            ToolStripMenuItem applyItem = new ToolStripMenuItem("Apply last setting");
            applyItem.Click += delegate {
                if (!Visible) ShowInTaskbar = false;
                ApplySavedFromTray();
            };

            ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += delegate {
                reallyExit = true;
                if (trayIcon != null) trayIcon.Visible = false;
                Close();
            };

            trayMenu.Items.Add(openItem);
            trayMenu.Items.Add(applyItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(exitItem);

            trayIcon = new NotifyIcon();
            trayIcon.Text = "Legacy Aura Control";
            trayIcon.ContextMenuStrip = trayMenu;

            try
            {
                Icon appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (appIcon != null) trayIcon.Icon = appIcon;
            }
            catch { }

            trayIcon.DoubleClick += delegate { ShowFromTray(); };
            trayIcon.Visible = true;
        }

        private void ShowFromTray()
        {
            Show();
            ShowInTaskbar = true;
            WindowState = FormWindowState.Normal;
            Activate();
            BringToFront();
        }

        private void HideToTray(bool showBalloon)
        {
            Hide();
            ShowInTaskbar = false;

            if (showBalloon && trayIcon != null)
            {
                trayIcon.BalloonTipTitle = "Legacy Aura Control";
                trayIcon.BalloonTipText = "Legacy Aura Control is still running in the notification area.";
                trayIcon.BalloonTipIcon = ToolTipIcon.Info;
                trayIcon.ShowBalloonTip(1800);
            }
        }

        private void ApplySavedFromTray()
        {
            try
            {
                RefreshDevices();
                if (devices.Count == 0)
                {
                    if (trayIcon != null)
                    {
                        trayIcon.BalloonTipTitle = "Legacy Aura Control";
                        trayIcon.BalloonTipText = "No compatible Aura device found.";
                        trayIcon.BalloonTipIcon = ToolTipIcon.Warning;
                        trayIcon.ShowBalloonTip(2200);
                    }
                    return;
                }

                HidDeviceInfo d = devices[0];
                AuraHid.Apply(
                    d,
                    effectBox.SelectedIndex,
                    speedBox.SelectedIndex,
                    fourZoneCheck.Checked,
                    colors,
                    breathSecond);

                AuraHid.SetBrightness(
                    d,
                    brightnessBox.SelectedIndex < 0 ? 3 : brightnessBox.SelectedIndex);

                if (trayIcon != null)
                {
                    trayIcon.BalloonTipTitle = "Legacy Aura Control";
                    trayIcon.BalloonTipText = "Last lighting setting applied.";
                    trayIcon.BalloonTipIcon = ToolTipIcon.Info;
                    trayIcon.ShowBalloonTip(1600);
                }
            }
            catch (Exception ex)
            {
                if (trayIcon != null)
                {
                    trayIcon.BalloonTipTitle = "Legacy Aura Control";
                    trayIcon.BalloonTipText = "Apply failed: " + ex.Message;
                    trayIcon.BalloonTipIcon = ToolTipIcon.Error;
                    trayIcon.ShowBalloonTip(3000);
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (WindowState == FormWindowState.Minimized)
                HideToTray(false);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!reallyExit && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideToTray(true);
                return;
            }

            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
                trayIcon = null;
            }

            base.OnFormClosing(e);
        }

        private void BuildUi()
        {
            Text = "Legacy Aura Control 1.2.0";
            Width = 900;
            Height = 665;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(22, 22, 25);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            Panel accent = new Panel();
            accent.Left = 0;
            accent.Top = 0;
            accent.Width = 8;
            accent.Height = 665;
            accent.BackColor = Color.FromArgb(196, 31, 47);
            Controls.Add(accent);

            Label title = new Label();
            title.Text = "LEGACY AURA CONTROL";
            title.Left = 26;
            title.Top = 18;
            title.Width = 430;
            title.Height = 36;
            title.Font = new Font("Segoe UI Semibold", 19F, FontStyle.Bold);
            title.ForeColor = Color.White;
            Controls.Add(title);

            Label version = MakeLabel("1.2  •  Standalone Aura Core replacement", 29, 55, 420);
            version.ForeColor = Color.FromArgb(165, 165, 170);
            Controls.Add(version);

            aboutButton = MakeDarkButton("About", 788, 23, 76, 30);
            aboutButton.Click += delegate { ShowAbout(); };
            Controls.Add(aboutButton);

            GroupBox devGroup = MakeGroup("Aura Core device", 24, 88, 840, 88);
            Controls.Add(devGroup);

            deviceBox = new ComboBox();
            deviceBox.Left = 16;
            deviceBox.Top = 32;
            deviceBox.Width = 550;
            deviceBox.DropDownStyle = ComboBoxStyle.DropDownList;
            devGroup.Controls.Add(deviceBox);

            refreshButton = MakeDarkButton("Scan", 579, 30, 102, 30);
            refreshButton.Click += delegate { RefreshDevices(); };
            devGroup.Controls.Add(refreshButton);

            initializeButton = MakeDarkButton("Initialize", 690, 30, 126, 30);
            initializeButton.Click += delegate { InitializeKeyboard(); };
            devGroup.Controls.Add(initializeButton);

            GroupBox effectGroup = MakeGroup("Lighting", 24, 190, 312, 245);
            Controls.Add(effectGroup);

            effectGroup.Controls.Add(MakeLabel("Effect", 18, 31, 85));
            effectBox = new ComboBox();
            effectBox.Left = 112;
            effectBox.Top = 28;
            effectBox.Width = 175;
            effectBox.DropDownStyle = ComboBoxStyle.DropDownList;
            effectBox.Items.AddRange(new object[] {
                "Static",
                "Breathing",
                "Color cycle",
                "Rainbow cycle",
                "Rainbow (4 zones)"
            });
            effectBox.SelectedIndex = 0;
            effectBox.SelectedIndexChanged += delegate { UpdateModeUi(); };
            effectGroup.Controls.Add(effectBox);

            effectGroup.Controls.Add(MakeLabel("Speed", 18, 72, 85));
            speedBox = new ComboBox();
            speedBox.Left = 112;
            speedBox.Top = 69;
            speedBox.Width = 175;
            speedBox.DropDownStyle = ComboBoxStyle.DropDownList;
            speedBox.Items.AddRange(new object[] { "Slow", "Normal", "Fast" });
            speedBox.SelectedIndex = 1;
            effectGroup.Controls.Add(speedBox);

            effectGroup.Controls.Add(MakeLabel("Brightness", 18, 113, 85));
            brightnessBox = new ComboBox();
            brightnessBox.Left = 112;
            brightnessBox.Top = 110;
            brightnessBox.Width = 175;
            brightnessBox.DropDownStyle = ComboBoxStyle.DropDownList;
            brightnessBox.Items.AddRange(new object[] {
                "Off (0)",
                "Low (1)",
                "Medium (2)",
                "High (3)"
            });
            brightnessBox.SelectedIndex = 3;
            effectGroup.Controls.Add(brightnessBox);

            fourZoneCheck = new CheckBox();
            fourZoneCheck.Text = "4-zone mode";
            fourZoneCheck.Left = 19;
            fourZoneCheck.Top = 154;
            fourZoneCheck.Width = 180;
            fourZoneCheck.ForeColor = Color.White;
            fourZoneCheck.CheckedChanged += delegate { UpdateModeUi(); };
            effectGroup.Controls.Add(fourZoneCheck);

            modeHintLabel = MakeLabel(
                "Static and Breathing can be controlled as one zone or four zones.",
                18, 184, 270);
            modeHintLabel.Height = 42;
            modeHintLabel.ForeColor = Color.FromArgb(145, 145, 150);
            effectGroup.Controls.Add(modeHintLabel);

            GroupBox colorsGroup = MakeGroup("Colors", 352, 190, 512, 300);
            Controls.Add(colorsGroup);

            for (int i = 0; i < 4; i++)
            {
                int y = 32 + i * 50;
                colorsGroup.Controls.Add(MakeLabel("Zone " + (i + 1).ToString(), 18, y + 5, 70));

                zonePanels[i] = new Panel();
                zonePanels[i].Left = 92;
                zonePanels[i].Top = y;
                zonePanels[i].Width = 82;
                zonePanels[i].Height = 30;
                zonePanels[i].BackColor = colors[i];
                zonePanels[i].BorderStyle = BorderStyle.FixedSingle;
                colorsGroup.Controls.Add(zonePanels[i]);

                zoneButtons[i] = MakeDarkButton("Choose color", 187, y, 118, 30);
                int ix = i;
                zoneButtons[i].Click += delegate { ChooseZoneColor(ix); };
                colorsGroup.Controls.Add(zoneButtons[i]);
            }

            colorsGroup.Controls.Add(MakeLabel("2nd breath color", 330, 37, 145));
            breathSecondPanel = new Panel();
            breathSecondPanel.Left = 342;
            breathSecondPanel.Top = 69;
            breathSecondPanel.Width = 82;
            breathSecondPanel.Height = 30;
            breathSecondPanel.BackColor = breathSecond;
            breathSecondPanel.BorderStyle = BorderStyle.FixedSingle;
            colorsGroup.Controls.Add(breathSecondPanel);

            breathSecondButton = MakeDarkButton("Choose color", 326, 111, 118, 30);
            breathSecondButton.Click += delegate { ChooseBreathSecond(); };
            colorsGroup.Controls.Add(breathSecondButton);

            Label hint = MakeLabel(
                "The second color is only used by Breathing in single-zone mode.",
                324, 153, 165);
            hint.Height = 78;
            hint.ForeColor = Color.FromArgb(145, 145, 150);
            colorsGroup.Controls.Add(hint);

            applyButton = new Button();
            applyButton.Text = "APPLY";
            applyButton.Left = 24;
            applyButton.Top = 452;
            applyButton.Width = 312;
            applyButton.Height = 58;
            applyButton.FlatStyle = FlatStyle.Flat;
            applyButton.FlatAppearance.BorderSize = 0;
            applyButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(218, 42, 59);
            applyButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(170, 25, 39);
            applyButton.BackColor = Color.FromArgb(196, 31, 47);
            applyButton.ForeColor = Color.White;
            applyButton.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            applyButton.Click += delegate { ApplyCurrent(); };
            Controls.Add(applyButton);

            autoApplyCheck = new CheckBox();
            autoApplyCheck.Text = "Apply last setting automatically at Windows login";
            autoApplyCheck.Left = 24;
            autoApplyCheck.Top = 525;
            autoApplyCheck.Width = 430;
            autoApplyCheck.ForeColor = Color.White;
            autoApplyCheck.CheckedChanged += delegate {
                if (!loadingSettings) UpdateStartup();
            };
            Controls.Add(autoApplyCheck);

            copyDiagButton = MakeDarkButton("Copy diagnostics", 704, 517, 160, 32);
            copyDiagButton.Click += delegate { CopyDiagnostics(); };
            Controls.Add(copyDiagButton);

            Label note = MakeLabel(
                "No Aura Core, Aura Service or Armoury Crate required. "
                + "Closing the window keeps Legacy Aura Control running in the notification area.",
                24, 558, 840);
            note.Height = 38;
            note.ForeColor = Color.FromArgb(150, 150, 155);
            Controls.Add(note);

            statusLabel = MakeLabel("Ready.", 24, 608, 840);
            statusLabel.Height = 28;
            statusLabel.ForeColor = Color.FromArgb(150, 220, 160);
            Controls.Add(statusLabel);
        }

        private void RefreshDevices()
        {
            try
            {
                devices = AuraHid.FindSupportedDevices();
                deviceBox.Items.Clear();

                for (int i = 0; i < devices.Count; i++)
                    deviceBox.Items.Add(devices[i]);

                if (deviceBox.Items.Count > 0)
                {
                    deviceBox.SelectedIndex = 0;
                    HidDeviceInfo d = devices[0];
                    SetStatus(string.Format(
                        "Ready: VID_{0:X4} PID_{1:X4}  |  OUT {2} B  |  FEATURE {3} B  |  {4} HID interface(s)",
                        d.VendorId, d.ProductId, d.OutputReportLength,
                        d.FeatureReportLength, devices.Count), true);
                }
                else
                {
                    SetStatus(
                        "No supported legacy Aura Core device found "
                        + "(erwartet: ASUS PID 1854 / 1866 / 1869).", false);
                }
            }
            catch (Exception ex)
            {
                SetStatus("Device scan failed: " + ex.Message, false);
            }
        }

        private HidDeviceInfo SelectedDevice()
        {
            if (deviceBox.SelectedIndex < 0 || deviceBox.SelectedIndex >= devices.Count)
                return null;
            return devices[deviceBox.SelectedIndex];
        }

        private void InitializeKeyboard()
        {
            HidDeviceInfo d = SelectedDevice();
            if (d == null)
            {
                SetStatus("No device selected.", false);
                return;
            }

            try
            {
                AuraHid.Initialize(d);
                SetStatus("Aura Core controller initialized.", true);
            }
            catch (Exception ex)
            {
                SetStatus("Initialization failed: " + ex.Message, false);
            }
        }

        private void ApplyCurrent()
        {
            HidDeviceInfo d = SelectedDevice();
            if (d == null)
            {
                SetStatus("No compatible Aura device found.", false);
                return;
            }

            SaveSettings();

            bool colorOk = false;
            bool brightnessOk = false;
            string colorError = "";
            string brightnessError = "";

            try
            {
                int effect = effectBox.SelectedIndex < 0 ? 0 : effectBox.SelectedIndex;
                int speed = speedBox.SelectedIndex < 0 ? 1 : speedBox.SelectedIndex;

                AuraHid.Apply(
                    d, effect, speed, fourZoneCheck.Checked, colors, breathSecond);
                colorOk = true;
            }
            catch (Exception ex)
            {
                colorError = ex.Message;
            }

            try
            {
                int brightness = brightnessBox.SelectedIndex < 0 ? 3 : brightnessBox.SelectedIndex;
                AuraHid.SetBrightness(d, brightness);
                brightnessOk = true;
            }
            catch (Exception ex)
            {
                brightnessError = ex.Message;
            }

            if (colorOk && brightnessOk)
            {
                SetStatus("Color/effect and brightness applied successfully.", true);
            }
            else if (colorOk)
            {
                SetStatus("Color/effect OK. Brightness failed: " + brightnessError, false);
            }
            else if (brightnessOk)
            {
                SetStatus("Brightness OK. Color/effect failed: " + colorError, false);
            }
            else
            {
                SetStatus(
                    "Apply failed. Color: " + colorError
                    + " | Brightness: " + brightnessError, false);
            }
        }

        private void ChooseZoneColor(int index)
        {
            ColorDialog cd = new ColorDialog();
            cd.Color = colors[index];
            cd.FullOpen = true;
            if (cd.ShowDialog(this) == DialogResult.OK)
            {
                colors[index] = cd.Color;
                zonePanels[index].BackColor = cd.Color;
            }
            cd.Dispose();
        }

        private void ChooseBreathSecond()
        {
            ColorDialog cd = new ColorDialog();
            cd.Color = breathSecond;
            cd.FullOpen = true;
            if (cd.ShowDialog(this) == DialogResult.OK)
            {
                breathSecond = cd.Color;
                breathSecondPanel.BackColor = cd.Color;
            }
            cd.Dispose();
        }

        private void UpdateModeUi()
        {
            if (effectBox == null) return;

            int effect = effectBox.SelectedIndex;
            bool staticOrBreathing = effect == 0 || effect == 1;
            bool breathing = effect == 1;
            bool four = fourZoneCheck != null && fourZoneCheck.Checked;

            if (fourZoneCheck != null)
            {
                fourZoneCheck.Enabled = staticOrBreathing;
                if (!staticOrBreathing && fourZoneCheck.Checked)
                    fourZoneCheck.Checked = false;
            }

            for (int i = 0; i < 4; i++)
            {
                bool enabled = staticOrBreathing && (i == 0 || four);
                if (zoneButtons[i] != null) zoneButtons[i].Enabled = enabled;
                if (zonePanels[i] != null) zonePanels[i].Enabled = enabled;
            }

            if (breathSecondButton != null)
                breathSecondButton.Enabled = breathing && !four;
            if (breathSecondPanel != null)
                breathSecondPanel.Enabled = breathing && !four;

            if (speedBox != null)
                speedBox.Enabled = effect != 0 && effect != 4;

            if (modeHintLabel != null)
            {
                if (effect == 4)
                    modeHintLabel.Text = "Fixed rainbow colors across the four keyboard zones.";
                else if (effect == 3)
                    modeHintLabel.Text = "Animated rainbow effect from the Aura Core controller.";
                else if (effect == 2)
                    modeHintLabel.Text = "Automatic color cycle. Custom zone colors are ignored.";
                else
                    modeHintLabel.Text = "Static and Breathing can be controlled as one zone or four zones.";
            }
        }

        private static string ColorToString(Color c)
        {
            return string.Format("{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
        }

        private static Color StringToColor(string s, Color fallback)
        {
            try
            {
                if (s != null && s.Length == 6)
                {
                    int r = Convert.ToInt32(s.Substring(0, 2), 16);
                    int g = Convert.ToInt32(s.Substring(2, 2), 16);
                    int b = Convert.ToInt32(s.Substring(4, 2), 16);
                    return Color.FromArgb(r, g, b);
                }
            }
            catch { }
            return fallback;
        }

        private Dictionary<string, string> ReadIni()
        {
            Dictionary<string, string> d =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(settingsPath)) return d;

            string[] lines = File.ReadAllLines(settingsPath);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                int p = line.IndexOf('=');
                if (p > 0)
                    d[line.Substring(0, p).Trim()] = line.Substring(p + 1).Trim();
            }
            return d;
        }

        private void LoadSettings()
        {
            loadingSettings = true;
            try
            {
                Dictionary<string, string> d = ReadIni();
                string v;

                if (d.TryGetValue("Effect", out v))
                    effectBox.SelectedIndex = Math.Max(0, Math.Min(4, Convert.ToInt32(v)));
                if (d.TryGetValue("Speed", out v))
                    speedBox.SelectedIndex = Math.Max(0, Math.Min(2, Convert.ToInt32(v)));
                if (d.TryGetValue("Brightness", out v))
                    brightnessBox.SelectedIndex = Math.Max(0, Math.Min(3, Convert.ToInt32(v)));
                if (d.TryGetValue("FourZones", out v))
                    fourZoneCheck.Checked = (v == "1");

                for (int i = 0; i < 4; i++)
                {
                    if (d.TryGetValue("Color" + (i + 1).ToString(), out v))
                        colors[i] = StringToColor(v, colors[i]);
                }

                if (d.TryGetValue("Breath2", out v))
                    breathSecond = StringToColor(v, breathSecond);

                for (int i = 0; i < 4; i++)
                    zonePanels[i].BackColor = colors[i];

                breathSecondPanel.BackColor = breathSecond;
                autoApplyCheck.Checked = File.Exists(startupPath);
            }
            catch { }
            finally
            {
                loadingSettings = false;
            }
        }

        private void SaveSettings()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));

                using (StreamWriter w = new StreamWriter(settingsPath, false))
                {
                    w.WriteLine("Effect=" + effectBox.SelectedIndex.ToString());
                    w.WriteLine("Speed=" + speedBox.SelectedIndex.ToString());
                    w.WriteLine("Brightness=" + brightnessBox.SelectedIndex.ToString());
                    w.WriteLine("FourZones=" + (fourZoneCheck.Checked ? "1" : "0"));

                    for (int i = 0; i < 4; i++)
                    {
                        w.WriteLine(
                            "Color" + (i + 1).ToString() + "=" + ColorToString(colors[i]));
                    }

                    w.WriteLine("Breath2=" + ColorToString(breathSecond));
                }
            }
            catch { }
        }

        private void UpdateStartup()
        {
            try
            {
                if (autoApplyCheck.Checked)
                {
                    SaveSettings();
                    string exe = Application.ExecutablePath;
                    File.WriteAllText(
                        startupPath,
                        "@echo off\r\n"
                        + "timeout /t 3 /nobreak >nul\r\n"
                        + "start \"\" \"" + exe + "\" /autoapply\r\n");
                }
                else if (File.Exists(startupPath))
                {
                    File.Delete(startupPath);
                }
            }
            catch (Exception ex)
            {
                SetStatus("Startup setting could not be changed: " + ex.Message, false);
            }
        }

        private void CopyDiagnostics()
        {
            try
            {
                string text = "Legacy Aura Control 1.2.0\r\n";
                text += "Windows: " + Environment.OSVersion.ToString() + "\r\n";
                text += "64-bit OS: " + Environment.Is64BitOperatingSystem.ToString() + "\r\n";
                text += "Detected HID interfaces: " + devices.Count.ToString() + "\r\n\r\n";

                for (int i = 0; i < devices.Count; i++)
                {
                    HidDeviceInfo d = devices[i];
                    text += string.Format(
                        "#{0}: VID_{1:X4} PID_{2:X4} OUT={3} FEATURE={4} INPUT={5} UsagePage=0x{6:X4} Usage=0x{7:X4}\r\n",
                        i + 1, d.VendorId, d.ProductId, d.OutputReportLength,
                        d.FeatureReportLength, d.InputReportLength, d.UsagePage, d.Usage);
                }

                Clipboard.SetText(text);
                SetStatus("Diagnostics copied to the clipboard.", true);
            }
            catch (Exception ex)
            {
                SetStatus("Diagnostics could not be copied: " + ex.Message, false);
            }
        }

        private void ShowAbout()
        {
            MessageBox.Show(
                this,
                "Legacy Aura Control 1.2.0\r\n\r\n"
                + "Standalone control for legacy ASUS ROG Aura Core keyboards.\r\n\r\n"
                + "Funktionen:\r\n"
                + "• Static color\r\n"
                + "• Breathing\r\n"
                + "• Color cycle\r\n"
                + "• Rainbow\r\n"
                + "• 4-zone control\r\n"
                + "• Hardware brightness 0–3\r\n"
                + "• Automatic apply at Windows login\r\n"
                + "• Tray icon / notification area with quick menu\r\n\r\n"
                + "No ASUS binaries required.",
                "About Legacy Aura Control",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void SetStatus(string text, bool ok)
        {
            statusLabel.Text = text;
            statusLabel.ForeColor =
                ok ? Color.FromArgb(150, 220, 160) : Color.FromArgb(245, 150, 150);
        }

        public void AutoApplyAndClose()
        {
            // Historical method name kept for compatibility with /autoapply.
            // Since v1.1 the app applies the saved profile and remains in the tray.
            startingHidden = true;

            Shown += delegate
            {
                HideToTray(false);

                for (int tries = 0; tries < 16; tries++)
                {
                    RefreshDevices();
                    if (devices.Count > 0) break;
                    Thread.Sleep(500);
                }

                if (devices.Count > 0)
                {
                    try
                    {
                        HidDeviceInfo d = devices[0];

                        AuraHid.Apply(
                            d,
                            effectBox.SelectedIndex,
                            speedBox.SelectedIndex,
                            fourZoneCheck.Checked,
                            colors,
                            breathSecond);

                        AuraHid.SetBrightness(
                            d,
                            brightnessBox.SelectedIndex < 0 ? 3 : brightnessBox.SelectedIndex);
                    }
                    catch { }
                }
            };
        }
    }

    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            MainForm form = new MainForm();

            if (args != null && args.Length > 0 &&
                string.Equals(args[0], "/autoapply", StringComparison.OrdinalIgnoreCase))
            {
                form.AutoApplyAndClose();
            }

            Application.Run(form);
        }
    }
}
