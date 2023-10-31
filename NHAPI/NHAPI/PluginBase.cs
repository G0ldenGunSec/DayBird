using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Drawing;
using System.Windows.Forms;


namespace NHAPI
{
    public abstract class PluginBase
    {

        protected Functions nhFunctions;

        //currentAgent references ConsoleWindow object associated with this script (which agent will the plugin run on)
        public PluginBase(object currentAgent)
        {
            if(currentAgent == null)
            {
                return;
            }
            nhFunctions = new Functions(currentAgent);
        }

        //Property that identifies any arguments defined as required (non-optional) by the designer of the implemented plugin.
        public abstract string[] RequiredArgs
        {
            get;
        }

        //Property that defines the text of the help message for the implemented plugin
        protected abstract string HelpText
        {
            get;
        }

        //abstract method -- for plugin developer to implement main execution functionality
        public abstract void Run(string args, DetailedMachineInfo agentInfo);


        //implemented methods below, these are used by plugins but shouldn't need to be modified

        //display help text for the plugin
        public void Help()
        {
            nhFunctions.PrintToConsole(HelpText, TextColors.Yellow,false,false);
        }  
    }
}
