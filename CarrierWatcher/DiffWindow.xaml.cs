using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace CarrierWatcher
{
    /// <summary>
    /// Interaktionslogik für DiffWindow.xaml
    /// </summary>
    public partial class DiffWindow : Window
    {
        public struct Job
        {
            public Job(string name, string department, string location)
            {
                Name = name;
                Department = department;
                Location = location;
            }
            public string Name { get; set; }
            public string Department { get; set; }
            public string Location { get; set; }
        }
        public ObservableCollection<Job> RemovedJobs { get; set; } = new ObservableCollection<Job>();
        public ObservableCollection<Job> AddedJobs { get; set; } = new ObservableCollection<Job>();

        public DiffWindow(List<string> removedJobs, List<string> addedJobs)
        {
            InitializeComponent();

            foreach (string job in removedJobs)
            {
                var split = job.Split(new string[] { "##" }, System.StringSplitOptions.None);
                if (split.Length > 2)
                {
                    this.RemovedJobs.Add(new Job(split[0], split[1], split[2]));
                }
                else
                {
                    MessageBox.Show("Fehler beim parsen des Jobs:\n" + job);
                }

            }

            foreach (string job in addedJobs)
            {
                var split = job.Split(new string[] { "##" }, System.StringSplitOptions.None);
                if (split.Length > 2)
                {
                    Job newJob = new Job(split[0], split[1], split[2]);
                    this.AddedJobs.Add(newJob);
                }
                else
                {
                    MessageBox.Show("Fehler beim parsen des Jobs:\n" + job);
                }

            }

        }

    }
}
