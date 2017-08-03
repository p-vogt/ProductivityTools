using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

namespace StartupManager
{
    public class ManagedTask : INotifyPropertyChanged
    {

        public delegate void TaskCompletedCallBack(ManagedTask sender);
        private TaskCompletedCallBack Callback;

        private CancellationTokenSource ct;
        public event PropertyChangedEventHandler PropertyChanged;
        private System.Timers.Timer timer = new System.Timers.Timer();
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ManagedTask() : this("",0,"",StartupAction.Start,true)
        {
      
        }
        public ManagedTask(string name, int delayTimeMs, string filePath, StartupAction action, bool isActivated )
        {
            Name = name;
            DelayTime = delayTimeMs;
            TaskPath = filePath;
            IsActivated = isActivated;
            Action = action;
        }

        internal void LoadIcon()
        {
            if(File.Exists(TaskPath))
            {
                Icon tmpicon = System.Drawing.Icon.ExtractAssociatedIcon(TaskPath);
                if(tmpicon != null)
                {
                    Bitmap bitmap = tmpicon.ToBitmap();

                    Icon =  System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                            bitmap.GetHbitmap(),
                            IntPtr.Zero,
                            System.Windows.Int32Rect.Empty,
                            BitmapSizeOptions.FromWidthAndHeight(bitmap.Width, bitmap.Height));
                }
                
            }
        }

        private bool isActivated = false;
        public bool IsActivated
        {
            get
            {
                return isActivated;
            }
            set
            {
                isActivated = value;
                OnPropertyChanged();
            }
        }

        public string Name { get; set; }
        /// <summary>
        /// Time of the delay in ms.
        /// </summary>
        public int DelayTime { get; set; }
        public StartupAction Action { get; set; }

        private string filePath;
        public string TaskPath
        {
            get
            {
                return filePath;
            }

            set
            {
                filePath = value;
                LoadIcon();
                OnPropertyChanged();
            }
        }

        private ImageSource icon;
        [XmlIgnore]
        public ImageSource Icon
        {
            get
            {
                return icon;
            }
            set
            {
                icon = value;
                OnPropertyChanged();
            }
        }

        private bool finished = false;
        [XmlIgnore]
        public bool Finished
        {
            get
            {
                return finished;
            }
            set
            {
                finished = value;
                OnPropertyChanged();
            }
        }

        private bool stoppedByUser;
        [XmlIgnore]
        public bool StoppedByUser
        {
            get
            {
                return stoppedByUser;
            }
            set
            {
                stoppedByUser = value;
                OnPropertyChanged();
            }
        }


        public override string ToString()
        {
            return Name;
        }

        public void ExecuteDelayed(TaskCompletedCallBack CallBack)
        {
            StoppedByUser = false;
            this.Callback = CallBack;
          
            timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            timer.Interval = (DelayTime * 1000) + 1; // +1 --> allow 0 seconds = instant (1ms)

            timer.Enabled = true;
            timer.AutoReset = false;
            timer.Start();

        }
        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            timer.Stop();
            if (StoppedByUser == false)
            {
                Execute();
            }
            Callback(this);
        }

        public void Stop()
        {
            timer.Stop();
            StoppedByUser = true;
        }
        private void StartTask()
        {
            if(File.Exists(TaskPath))
            {
                Process.Start(TaskPath);
                Finished = true;
            } 
            else if(Directory.Exists(TaskPath))
            {
                Process.Start("explorer.exe", TaskPath);
                Finished = true;
            }

           
        }
        public bool Execute()
        {
            if (File.Exists(TaskPath) || Directory.Exists(TaskPath))
            {
                try
                {
                    switch (Action)
                    {
                        case StartupAction.Close:
                            CloseProcess();
                            break;
                        case StartupAction.Start:
                                StartTask();
                            break;
                        case StartupAction.Restart:
                            CloseProcess();
                            StartTask();
                            break;
                        default:
                            throw new System.InvalidOperationException("Action state: " + Action);
                            break;  
                    }
                    StoppedByUser = false;
                    return true;
                }
                catch (Exception ex)
                {

                }
            }
            StoppedByUser = true;
            return false;
        }

        private bool CloseProcess()
        {
            bool stoppedProcess = false;
            Process[] processList = Process.GetProcesses();

            foreach (Process process in processList)
            {

                string processPath = "";
                try
                {
                    processPath = process.MainModule.FileName;
                }
                catch (Exception)
                {
                    continue;
                }


                if (processPath == TaskPath)
                {
                    process.Kill();
                    stoppedProcess = true;
                }
            }
            return stoppedProcess;
        }

        private bool IsProcessRunning()
        {
            Process[] processList = Process.GetProcesses();

            foreach (Process process in processList)
            {

                string processPath = "";
                try
                {
                    processPath = process.MainModule.FileName;
                }
                catch (Exception)
                {
                    continue;
                }

                if (processPath == TaskPath)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
