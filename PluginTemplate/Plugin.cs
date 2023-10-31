using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHAPI;


namespace PluginTemplate
{
    //If you change the class name from Plugin, just ensure any other classes in your plugin dont implement a 'Run' method.
    //By default DayBird will attempt to load the 'Plugin' type, but if it it can't find the type will find the first available type that contains a 'Run' method.
    internal class Plugin : NHAPI.PluginBase
    {
        //Required construcor to pass associated Console object to base class.
        public Plugin(object console) : base(console) { }

        //Put names of any required args here in a string array. These must exactly match any arg names your plugin will be looking for. This will be used if the plugin is configured to run as an autorun so that valuse can be set prior to exectuion.
        public override string[] RequiredArgs => new string[] { };

        //General help text associated with the plugin goes here
        protected override string HelpText => "Basic template for a plugin";

        //Note: args will be null if no parameters are passed in
        public override void Run(string args, DetailedMachineInfo detailedInfo)
        {
            //plugin logic, function calls, etc. go here.
            Dictionary<string, string> parsedArgs = new Dictionary<string, string>();
            if(args != null)
            {
                parsedArgs = ArgUtil.ParseArgumentsFromString(args);
            }
        }
    }
}
