using Microsoft.Win32;
using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;

namespace StartupManager
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string OPEN_FILE_DIALOG_FILTER = "All files (*.*)|*.*|Executable (.exe)|*.exe|Batch Files (.bat)|*.bat";
        private const string CONFIG_FILE_NAME = "config.xml";

        public MainWindowDataModel Model { get; set; } = new MainWindowDataModel();

        public MainWindow()
        {
            InitializeComponent();
            Model = LoadConfigFile(CONFIG_FILE_NAME);
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Model.AddTask(new Task("name", 123123123, "filePath", StartupAction.Restart, true));
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
            Task newTask = new Task();

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
            bool started = Model.Tasks[index].Start();
            if(!started)
            {
                MessageBox.Show("Could not start the file!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveConfigFile(CONFIG_FILE_NAME);
        }

        private void SaveConfigFile(string fileName)
        {
            var aSerializer = new XmlSerializer(typeof(MainWindowDataModel));
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                aSerializer.Serialize(sw, Model);
            }
        }

        private MainWindowDataModel LoadConfigFile(string fileName)
        {
            if(fileName == null)
            {
                throw new NullReferenceException("fileName is null");
            }

            MainWindowDataModel newModel;
            if (!File.Exists(fileName))
            {
                newModel = new MainWindowDataModel();
            }
            else
            {
                XmlSerializer deserializer = new XmlSerializer(typeof(MainWindowDataModel));

                TextReader reader = new StreamReader(fileName);

                object obj = deserializer.Deserialize(reader);
                reader.Close();

                newModel = (MainWindowDataModel) obj;
            }
            return newModel;
        }
    }
}
