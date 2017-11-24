﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Git_Svn_Console
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        Brush svnBranchBrush;
        public Brush SvnBranchBrush
        {
            get
            {
                return svnBranchBrush;
            }
            set
            {
                if (value != svnBranchBrush)
                {
                    svnBranchBrush = value;
                    NotifyPropertyChanged();
                }
            }
        }

        List<string> localGitBranches;
        public List<string> LocalGitBranches
        {
            get
            {
                return localGitBranches;
            }
            set
            {
                if (value != localGitBranches)
                {
                    localGitBranches = value;
                    NotifyPropertyChanged();
                }
            }
        }
        string targetGitBranch;
        public string TargetGitBranch
        {
            get
            {
                return targetGitBranch;
            }
            set
            {
                if (value != targetGitBranch)
                {
                    targetGitBranch = value;
                    if (targetGitBranch != "")
                    {
                        client.Checkout(targetGitBranch);
                         Thread.Sleep(500);
                        UpdateSVNAsync();
                      
                    }

                    NotifyPropertyChanged();
                }
            }
        }
        string currentSvnBranch;
        public string CurrentSvnBranch
        {
            get
            {
                return currentSvnBranch;
            }
            set
            {
                if (value != currentSvnBranch)
                {
                    currentSvnBranch = value;
                    NotifyPropertyChanged();
                }
            }
        }
        //TODO startup path
        private string workingDir = "D:/temp/SvnGitTest2/SvnGit_V2";
        private Process consoleProcess;
        private IntPtr hWndOriginalParent;
        private IntPtr consoleHandle;
        GitSvnClient client;


        public MainWindow()
        {
            InitializeComponent();
        }

        public void ExcludeGitBashFromGUI()
        {
            WinAPI.SetParent(consoleHandle, hWndOriginalParent);
            consoleProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            uint processId;
            WinAPI.GetWindowThreadProcessId(consoleHandle, out processId);
            WinAPI.KillProcessAndChildren((int)processId);
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitAsync();
        }

        private void InitAsync()
        {
            IncludeGitBashInGUI();
            // TODO workingDir as start param
            WinAPI.SendString("cd " + workingDir + "\n", consoleHandle);
            WinAPI.ClearConsole(consoleHandle);
            Task.Delay(1000).Wait();
            UpdateGitLocalBranches();
            Task.Delay(100).Wait();
            WinAPI.ClearConsole(consoleHandle);
        }

        private void IncludeGitBashInGUI()
        {
            var windowHandle = new WindowInteropHelper(Application.Current.MainWindow).Handle;
            var hWndParent = IntPtr.Zero;
            //TODO
            var fileName = @"C:\Program Files\Git\git-bash.exe";
            var info = new ProcessStartInfo(fileName)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Minimized
            };
            consoleProcess = Process.Start(fileName);
            consoleHandle = IntPtr.Zero;
            while (consoleHandle == IntPtr.Zero)
            {
                consoleProcess.WaitForInputIdle(100); //wait for the window to be ready for input;
                System.Threading.Thread.Sleep(100);
                consoleProcess.Refresh();              //update process info
                if (consoleProcess.HasExited)
                {
                    return; //abort if the process finished before we got a handle.
                }
                //https://stackoverflow.com/questions/1277563/how-do-i-get-the-handle-of-a-console-applications-window
                //TODO
                consoleHandle = GetGitBashWindowName();
            }
            client = new GitSvnClient(workingDir, consoleHandle);
            const int GWL_STYLE = -16;
            const int WS_VISIBLE = 0x10000000;
            WinAPI.SetWindowLong(consoleHandle, GWL_STYLE, WS_VISIBLE);
            //Windows API call to change the parent of the target window.
            //It returns the hWnd of the window's parent prior to this call.
            hWndOriginalParent = WinAPI.SetParent(consoleHandle, windowHandle);

            //Wire up the event to keep the window sized to match the control
            SizeChanged += WindowResize;
            //Perform an initial call to set the size.
            WindowResize(new object(), new EventArgs());
        }

        private IntPtr GetGitBashWindowName()
        {

            var processlist = Process.GetProcesses();
            var baseDir = AppDomain.CurrentDomain.BaseDirectory.ToUpper();
            baseDir = baseDir.Replace(":", "");
            baseDir = baseDir.Replace("\\", "/");
            for (int i = 0; i < processlist.Length; ++i)
            {
                if (String.IsNullOrWhiteSpace(processlist[i].MainWindowTitle))
                {
                    continue;
                }

                var title = processlist[i].MainWindowTitle.ToUpper() + "/";

                if (title == "MINGW64:/" + baseDir)
                {
                    return WinAPI.FindWindowByCaption(IntPtr.Zero, processlist[i].MainWindowTitle);
                }
            }
            return IntPtr.Zero;
        }

        private void WindowResize(object sender, EventArgs e)
        {
            var windowHandle = new WindowInteropHelper(Application.Current.MainWindow).Handle;

            //Change the docked windows size
            WinAPI.MoveWindow(consoleHandle, 0, (int)(consoleRow.Offset),
                             (int)(consoleColumn.ActualWidth), (int)(consoleRow.ActualHeight - Margin.Bottom - 5), true);
        }

        private void btnCommit_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentSvnBranch == "trunk")
            {
                var result = MessageBox.Show("Warning: trunk-commit! Continue?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    client.Commit();
                }
            }
        }

        private async Task UpdateSVNAsync()
        {
            await Task.Factory.StartNew(() =>
            {
                UpdateCurrentSvnBranch();
            });
        }

        private void UpdateCurrentSvnBranch()
        {
            CurrentSvnBranch = "Determining active SVN Branch...";
            Dispatcher.BeginInvoke(new Action(() =>
            {
                SvnBranchBrush = new SolidColorBrush(Color.FromRgb(0, 120, 0));
            }));

            CurrentSvnBranch = client.GetCurrentSvnBranch();
            if (CurrentSvnBranch == GitSvnClient.ERROR_INDICATOR
             || CurrentSvnBranch.EndsWith("trunk", StringComparison.CurrentCulture))
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    SvnBranchBrush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                }));

            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    SvnBranchBrush = new SolidColorBrush(Color.FromRgb(35, 196, 255));
                }));
            }

        }

        private void UpdateGitLocalBranches()
        {
            List<string> gitBranchList;
            string currentGitBranch;
            client.GetGitBranches(out gitBranchList, out currentGitBranch);
            LocalGitBranches = gitBranchList;
            TargetGitBranch = currentGitBranch;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (consoleProcess?.HasExited == false)
            {
                ExcludeGitBashFromGUI();
                consoleProcess.Close();
            }
            KillAllTasksDialog();
        }

        private static bool KillAllTasksDialog()
        {
            var result = MessageBox.Show("Do you want to terminate >ALL< bash, git-bash, conhost and perl processes?",
                                "Kill Tasks", MessageBoxButton.YesNo,
                                MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                KillBackgroundTasks();
            }
            return result == MessageBoxResult.Yes;
        }

        private void btnKillTasks_Click(object sender, RoutedEventArgs e)
        {
            if (KillAllTasksDialog())
            {
                InitAsync();
            }
        }

        private static void KillBackgroundTasks()
        {
            var allProcceses = Process.GetProcesses();

            for (int i = 0; i < allProcceses.Length; i++)
            {
                var processName = allProcceses[i].ProcessName;
                if (processName == "perl"
                || processName == "bash"
                || processName == "git-bash"
                || processName == "conhost")
                {
                    if (!allProcceses[i].HasExited)
                    {
                        allProcceses[i].Kill();
                    }
                }
            }
        }

        private void btnUpdateWorkingDir_Click(object sender, RoutedEventArgs e)
        {
            workingDir = client.DetermineWorkingDirectory();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            //ExcludeGitBashFromGUI();
            client.ClearCurrentInput();
        }

        private void btnFetch_Click(object sender, RoutedEventArgs e)
        {
            client.Fetch();
        }

        private void btnReload_Click(object sender, RoutedEventArgs e)
        {
            UpdateGitLocalBranches();
        }
    }
}
