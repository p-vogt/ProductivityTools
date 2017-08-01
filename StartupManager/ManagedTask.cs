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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

namespace StartupManager
{
    public class ManagedTask : INotifyPropertyChanged
    {
        private CancellationTokenSource ct;
        public event PropertyChangedEventHandler PropertyChanged;
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
            FilePath = filePath;
            IsActivated = isActivated;
            Action = action;
        }

        internal void LoadIcon()
        {
            if(File.Exists(FilePath))
            {
                Bitmap bitmap = System.Drawing.Icon.ExtractAssociatedIcon(FilePath).ToBitmap();

                Icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                bitmap.GetHbitmap(),
                IntPtr.Zero,
                System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(bitmap.Width, bitmap.Height));
            }
        }

        [XmlIgnore]
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
        public string FilePath
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

        [XmlIgnore]
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

        [XmlIgnore]
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
        [XmlIgnore]
        private bool? errorExecuting = null;
        [XmlIgnore]
        public bool? ErrorExecuting
        {
            get
            {
                return errorExecuting;
            }
            set
            {
                errorExecuting = value;
                OnPropertyChanged();
            }
        }


        public override string ToString()
        {
            return Name;
        }



        private bool ExecuteDelayed_internal()
        {

            ErrorExecuting = null;
            try
            {
                for (int i = 0; i < 1000; ++i) // 1000 -> ms to secs
                {
                    Thread.Sleep(DelayTime);
                    ct.Token.ThrowIfCancellationRequested();
                }
                return Execute();
            }
            catch (OperationCanceledException)
            {

            }
            finally
            {
                Finished = true;
            }
            return false;
        }
        public async Task<bool> ExecuteDelayed(CancellationTokenSource ct)
        {
            if(ct == null)
            {
                throw new NullReferenceException("ct");
            }
            this.ct = ct;
            Task<bool> task = new Task<bool>(ExecuteDelayed_internal);
            task.Start();
            await task;
            return task.Result;
        }

        public bool Execute()
        {
            if (File.Exists(FilePath))
            {
                try
                {
                    switch (Action)
                    {
                        case StartupAction.Close:
                            CloseProcess();
                            break;
                        case StartupAction.Start:
                            if(!IsProcessRunning())
                            {
                                Process.Start(FilePath);
                            }
                            break;
                        case StartupAction.Restart:
                            CloseProcess();
                            Process.Start(FilePath);
                            break;
                        default:
                            throw new System.InvalidOperationException("Action state: " + Action);
                            break;  
                    }
                    ErrorExecuting = false;
                    return true;
                }
                catch (Exception ex)
                {

                }
            }
            ErrorExecuting = true;
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


                if (processPath == FilePath)
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


                if (processPath == FilePath)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
