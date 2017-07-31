using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace StartupManager
{
    public class Task
    {
        public Task() : this("",0,"",StartupAction.Start,true)
        {
      
        }
        public Task(string name, int startTimeMs, string filePath, StartupAction action, bool isActivated )
        {
            Name = name;
            StartTime = startTimeMs;
            FilePath = filePath;
            IsActivated = isActivated;
            Action = action;
        }
        public bool IsActivated { get; set; }
        public string Name { get; set; }
        public int StartTime { get; set; }
        public StartupAction Action { get; set; }
        public string FilePath { get; set; }


        public bool Start()
        {
            if(File.Exists(FilePath))
            {
                Process.Start(FilePath);
                return true;
            }
            else
            {
                return false;
            }
          
        }


    }
}
