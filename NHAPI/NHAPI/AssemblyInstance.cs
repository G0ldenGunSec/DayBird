using System;
using System.Reflection;

namespace NHAPI
{
    public class AssemblyInstance
    {
        public string AssemblyName { get; set; }
        public DateTime LastModified { get; set; }
        public long AssemblySize { get; set; }
        public Assembly AssemblyObj { get; set; }


        public AssemblyInstance(string AssemblyName)
        { this.AssemblyName = AssemblyName; }

        public AssemblyInstance(string AssemblyName, DateTime LastModified, long AssemblySize, Assembly AssemblyObj)
        {
            this.AssemblyName = AssemblyName;
            this.LastModified = LastModified;
            this.AssemblySize = AssemblySize;
            this.AssemblyObj = AssemblyObj;
        }
    }
}

