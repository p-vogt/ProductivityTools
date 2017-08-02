using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WinForms = System.Windows.Forms;

namespace StartupManager.Windows
{
    /// <summary>
    /// Interaktionslogik für FileFolderDialog.xaml
    /// </summary>
    public partial class FileFolderDialog : Window
    {
        private const string OPEN_FILE_DIALOG_FILTER = "All files (*.*)|*.*|Executable (.exe)|*.exe|Batch Files (.bat)|*.bat";

        string destinationPath = "";
        public FileFolderDialog()
        {
            InitializeComponent();
        }

        new public string ShowDialog()
        {
            base.ShowDialog();
            return destinationPath;
        }

        private void btnFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = OPEN_FILE_DIALOG_FILTER; // filter files by extension

            // Show open file dialog box
            bool? result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                destinationPath = dlg.FileName;
                Close();
            }            
        }

        private void btnFolder_Click(object sender, RoutedEventArgs e)
        {
            WinForms.FolderBrowserDialog fbd = new WinForms.FolderBrowserDialog();
            WinForms.DialogResult result = fbd.ShowDialog();
            if(result == WinForms.DialogResult.OK)
            {
                destinationPath = fbd.SelectedPath;
                Close();
            }
         
        }
    }
}
