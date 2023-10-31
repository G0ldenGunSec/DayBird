using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace DayBird
{
    public class AssemblyInstance
    {
        public string AssemblyName { get; set; }
        public DateTime LastModified { get; set; }
        public long AssemblySize { get; set; }
        public Assembly AssemblyObj { get; set; }
        public string RequiresConfig { get; set; } = "";
        public int Priority { get; set; } = -1;
        public string dgvPriority { get; set; } = "Set";
        public string dgvEnabled { get; set; } = "false";

        public Dictionary<string, string> RequiredArgs { get; set; }

        public AssemblyInstance(string AssemblyName)
        { this.AssemblyName = AssemblyName; }

        public AssemblyInstance(string AssemblyName, DateTime LastModified, long AssemblySize, Dictionary<string, string> RequiredArgs, Assembly AssemblyOb)
        {
            this.AssemblyName = AssemblyName;
            this.LastModified = LastModified;
            this.AssemblySize = AssemblySize;
            this.RequiredArgs = RequiredArgs;
            this.AssemblyObj = AssemblyObj;
        }
    }
}
