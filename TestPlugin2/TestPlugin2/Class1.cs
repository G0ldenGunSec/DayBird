using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHAPI;

namespace TestPlugin2
{
    public class Plugin : NHAPI.PluginBase
    {
        //required construcor to pass associated Console object to base class.
        public Plugin(object console) : base(console) { }

        //put names of any required args here. These must exactly match any arg names your plugin will be looking for. This will be used if the plugin is configured to run as an autorun so that valuse can be set prior to exectuion.
        public override string[] RequiredArgs => new string[]{"port", "host"};

        //general help text associated with the plugin goes here
        protected override string HelpText => "Checks if a port on a given host is open/closed. Args: \r\n    /host: hostname/IP of target\r\n    /port: target port";

        //Note: args will be null if no parameters are passed in
        public override void Run(string args, DetailedMachineInfo detailedInfo)
        {
            //added this here since this particular plugin requires arguments
            if (args == null)
            {
                nhFunctions.PrintToConsole("Error - args are required", NHAPI.TextColors.Red);
                Help();
                return;
            }

            //Parse arguments using NHAPI's built-in arg parser. Could also implement your own arg handling that parses the args string directly.
            Dictionary<string, string> parsedArgs = ArgUtil.ParseArgumentsFromString(args);

            if(!parsedArgs.ContainsKey("port") || !parsedArgs.ContainsKey("host"))
            {
                nhFunctions.PrintToConsole("Error - include both target port and host", NHAPI.TextColors.Red);
                return;
            }

            List<BofArg> typedArgs = new List<BofArg>();

            typedArgs.Add(new BofArg(DataType.ascii_string, parsedArgs["host"]));
            typedArgs.Add(new BofArg(DataType.int_val, parsedArgs["port"]));

            string packedArgs = nhFunctions.BofPack(typedArgs);

            nhFunctions.PrintToConsole("Example script providing front-end arg parsing + execution for TrustedSec's Probe BOF", NHAPI.TextColors.LightSalmon);
            
            //If you just wanted to run the BOF "blind" without getting output back, you could run it with the below (commented out) command
            //Not getting output back means not having access to the result within the logic of the script. As this calls the UI method to run the BOF, the result will be displayed to screen either way
            //Getting the output back is mainly useful for if you want to have more logic in your script that performs additional functions based on initial results (e.g. if a named pipe exists, run xyz BOF)
            //nhFunctions.ExecuteBof("probe.x64.o", "go",packedArgs);

            //Alternatively, if you're interested in getting output back you can run the "retrieveResults" command variant. This will give you a Guid value for the command
            //this Guid can then be used to retrieve any results returned from the command

            //send executeBof command to associated beacon
            Guid retVal = nhFunctions.ExecuteBof_RetrieveResults("probe.x64.o", "go", packedArgs);

            //Wait for results, with a default timeout val of 30s. If exec doesnt finish / beacon doesnt check back in before time elapses an empty string is returned.
            string bofResults = OutputRetrieval.GetJobOutput(retVal);

            //printing out the results to screen here
            nhFunctions.PrintToConsole("Received text back from bof in our script: " + bofResults, NHAPI.TextColors.LightGreen);

            if(bofResults.Contains("OPEN"))
            {
                nhFunctions.PrintToConsole(string.Format("Script identified port {0} on {1} is open. Could automate further execution here to move laterally", parsedArgs["port"], parsedArgs["host"]), NHAPI.TextColors.White);
            }
        }
    }
}
