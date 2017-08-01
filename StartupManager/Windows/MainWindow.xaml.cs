using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Xml.Serialization;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StartupManager
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string OPEN_FILE_DIALOG_FILTER = "All files (*.*)|*.*|Executable (.exe)|*.exe|Batch Files (.bat)|*.bat";
        private const string CONFIG_FILE_NAME = "config.xml";
        private List<CancellationTokenSource> ctsList = new List<CancellationTokenSource>(); 
        public MainWindowDataModel Model { get; set; } = new MainWindowDataModel();

        public MainWindow()
        {
            InitializeComponent();
            Model = LoadConfigFile(CONFIG_FILE_NAME);
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Model.AddTask(new ManagedTask("name", 123123123, "filePath", StartupAction.Restart, true));
        }

        private void dGridTasks_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            int index = dGridTasks.SelectedIndex;
            string colName = e.Column.Header.ToString();
            if (colName == "FilePath")
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.InitialDirectory = Path.GetDirectoryName(Model.Tasks[index].FilePath);
                dlg.Filter = OPEN_FILE_DIALOG_FILTER; // Filter files by extension

                // Show save file dialog box
                bool? result = dlg.ShowDialog();

                // Process save file dialog box results
                if (result == true)
                {
                    Model.Tasks[index].FilePath = dlg.FileName;
                }
            }
        }

        private void btnAddTask_Click(object sender, RoutedEventArgs e)
        {
            ManagedTask newTask = new ManagedTask();

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = OPEN_FILE_DIALOG_FILTER; // Filter files by extension

            // Show save file dialog box
            bool? result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                newTask.FilePath = dlg.FileName;
                newTask.Name = Path.GetFileName(dlg.FileName);
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
                    else
                    {
                        
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

        private async void btnSimulateAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (ManagedTask task in Model.Tasks)
            {
                if(task.IsActivated)
                {
                    CancellationTokenSource cts = new CancellationTokenSource();
                    ctsList.Add(cts);
                    bool finished = await task.ExecuteDelayed(cts);
                    
                }
            
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            foreach(CancellationTokenSource cts in ctsList)
            {
                cts.Cancel();
            }
        }
    }
}
