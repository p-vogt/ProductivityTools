using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeedChecker
{
    public class ILIASCourse
    {
        public string title = "";
        public string link = "";
        public string description = "";
        public string pubDate = "";
        public string guid = "";
        public override bool Equals(object Obj)
        {
            ILIASCourse other = (ILIASCourse)Obj;
            return (this.title == other.title &&
                this.link == other.link &&
                this.description == other.description &&
                this.pubDate == other.pubDate &&
                this.guid == other.guid);
        }
    }

  
}
