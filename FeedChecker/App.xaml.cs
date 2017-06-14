using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace FeedChecker
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
            [STAThread]
            public static void Main(string[] args)
            {
                if (!IsFirstInstance())
                {
                    MessageBox.Show("Already running...", "Ilias Feed Checker", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var app = new App();
                app.InitializeComponent();
                app.Run();
            }
            
            //give the mutex a name => it will be systemwide.
            private static readonly Mutex mutex = new Mutex(true, "_!_ILIAS_Feed_Checker_!_");
            public static bool IsFirstInstance()
            {
                return mutex.WaitOne(TimeSpan.Zero, false);
            }
            protected override void OnExit(ExitEventArgs e)
            {
                mutex?.ReleaseMutex();
                base.OnExit(e);
            }
    }
}
