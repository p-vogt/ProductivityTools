using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace StartupManager
{
    [Serializable]
    [XmlRoot(ElementName = "Data")]
    public class MainWindowDataModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [XmlArray("Tasks")]
        [XmlArrayItem("Task")]
        public ObservableCollection<ManagedTask> Tasks { get; set; } = new ObservableCollection<ManagedTask>();
        
        public int ElapsedTime
        {
            get
            {
                return elapsedTime;
            }

            set
            {
                elapsedTime = value;
                OnPropertyChanged();
            }
        }

        private int elapsedTime = 0;
        public void AddTask(ManagedTask task)
        {
            Tasks.Add(task);
        }

        public override string ToString()
        {
            return Tasks.ToString();
        }

        public int ActiveTasks => (from task in Tasks where task.IsActivated == true select task).Count();
        public int NumOfTaskRemaining { get; set; }
    }
}
