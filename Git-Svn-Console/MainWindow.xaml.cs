using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

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
                        UpdateUIAsync();
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
        //TODO
        private const string WORKING_DIR = "D:/temp/SvnGitTest2/SvnGit_V2";
        private Process consoleProcess;
        private IntPtr hWndOriginalParent;
        private IntPtr consoleHandle;
        GitSvnClient client;


        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitAsync();
        }

        private async Task InitAsync()
        {
            IncludeGitBashInGUI();
            // TODO             WinAPI.ClearConsole(consoleHandle); as start param
            WinAPI.SendString("cd " + WORKING_DIR + "\n", consoleHandle);
            WinAPI.ClearConsole(consoleHandle);
            await UpdateUIAsync();
            WinAPI.ClearConsole(consoleHandle);
        }

        private void IncludeGitBashInGUI()
        {
            var windowHandle = new WindowInteropHelper(Application.Current.MainWindow).Handle;
            var hWndParent = IntPtr.Zero;
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
            client = new GitSvnClient(WORKING_DIR, consoleHandle);
            const int GWL_STYLE = (-16);
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

            Process[] processlist = Process.GetProcesses();
            var baseDir = AppDomain.CurrentDomain.BaseDirectory.ToUpper();
            baseDir = baseDir.Replace(":", "");
            baseDir = baseDir.Replace("\\", "/");
            for (int i = 0; i < processlist.Length; ++i)
            {
                if (String.IsNullOrWhiteSpace(processlist[i].MainWindowTitle))
                {
                    continue;
                }

                string title = processlist[i].MainWindowTitle.ToUpper() + "/";
 
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

        private async Task UpdateUIAsync()
        {
            await Task.Factory.StartNew(() =>
            {
                UpdateCurrentSvnBranch();
                UpdateGitLocalBranches();
            });

        }

        private void UpdateCurrentSvnBranch()
        {
            CurrentSvnBranch = client.GetCurrentSvnBranch();
        }

        private void UpdateGitLocalBranches()
        {
            var regexBranches = new Regex(@"^\s*(?<branchName>\S+?)\n?$", RegexOptions.Multiline);
            var regexCurrentBranch = new Regex(@"^\s*\*\s*(?<branchName>\S+?)\n?$", RegexOptions.Multiline);
            List<string> gitBranchList;
            string currentGitBranch;
            client.GetGitBranches(regexBranches, regexCurrentBranch, out gitBranchList, out currentGitBranch);
            gitBranchList.Add(currentGitBranch);

            LocalGitBranches = gitBranchList;
            TargetGitBranch = currentGitBranch;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (consoleProcess?.HasExited == false)
            {
                consoleProcess.Close();
            }
            KillAllTasksDialog();
        }

        private static bool KillAllTasksDialog()
        {
            var result = MessageBox.Show("Do you want to terminate >ALL< bash, git-bash and perl processes?",
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
                IncludeGitBashInGUI();
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
                || processName == "git-bash")
                {
                    if (!allProcceses[i].HasExited)
                    {
                        allProcceses[i].Kill();
                    }
                }
            }
        }
    }
}
