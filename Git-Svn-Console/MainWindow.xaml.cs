using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

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
    }
}
