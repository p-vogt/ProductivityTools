using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace StartupManager
{
    [Serializable]
    [XmlRoot(ElementName = "Data")]
    public class MainWindowDataModel
    {
        [XmlArray("Tasks")]
        [XmlArrayItem("Task")]
        public ObservableCollection<ManagedTask> Tasks { get; set; } = new ObservableCollection<ManagedTask>();

        public void AddTask(ManagedTask task)
        {
            Tasks.Add(task);
        }

        public override string ToString()
        {
            return Tasks.ToString();
        }
    }
}
