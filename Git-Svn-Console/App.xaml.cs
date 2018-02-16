using System.Windows;

namespace Git_Svn_Console
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var workingDir = "";
            for (int i = 0; i != e.Args.Length; ++i)
            {
                if (e.Args[i] == "-debug")
                {
                    workingDir = "D:/temp/SvnGitTest2/SvnGit_V2";
                }
                else
                {
                    workingDir = e.Args[i];
                }
            }

            var mainWindow = new MainWindow(workingDir);
            mainWindow.Show();
        }
    }
}
