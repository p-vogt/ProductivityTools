using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace Git_Svn_Console
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private void OnSystemCommandCloseWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow((Window)e.Parameter);
        }
        private void OnMinimizeWindowCommand(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow((Window)e.Parameter);
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            WindowContent.SetMainWindow(this, WindowContent.Margin.Top);
            WindowContent.InitAsync();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            WindowContent.Window_Closing(sender, e);
        }

        private void WindowContent_Loaded(object sender, RoutedEventArgs e)
        {
            MainWindowContent.RemoveTemporaryCmdFiles();

        }
    }
}
