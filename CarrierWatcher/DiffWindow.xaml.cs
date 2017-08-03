using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace CarrierWatcher
{
    /// <summary>
    /// Interaktionslogik für DiffWindow.xaml
    /// </summary>
    public partial class DiffWindow : Window
    {
        public struct Job
        {
            public Job(string name, string url)
            {
                Name = name;
                Url = url;
            }
            public string Name { get; set; }
            public string Url { get; set; }
        }
        public ObservableCollection<Job> RemovedJobs { get; set; } = new ObservableCollection<Job>();
        public ObservableCollection<Job> AddedJobs { get; set; } = new ObservableCollection<Job>();

        public DiffWindow(List<string> removedJobs, List<string> addedJobs)
        {
            InitializeComponent();

            foreach (string job in removedJobs)
            {
                string[] split = job.Split(" ".ToArray(), 2);
                if (split.Length > 1)
                {
                    this.RemovedJobs.Add(new Job(split[1], split[0]));
                }
                else
                {
                    MessageBox.Show("Fehler beim parsen des Jobs:\n" + job);
                }

            }

            foreach (string job in addedJobs)
            {
                string[] split = job.Split(" ".ToArray(), 2);
                if (split.Length > 1)
                {
                    Job newJob = new Job(split[1], split[0]);
                    this.AddedJobs.Add(newJob);
                }
                else
                {
                    MessageBox.Show("Fehler beim parsen des Jobs:\n" + job);
                }

            }

        }

        void OnHyperlinkClick(object sender, RoutedEventArgs e)
        {
            var context = ((Hyperlink)e.OriginalSource).DataContext;
            if(context != null && context is Job)
            {
                Job job = (Job)context;
                System.Diagnostics.Process.Start(job.Url);
            }
           
        }

    }
}
