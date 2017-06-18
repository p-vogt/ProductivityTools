using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;
using WpfTrayIcon;

namespace WindowOnTop
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TrayIcon trayIcon;
        private HotKey hotkey;

        [Serializable()]
        private class StoredSettings
        {
            public ModifierKeys modifiers = ModifierKeys.None;
            public Key key = Key.None;

            override public string ToString()
            {
                string hotkeyStr = "";
                if (modifiers != ModifierKeys.None)
                {
                    hotkeyStr += modifiers.ToString().Replace(",", " +").Replace("Left", "").Replace("Right", "") + " + ";
                }
                hotkeyStr += key.ToString();
                hotkeyStr = hotkeyStr.Replace("Oem", "");
                return hotkeyStr;
            }

        }

        private StoredSettings settings = new StoredSettings();

        private const String filename = "settings.bin";
        public enum GWL
        {
            GWL_WNDPROC = (-4),
            GWL_HINSTANCE = (-6),
            GWL_HWNDPARENT = (-8),
            GWL_STYLE = (-16),
            GWL_EXSTYLE = (-20),
            GWL_USERDATA = (-21),
            GWL_ID = (-12)
        }

        //Places the window above all non-topmost windows. The window maintains its topmost position even when it is deactivated.
        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        //Places the window above all non-topmost windows (that is, behind all topmost windows). This flag has no effect if the window is already a non-topmost window.
        static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        //Retains the current size (ignores the cx and cy parameters).
        const UInt32 SWP_NOSIZE = 0x0001;
        //Retains the current position (ignores X and Y parameters).
        const UInt32 SWP_NOMOVE = 0x0002;
        //Displays the window.
        const UInt32 SWP_SHOWWINDOW = 0x0040;
        // is the window topmost?
        const UInt32 WS_EX_TOPMOST = 0x00000008;

        public MainWindow()
        {
            InitializeComponent();
            trayIcon = new SimpleTrayIcon(this, Properties.Resources.top);
            trayIcon.Visible = true;
            Loaded += (s, e) =>
            {
                if (File.Exists(filename))
                {
                    try
                    {
                        LoadSettings();
                        btnActivate_Click(null,null); // register hotkey
                        UpdateHotkeyLabel();
                    }
                    catch (Exception)
                    {
                    }
                }
                this.WindowState = WindowState.Minimized;
                this.Visibility = Visibility.Hidden;
            };   
        }

        Process GetActiveProcess()
        {
            IntPtr hwnd = GetForegroundWindow();
            uint pid;
            GetWindowThreadProcessId(hwnd, out pid);
            Process p = Process.GetProcessById((int)pid);

            return p;
           
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();


        [DllImport("user32.dll")]
        static extern bool SetWindowText(IntPtr hWnd, string text);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString,
            int nMaxCount);


        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, GWL nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, GWL nIndex);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetWindowTextLength(IntPtr hWnd);

        // This static method is required because Win32 does not support
        // GetWindowLongPtr directly
        public static IntPtr GetWindowLongPtr(IntPtr hWnd, GWL nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex);
            else
                return GetWindowLongPtr32(hWnd, nIndex);
        }

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private void ToggleCurrentWindowTopmostState()
        {
            Process window = GetActiveProcess();
            IntPtr windowHandle = window.MainWindowHandle;
            string windowText = GetWindowText(windowHandle);
            const string appendix = " - (topmost)";
            if (IsWindowTopmost(windowHandle))
            {
                WindowUnsetTopmost(windowHandle);
                if(windowText.Contains(appendix))
                {
                    SetWindowText(windowHandle, windowText.Replace(appendix,""));
                }
            }
            else
            {
                WindowSetTopmost(windowHandle);
                
                SetWindowText(windowHandle, windowText + appendix);
            }
        }

        private string GetWindowText(IntPtr hWnd)
        {
            int textLength = GetWindowTextLength(hWnd);
            StringBuilder outText = new StringBuilder(textLength + 1);
            GetWindowText(hWnd, outText, 150);
            return outText.ToString();
        }

        private void WindowSetTopmost(IntPtr hWnd)
        {
            if (hWnd != IntPtr.Zero)
            {
                SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            }
        }
        private void WindowUnsetTopmost(IntPtr hWnd)
        {
            if (hWnd != IntPtr.Zero)
            {
                SetWindowPos(hWnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            }
        }

        private bool IsWindowTopmost(IntPtr hWnd)
        {
            IntPtr dwExStyle = GetWindowLongPtr(hWnd, GWL.GWL_EXSTYLE);
            return (dwExStyle.ToInt64() & WS_EX_TOPMOST) != 0;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // no modifierkey fired this event
            if (e.Key != Key.System && e.Key != Key.LeftAlt && e.Key != Key.LeftCtrl && e.Key != Key.LeftShift && e.Key != Key.LWin
            && e.Key != Key.RightAlt && e.Key != Key.RightCtrl && e.Key != Key.RightShift && e.Key != Key.RWin)
            {
                settings.modifiers = Keyboard.Modifiers;
                settings.key = e.Key;

                UpdateHotkeyLabel();

            }

            e.Handled = true;
        }

        private void UpdateHotkeyLabel()
        {
            textBox.Content = settings.ToString();
        }

        private void btnSetHotkey_Click(object sender, RoutedEventArgs e)
        {
            btnActivate_Click(null, null); // register
            SaveSettings();
        }

        private void registerHotkey()
        {
            try
            {
                unregisterHotkey();
                hotkey = new HotKey(settings.modifiers, settings.key, this);
                hotkey.HotKeyPressed += (k) => ToggleCurrentWindowTopmostState();
            }
            catch (Exception e)
            {

                MessageBox.Show(e.Message);
            }
          
        }
        private void unregisterHotkey()
        {
            hotkey?.Dispose();
        }
        private void btnActivate_Click(object sender, RoutedEventArgs e)
        {
            btnActivate.IsEnabled = false;
            registerHotkey();
            btnDeactivate.IsEnabled = true;
        }

        private void btnDeactivate_Click(object sender, RoutedEventArgs e)
        {
            btnDeactivate.IsEnabled = false;
            unregisterHotkey();
            btnActivate.IsEnabled = true;
        }

        private void SaveSettings()
        {
            try
            {
                Stream testFileStream = File.Create(filename);
                BinaryFormatter serializer = new BinaryFormatter();
                serializer.Serialize(testFileStream, settings);
                testFileStream.Close();
            }
            catch (Exception e)
            {

                MessageBox.Show(e.Message);
            }

        }
        private void LoadSettings()
        {
            try
            {
                Stream testFileStream = File.OpenRead(filename);
                BinaryFormatter deserializer = new BinaryFormatter();
                settings = (StoredSettings)deserializer.Deserialize(testFileStream);
                testFileStream.Close();
            }
            catch (Exception e)
            {

                MessageBox.Show(e.Message);
            }
     
        }       
    }
    
}
