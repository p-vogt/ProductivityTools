using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;

namespace Git_Svn_Console
{
    /// <summary>
    /// Interaktionslogik für GitSvnCloneWindow.xaml
    /// </summary>
    public partial class GitSvnCloneWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        string svnHeadRevision;
        public string SvnHeadRevision
        {
            get
            {
                return svnHeadRevision;
            }
            set
            {
                if (value != svnHeadRevision)
                {
                    svnHeadRevision = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(IsCloneAllowed));
                }
            }
        }

        string svnCloneStartRevision = "1";
        public string SvnCloneStartRevision
        {
            get
            {
                return svnCloneStartRevision;
            }
            set
            {
                int result;
                int.TryParse(value, out result);
                if (result > 0 && value != svnCloneStartRevision)
                {
                    svnCloneStartRevision = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(IsCloneAllowed));
                }
            }
        }

        string svnSourceRepository;
        public string SvnSourceRepository
        {
            get
            {
                return svnSourceRepository;
            }
            set
            {
                if (value != svnSourceRepository)
                {
                    svnSourceRepository = value;
                    RefreshRevision();
                    NotifyPropertyChanged();
                }
            }
        }

        private void RefreshRevision()
        {
            var revision = client.GetCurrentSvnRevision(SvnSourceRepository);
            SvnHeadRevision = revision.ToString();
        }

        string destinationFolder;
        public string DestinationFolder
        {
            get
            {
                return destinationFolder;
            }
            set
            {
                if (value != destinationFolder)
                {
                    destinationFolder = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public bool IsCloneAllowed => int.Parse(SvnHeadRevision) > 0 && int.Parse(SvnCloneStartRevision) <= int.Parse(SvnHeadRevision);
        GitSvnClient client;
        public GitSvnCloneWindow(GitSvnClient client)
        {
            if (client == null)
            {
                throw new NullReferenceException(nameof(client));
            }
            this.client = client;
            InitializeComponent();
        }

        private void btnCloneRepo_Click(object sender, RoutedEventArgs e)
        {


            client.CloneRepository(SvnSourceRepository, DestinationFolder, SvnCloneStartRevision);
            string trimmedUri = SvnSourceRepository.TrimEnd('/', '\\');
            var splitted = trimmedUri.Split('/', '\\');
            string dirName = splitted[splitted.Length - 1];
            string dirPath = DestinationFolder + "\\" + dirName;

            // no need to wait for the folder to be created since this command will be executed after the checkout has finished
            client.ChangeDirectoy(dirPath.Replace("\\", "/"));
        }

        public new void Show()
        {
            //TODO
            SvnSourceRepository = "https://PC/svn/SvnGit_V2/";
            DestinationFolder = "C:\\Temp\\repo";

            base.Show();
        }

        private static bool IsTextANumber(string text)
        {
            var regex = new Regex("\\d+");
            return regex.IsMatch(text);
        }

        private void tBoxSvnCloneStartRevision_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !IsTextANumber(e.Text);
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshRevision();
        }
    }
}
