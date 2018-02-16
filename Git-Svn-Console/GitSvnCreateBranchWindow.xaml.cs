using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;

namespace Git_Svn_Console
{
    /// <summary>
    /// Interaktionslogik für GitSvnCloneWindow.xaml
    /// </summary>
    public partial class GitSvnCreateBranchWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        string branchname;
        public string Branchname
        {
            get
            {
                return branchname;
            }
            set
            {
                if (value != branchname)
                {
                    branchname = value;
                    NotifyPropertyChanged();
                }
            }
        }
        GitSvnClient client;
        public GitSvnCreateBranchWindow(GitSvnClient client)
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
            client.CreateSvnBranch(tBoxBranchname.Text);
            client.CreateGitSvnBranch(tBoxBranchname.Text);
            Close();
        }

        public new void ShowDialog(string currentBranch)
        {
            Branchname = currentBranch;
            base.ShowDialog();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
         
        }
    }
}
