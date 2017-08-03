using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Serialization;
using StartupManager.Windows;
using System.Timers;

namespace StartupManager
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private enum ExecutionMode
        {
            Normal,
            Autostart
        };

        private ExecutionMode appMode = ExecutionMode.Normal;

        private const string CONFIG_FILE_NAME = "config.xml";
        public MainWindowDataModel Model { get; set; } = new MainWindowDataModel();
        private Timer timer = new Timer(1000);

        public MainWindow()
        {
            InitializeComponent();
            Model = LoadConfigFile(CONFIG_FILE_NAME);
            timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            List<string> args = new List<string>(Environment.GetCommandLineArgs());

            if(args.Contains("-auto"))
            {
                appMode = ExecutionMode.Autostart;
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Model.AddTask(new ManagedTask("name", 123123123, "filePath", StartupAction.Restart, true));
        }

        private void dGridTasks_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            int index = dGridTasks.SelectedIndex;
            string colName = e.Column.Header.ToString();
            if (colName == "Path")
            {
                FileFolderDialog taskTypeWin = new FileFolderDialog();
                string taskPath = taskTypeWin.ShowDialog();

                // Process save file dialog box results
                if (taskPath != "")
                {
                    Model.Tasks[index].TaskPath = taskPath;
                }
            }
        }

        private void btnAddTask_Click(object sender, RoutedEventArgs e)
        {
            FileFolderDialog taskTypeWin = new FileFolderDialog();
            string taskPath = taskTypeWin.ShowDialog();
            
            // Process save file dialog box results
            if (taskPath != "")
            {
                ManagedTask newTask = new ManagedTask();
                newTask.TaskPath = taskPath;
                newTask.Name = Path.GetFileName(taskPath);
                Model.AddTask(newTask);
            } 
        }
        private void btnTestTask_Click(object sender, RoutedEventArgs e)
        {
            int index = dGridTasks.SelectedIndex;
            ManagedTask task = Model.Tasks[index];
            bool started = task.Execute();
            if (!started)
            {
                MessageBox.Show("Could not execute the task: " + task, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveConfigFile(CONFIG_FILE_NAME);
        }

        private void SaveConfigFile(string fileName)
        {
            if(Model != null)
            {
                var aSerializer = new XmlSerializer(typeof(MainWindowDataModel));
                using (StreamWriter sw = new StreamWriter(fileName))
                {
                    aSerializer.Serialize(sw, Model);
                }
            }
        }

        private MainWindowDataModel LoadConfigFile(string fileName)
        {
            if(fileName == null)
            {
                throw new NullReferenceException("fileName is null");
            }

            MainWindowDataModel newModel = new MainWindowDataModel();
            if (File.Exists(fileName))
            {
                try
                {
                    XmlSerializer deserializer = new XmlSerializer(typeof(MainWindowDataModel));

                    TextReader reader = new StreamReader(fileName);

                    object obj = deserializer.Deserialize(reader);
                    reader.Close();

                    newModel = (MainWindowDataModel)obj;
                }
                catch (Exception ex)
                {
                    string additionalInfo = "";
                    additionalInfo = ex.Message;
                    if (ex.InnerException != null)
                    {
                        additionalInfo += "\n" + ex.InnerException.Message;
                    }
                    MessageBox.Show("Error reading the config file:\n" + additionalInfo, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
               
            }
            return newModel;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Model == null)
            {
                Close();
            }

            if(appMode == ExecutionMode.Autostart)
            {
                StartTasks();
            }
        }

        private void dGridTasks_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            DataGrid dg = sender as DataGrid;
            if (dg != null)
            {
                DataGridRow dgr = (DataGridRow)(dg.ItemContainerGenerator.ContainerFromIndex(dg.SelectedIndex));
                if (e.Key == Key.Delete && !dgr.IsEditing)
                {
                    // User is attempting to delete the row
                    var result = MessageBox.Show(
                        "About to delete the current task.\n\nProceed?",
                        "Delete",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question,
                        MessageBoxResult.No);
                    e.Handled = (result == MessageBoxResult.No);
                }
            }
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            ++Model.ElapsedTime;
        }

        private void btnSimulateAll_Click(object sender, RoutedEventArgs e)
        {
            StartTasks();
        }
        static int numOfTasksRemaining;
        private void ATaskFinished(ManagedTask task)
        {
            numOfTasksRemaining--;
            if(numOfTasksRemaining == 0)
            {
                FinishedAllTasks();
            }
        }
        private void StartTasks()
        {
            dGridTasks.IsReadOnly = true;
            btnSimulateAll.IsEnabled = false;

            Model.ElapsedTime = 0;
            timer.Start();
            numOfTasksRemaining = Model.ActiveTasks;
            foreach (ManagedTask task in Model.Tasks)
            {
                if (task.IsActivated)
                {
                    task.ExecuteDelayed(ATaskFinished);
                }
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            foreach(ManagedTask task in Model.Tasks)
            {
                if(task.IsActivated)
                {
                    task.Stop();
                }
            }
            btnSimulateAll.IsEnabled = true;
            dGridTasks.IsReadOnly = false;
        }

        private void FinishedAllTasks()
        {
            if(appMode == ExecutionMode.Autostart)
            {
                Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        Close();
                    }
                    ));
            }
            dGridTasks.IsReadOnly = false;
        }
    }
}
