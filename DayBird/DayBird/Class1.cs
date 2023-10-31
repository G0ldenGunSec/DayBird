using System;
using System.Reflection;
using System.IO;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Timers;
using UI;
using Managers;
using Protocol;


public sealed class MyAppDomainManager : AppDomainManager
{
    public override void InitializeNewDomain(AppDomainSetup appDomainInfo)
    {
        Task.Run(() => DelayRun());
    }

    private void DelayRun()
    {
        Thread.Sleep(1000);
        DayBird.Class1 adInjObj = new DayBird.Class1();
        adInjObj.Init_Main();
    }
}


namespace DayBird
{
    public class Class1
    {
        private Type consoleManagerType = null;
        private Type tabMetadataType = null;
        private Type consoleType = null;
        private Type historicCommandType = null;
        private Type mainInterfaceType = null;
        private Thread autoRunThread = null;
        private Type agentManagerType = null;
        private Type operationsManagerType = null;

        public static void testing(Class1 main)
        {
            main.Init_Main();
        }
        public void Init_Main()
        {
            try
            {
                Assembly a = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.FullName.StartsWith("UI,", StringComparison.OrdinalIgnoreCase));
                
                consoleManagerType = a.GetType("UI.ConsoleWindowManager");
                consoleType = a.GetType("UI.ConsoleWindow");
                mainInterfaceType = a.GetType("UI.MainInterface");
                operationsManagerType = a.GetType("Managers.OperationsManager");
                tabMetadataType = a.GetType("UI.TabMetadata");
                historicCommandType = a.GetType("UI.ConsoleWindow+HistoricCommand");
                agentManagerType = a.GetType("Managers.AgentManager");                
            }
            catch (Exception e)
            {
                MessageBox.Show("Assembly enumeration error \n" + e.ToString());
                return;
            }
            try
            {
                AddNewMenuItem();
                //if there is an autoruns dir + plugins in the autoruns folder, start autorun functionality
                if (Directory.Exists("Autoruns") && Directory.GetFiles("Autoruns").Count() > 0)
                {
                    Task.Run(() => ManageAutoRuns(agentManagerType, operationsManagerType));
                }

                //load event handlers here
                Managers.OperationsManager.NotifyCommandResultReceived += new Managers.CommandResultReceivedDelegate(this.WhoamiResultReceived);
                Managers.OperationsManager.NotifyCommandResultReceived += new Managers.CommandResultReceivedDelegate(this.EnumProcessesResultReceived);
                Managers.OperationsManager.NotifyCommandResultReceived += new Managers.CommandResultReceivedDelegate(this.ExecuteBofResultReceived);
                Managers.OperationsManager.NotifyCommandResultReceived += new Managers.CommandResultReceivedDelegate(this.ImpersonateResultReceived);
                Managers.OperationsManager.NotifyCommandResultReceived += new Managers.CommandResultReceivedDelegate(this.LinkAgentResultReceived);
                Managers.OperationsManager.NotifyCommandResultReceived += new Managers.CommandResultReceivedDelegate(this.ChangeDirectoryResultReceived);
                Managers.OperationsManager.NotifyCommandResultReceived += new Managers.CommandResultReceivedDelegate(this.GetCurrentDirectoryPathResultReceived);
                Managers.OperationsManager.NotifyCommandResultReceived += new Managers.CommandResultReceivedDelegate(this.CreateDirectoryResultReceived);
                Managers.OperationsManager.NotifyCommandResultReceived += new Managers.CommandResultReceivedDelegate(this.DeleteDirectoryResultReceived);
                Managers.OperationsManager.NotifyCommandResultReceived += new Managers.CommandResultReceivedDelegate(this.DeleteFileResultReceived);
                Managers.OperationsManager.NotifyCommandResultReceived += new Managers.CommandResultReceivedDelegate(this.RenameFileResultReceived);
                Managers.OperationsManager.NotifyCommandResultReceived += new Managers.CommandResultReceivedDelegate(this.GetDirectoryResultReceived);
                Managers.OperationsManager.NotifyCommandResultReceived += new Managers.CommandResultReceivedDelegate(this.ExecuteAssemblyResultReceived);
                Managers.OperationsManager.NotifyCommandResultReceived += new Managers.CommandResultReceivedDelegate(this.InjectShellcodeResultReceived);
                Managers.FileTransferManager.NotifyFileDownloadComplete += new FileDownloadStatusDelegate(this.DownloadFileSuccessResultReceived);
                Managers.FileTransferManager.NotifyFileDownloadError += new FileDownloadStatusDelegate(this.DownloadFileErrorResultReceived);
                Managers.FileTransferManager.NotifyFileUploadError += new FileUploadStatusDelegate(this.UploadFileErrorResultReceived);
                Managers.FileTransferManager.NotifyFileUploadComplete += new FileUploadStatusDelegate(this.UploadFileSuccessResultReceived);


                List<Guid> updatedConsoles = new List<Guid>();
                while (1==1)
                {
                    Thread.Sleep(1000);
                    object consoleList = consoleManagerType.GetField("_consoles", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                    IEnumerable<Guid> consoleKeys = consoleList.GetType().GetProperty("Keys", BindingFlags.Instance | BindingFlags.Public).GetValue(consoleList) as IEnumerable<Guid>;

                    foreach (Guid consoleGuid in consoleKeys)
                    {
                        if (updatedConsoles.IndexOf(consoleGuid) > -1)
                        {
                            continue;
                        }
  
                        try
                        {
                            object consoleWindow = consoleManagerType.GetMethod("Get", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[] { consoleGuid });
                            if (consoleWindow != null)
                            {   
                                RichTextBox inputBox = (RichTextBox)consoleType.GetField("_input", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(consoleWindow);
                                inputBox.PreviewKeyDown += new PreviewKeyDownEventHandler(PreviewTextInput);
                                inputBox.KeyUp += new KeyEventHandler(RTBCleanup);
                                updatedConsoles.Add(consoleGuid);
                            }
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show("Unable to get console window obj for: " + consoleGuid + "\n" + e.ToString());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Object Retrieval Error:\n" + e.ToString());
                return;
            } 
        }

        private HashSet<Guid> getPreviousConnections()
        {
            string[] logPath = Directory.GetCurrentDirectory().Split('\\');
            string formattedPath = string.Join("\\", logPath.Take(logPath.Length - 2)) + "\\logs";

            HashSet<Guid> existingConnections = new HashSet<Guid>();
            try
            {
                //get logfile dir
                string[] logFiles = Directory.GetFiles(formattedPath);

                //check each xml file in logdir
                foreach(string filePath in logFiles)
                {
                    //if error parsing any file names continue to next file instead of backing out completely
                    try
                    {
                        if (!filePath.EndsWith(".xml"))
                        {
                            continue;
                        }
                        //parse log file name, remove filepath and .xml, then remove "console-" from front
                        string fileName = filePath.Split('\\').Last().Split('.')[0].Substring(8);                   
                        existingConnections.Add(new Guid(fileName));
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            catch
            {
                MessageBox.Show("[X] Error: Unable to locate NightHawk's 'logs' folder, will not proceed with autoruns");
                return null;
            }
            return existingConnections;
        }

        //this method is needed to successfully close the STAThread managing autorun functionality, otherwise the thread doesnt close and the process stays alive even after the GUI closes.
        private void OnApplicationExit(object sender, EventArgs e)
        {
            try
            {
                if(autoRunThread != null)
                {
                    autoRunThread.Abort();
                }
            }
            catch { }
        }

        private void AutoRunPlugins(Type agentManagerType, Type operationsManagerType)
        {            
            //create dialog box to manage configuration of autorun plugins that will be ran
            AutoRunForm baseForm = new AutoRunForm();          
            baseForm.ShowDialog();
            baseForm.Dispose();
            return;            
        }
               

        public void ManageAutoRuns(Type agentManagerType, Type operationsManagerType)
        {
            //wait until console is connected to a team server
            while (operationsManagerType.GetField("_info", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) == null)
            {
                Thread.Sleep(1000);
            }

            //once connected, wait 20s for agents to load in before starting autorun polling
            Thread.Sleep(20000);
                        
            //should now have all config information for plugins - which ones are we running, order to run in, args, etc

            //get all historical connection GUIDs from log files
            HashSet<Guid> allConnections = getPreviousConnections();
            //add all registered beacons (agents) to our hashset
            ConcurrentDictionary<Guid, Managers.AgentInfo> registeredAgents = (ConcurrentDictionary<Guid, Managers.AgentInfo>)agentManagerType.GetField("_agents", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            foreach (Guid liveAgentGuid in registeredAgents.Keys)
            {
                allConnections.Add(liveAgentGuid);
            }


            //begin main loop that looks for any new agent connections. Queries once every 2 seconds
            while (1 == 1)
            {
                registeredAgents = (ConcurrentDictionary<Guid, Managers.AgentInfo>)agentManagerType.GetField("_agents", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

                foreach (Guid liveAgentGuid in registeredAgents.Keys)
                {
                    //new agent connection
                    if (!allConnections.Contains(liveAgentGuid))
                    {
                        //dont do anything until "full" connect where detailed info is populated for the beacon
                        if (registeredAgents[liveAgentGuid].DetailedInfo != null)
                        {
                            allConnections.Add(liveAgentGuid);

                            //skip any further execution if no autorun scripts are currently enabled
                            if(PluginManager.LoadAutoRuns().Where(x => x.Priority > -1).Count() == 0)
                            {
                                continue;
                            }

                            //create a console object so we expose the methods we need to run commands on the beacon
                            //open this new console in the GUI so the UI doesn't freak out if the operator attempts to open the new agent while autoruns are running
                            UI.MainInterface nhInterface = UI.MainInterface.Instance;
                            
                            nhInterface.Invoke(new Action(() => { OpenNewBeacon(liveAgentGuid, nhInterface); }));
                            
                            object consoleWindow = consoleManagerType.GetMethod("Get", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[] { liveAgentGuid });
                            //hand running scripts off to another thread so that this thread can keep looping to look for new connections
                            Task.Run(()=> { Thread.Sleep(3000); PluginManager.ExecAutoRuns(consoleWindow, consoleType, historicCommandType); });
                        }
                    }
                }
                Thread.Sleep(2000);
            }
        }

        public void OpenNewBeacon(Guid liveAgentGuid, UI.MainInterface nhInterface)
        {
            TabPage tabPage = (TabPage)mainInterfaceType.GetMethod("FindExistingTab", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(nhInterface, new object[] { UI.TabType.Console, liveAgentGuid });
            UI.ConsoleWindow consoleWindow = null;
            TabControl tabView = (TabControl)mainInterfaceType.GetField("_tabbedView", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(nhInterface);

            if (tabPage == null)
            {
                tabPage = new TabPage();
                tabPage.Bounds = new Rectangle(0, 0, tabView.Width, tabView.Height);
                tabPage.BackColor = UI.Config.ConsoleUI.Layout.BackgroundColor;
                consoleWindow = UI.ConsoleWindowManager.Create(liveAgentGuid, tabPage.Bounds);

                tabPage.Text = consoleWindow.GetTitle();
                tabPage.Controls.Add((Control)consoleWindow.GetPanel());
                object tabMetadata = Activator.CreateInstance(tabMetadataType, new object[] { });

                tabMetadataType.GetProperty("Type").SetValue(tabMetadata, UI.TabType.Console);
                tabMetadataType.GetProperty("Metadata").SetValue(tabMetadata, (object)liveAgentGuid);
                tabPage.Tag = (object)tabMetadata;
                tabView.TabPages.Add(tabPage);
            }
            //if page already exists -- operator has manually opened the tab in the GUI instead of us populating it above
            else
            {
                //in this case, manually retrieve the newly created ConsoleWindow obj
                consoleWindow = (UI.ConsoleWindow)consoleManagerType.GetMethod("Get", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[] { liveAgentGuid });
            }
            consoleWindow.Restore();
            tabView.SelectedTab = tabPage;
        }

        public void AddNewMenuItem()
        {
            UI.MainInterface nhInterface = UI.MainInterface.Instance;
            MenuStrip interfaceMenu = (MenuStrip)mainInterfaceType.GetField("topMenu", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(nhInterface);

            ToolStripMenuItem autoRunsMenu = new ToolStripMenuItem();
            autoRunsMenu.Name = "autoRunsMenu";
            autoRunsMenu.Size = new Size((int)sbyte.MaxValue, 38);
            autoRunsMenu.Text = "AutoRuns";

            ToolStripMenuItem manageAutoRunsDropdown = new ToolStripMenuItem();
            manageAutoRunsDropdown.Name = "dropdownTest";
            manageAutoRunsDropdown.Size = new Size(284, 44);
            manageAutoRunsDropdown.Text = "Manage AutoRuns";
            manageAutoRunsDropdown.Click += new EventHandler(this.ManageAutoRuns_Click);
            autoRunsMenu.DropDownItems.Add(manageAutoRunsDropdown);

            ToolStripMenuItem checkAutoRunsDropdown = new ToolStripMenuItem();
            checkAutoRunsDropdown.Name = "dropdownTest";
            checkAutoRunsDropdown.Size = new Size(284, 44);
            checkAutoRunsDropdown.Text = "View Enabled";
            checkAutoRunsDropdown.Click += new EventHandler(this.CheckAutoRuns_Click);
            autoRunsMenu.DropDownItems.Add(checkAutoRunsDropdown);

            //invoke GUI updates on GUI thread
            nhInterface.Invoke(new Action(() => { interfaceMenu.Items.Add(autoRunsMenu); nhInterface.Update(); }));
        }

        public void ManageAutoRuns_Click(object sender, EventArgs e)
        {
            autoRunThread = new Thread(() => AutoRunPlugins(agentManagerType, operationsManagerType));
            autoRunThread.SetApartmentState(ApartmentState.STA);
            autoRunThread.Start();
            autoRunThread.Join();
        }

        public void CheckAutoRuns_Click(object sender, EventArgs e)
        {
            IEnumerable<AssemblyInstance> autoruns = PluginManager.LoadAutoRuns().Where(x => x.Priority > -1).OrderBy(y => y.Priority);
            StringBuilder sb = new StringBuilder("*Enabled AutoRuns below*\n");
            foreach(AssemblyInstance autorun in autoruns)
            {
                sb.Append(autorun.AssemblyName + "\n");
                foreach(KeyValuePair<string,string> reqArg in autorun.RequiredArgs)
                {
                    sb.Append(string.Format("  -->{0}:{1}\n",reqArg.Key,reqArg.Value));
                }
            }            
            MessageBox.Show(sb.ToString(), "DayBird AutoRuns CAW CAW", MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);
        }

        //whoami handler
        public void WhoamiResultReceived(Protocol.CommsProtocolMessage msg)
        {
            //only look at whoami messages that are related to script commands we've queued
            if (msg.Type != Protocol.ECommsProtocolMessageType.CPMT_WHOAMI || !NHAPI.OutputRetrieval.activeScriptJobs.Contains(msg.MessageId))
            {
                return;
            }
            string whoamiResult = Protocol.CmdCtrlMsgBinaryFormat.ParseWhoamiResult(msg);
            NHAPI.OutputRetrieval.returnedJobOutput.TryAdd(msg.MessageId, whoamiResult);
            NHAPI.OutputRetrieval.activeScriptJobs.Remove(msg.MessageId);
        }

        //ps handler
        public void EnumProcessesResultReceived(Protocol.CommsProtocolMessage msg)
        {
            //only look at ps messages that are related to script commands we've queued
            if (msg.Type != Protocol.ECommsProtocolMessageType.CPMT_ENUM_PROCESSES || !NHAPI.OutputRetrieval.activeScriptJobs.Contains(msg.MessageId))
            {
                return;
            }
            Protocol.ProcessInformation[] enumProcessesResult = Protocol.CmdCtrlMsgBinaryFormat.ParseEnumProcessesResult(msg);

            StringBuilder psResult = new StringBuilder();

            foreach(Protocol.ProcessInformation singleProc in enumProcessesResult)
            {
                psResult.Append(singleProc.ImageName + "|");
                psResult.Append(singleProc.Architecture.ToString() + "|");
                psResult.Append(singleProc.Pid + "|");
                psResult.Append(singleProc.ParentPid + "|");
                psResult.Append(singleProc.SessionId + "|");
                psResult.Append(singleProc.Username + "|");
                psResult.Append(singleProc.Integrity.ToString());
                psResult.Append("\n");
            }
            NHAPI.OutputRetrieval.returnedJobOutput.TryAdd(msg.MessageId, psResult.ToString());
            NHAPI.OutputRetrieval.activeScriptJobs.Remove(msg.MessageId);
        }

        public void ExecuteBofResultReceived(Protocol.CommsProtocolMessage msg)
        {
            //only look at bof messages that are related to script commands we've queued
            if (msg.Type != Protocol.ECommsProtocolMessageType.CPMT_EXECUTE_BOF || !NHAPI.OutputRetrieval.activeScriptJobs.Contains(msg.MessageId))
            {
                return;
            }
            BofExecutionResultMessage executeBofResult = CmdCtrlMsgBinaryFormat.ParseExecuteBofResult(msg);
            //if error, log it. otherwise return output data
            if (!executeBofResult.Success)
            {
                string errMsg = "Error executing BOF " + executeBofResult.BofFileName + ": " + executeBofResult.ErrorMessage;
                NHAPI.OutputRetrieval.returnedJobOutput.TryAdd(msg.MessageId, errMsg);
            }
            else
            {                
                NHAPI.OutputRetrieval.returnedJobOutput.TryAdd(msg.MessageId, executeBofResult.TextOutput);
            }
            
            NHAPI.OutputRetrieval.activeScriptJobs.Remove(msg.MessageId);
        }

        public void ImpersonateResultReceived(Protocol.CommsProtocolMessage msg)
        {
            //only look at impersonation messages that are related to script commands we've queued
            if (msg.Type != Protocol.ECommsProtocolMessageType.CPMT_IMPERSONATE_USER || !NHAPI.OutputRetrieval.activeScriptJobs.Contains(msg.MessageId))
            {
                return;
            }
            Tuple<bool, bool, string, string, int> impersonateUserResult = CmdCtrlMsgBinaryFormat.ParseImpersonateUserResult(msg);
            string impersonationResult = "";
            if (impersonateUserResult.Item1)
            {
                impersonationResult = "Impersonation succeeded (user: " + impersonateUserResult.Item3 + (!string.IsNullOrEmpty(impersonateUserResult.Item4) ? ", domain: " + impersonateUserResult.Item4 : "") + ", type: " + (impersonateUserResult.Item2 ? "network" : "interactive") + ")";
            }
            else
            {
                impersonationResult = "Impersonation failed (user: " + impersonateUserResult.Item3 + (!string.IsNullOrEmpty(impersonateUserResult.Item4) ? ", domain: " + impersonateUserResult.Item4 : "") + ", type: " + (impersonateUserResult.Item2 ? "network" : "interactive") + ") (" + Helpers.LastErrorToString(impersonateUserResult.Item5) + ")";
            }
                        
            NHAPI.OutputRetrieval.returnedJobOutput.TryAdd(msg.MessageId, impersonationResult);
            NHAPI.OutputRetrieval.activeScriptJobs.Remove(msg.MessageId);
        }

        public void LinkAgentResultReceived(Protocol.CommsProtocolMessage msg)
        {
            //only look at link messages that are related to script commands we've queued
            if (msg.Type != Protocol.ECommsProtocolMessageType.CPMT_TUNNEL || !NHAPI.OutputRetrieval.activeScriptJobs.Contains(msg.MessageId))
            {
                return;
            }
            string linkResult;
            Tuple<bool, int, string> linkTunnelResult = CmdCtrlMsgBinaryFormat.ParseLinkTunnelResult(msg);
            if (linkTunnelResult.Item1)
            {
                linkResult = "Agent linked on URI " + linkTunnelResult.Item3;
            }
            else
            {
                linkResult = "Failed to link agent on URI " + linkTunnelResult.Item3 + (linkTunnelResult.Item2 != 0 ? " (" + Helpers.LastErrorToString(linkTunnelResult.Item2) + ")" : "");
            }

            NHAPI.OutputRetrieval.returnedJobOutput.TryAdd(msg.MessageId, linkResult);
            NHAPI.OutputRetrieval.activeScriptJobs.Remove(msg.MessageId);
        }

        public void ChangeDirectoryResultReceived(Protocol.CommsProtocolMessage msg)
        {
            if (msg.Type != Protocol.ECommsProtocolMessageType.CPMT_CHANGE_DIRECTORY || !NHAPI.OutputRetrieval.activeScriptJobs.Contains(msg.MessageId))
            {
                return;
            }
            string cdResult;
            Tuple<bool, int, string> changeDirectoryResult = CmdCtrlMsgBinaryFormat.ParseChangeDirectoryResult(msg);
            if (!changeDirectoryResult.Item1)
            {
                cdResult = "Failed to change directory to " + changeDirectoryResult.Item3 + " (" + Helpers.LastErrorToString(changeDirectoryResult.Item2) + ")";
            }
            else
            {
                cdResult = "Changed directory to " + changeDirectoryResult.Item3;
            }
            NHAPI.OutputRetrieval.returnedJobOutput.TryAdd(msg.MessageId, cdResult);
            NHAPI.OutputRetrieval.activeScriptJobs.Remove(msg.MessageId);
        }

        public void GetCurrentDirectoryPathResultReceived(Protocol.CommsProtocolMessage msg) 
        {
            if (msg.Type != Protocol.ECommsProtocolMessageType.CPMT_PRINT_DIRECTORY || !NHAPI.OutputRetrieval.activeScriptJobs.Contains(msg.MessageId))
            {
                return;
            }
            string pwdResult;
            Tuple<bool, int, string> printDirectoryResult = CmdCtrlMsgBinaryFormat.ParsePrintDirectoryResult(msg);
            if (!printDirectoryResult.Item1)
            {
                pwdResult = "Failed to print current directory (" + Helpers.LastErrorToString(printDirectoryResult.Item2) + ")";
            }
            else
            {
                pwdResult = "Current directory " + Config.ConsoleUI.Layout.NormalTextColor.ToTag() + printDirectoryResult.Item3;
            }
            NHAPI.OutputRetrieval.returnedJobOutput.TryAdd(msg.MessageId, pwdResult);
            NHAPI.OutputRetrieval.activeScriptJobs.Remove(msg.MessageId);
        }

        public void CreateDirectoryResultReceived(Protocol.CommsProtocolMessage msg)
        {
            if (msg.Type != Protocol.ECommsProtocolMessageType.CPMT_CREATE_DIRECTORY || !NHAPI.OutputRetrieval.activeScriptJobs.Contains(msg.MessageId))
            {
                return;
            }
            string mkdirResult;
            Tuple<bool, int, string> deleteDirectoryResult = CmdCtrlMsgBinaryFormat.ParseDeleteDirectoryResult(msg);
            if (!deleteDirectoryResult.Item1)
            {
                mkdirResult = "Failed to create directory " + deleteDirectoryResult.Item3 +  " (" + Helpers.LastErrorToString(deleteDirectoryResult.Item2) + ")";
            }
            else
            {
                mkdirResult = "Created directory " + deleteDirectoryResult.Item3;
            }
            NHAPI.OutputRetrieval.returnedJobOutput.TryAdd(msg.MessageId, mkdirResult);
            NHAPI.OutputRetrieval.activeScriptJobs.Remove(msg.MessageId);
        }

        public void DeleteDirectoryResultReceived(Protocol.CommsProtocolMessage msg)
        {
            if (msg.Type != Protocol.ECommsProtocolMessageType.CPMT_REMOVE_DIRECTORY || !NHAPI.OutputRetrieval.activeScriptJobs.Contains(msg.MessageId))
            {
                return;
            }
            string rmdirResult;
            Tuple<bool, int, string> deleteDirectoryResult = CmdCtrlMsgBinaryFormat.ParseDeleteDirectoryResult(msg);
            if (!deleteDirectoryResult.Item1)
            {
                rmdirResult = "Failed to remove directory " + deleteDirectoryResult.Item3 + " (" + Helpers.LastErrorToString(deleteDirectoryResult.Item2) + ")";
            }
            else
            {
                rmdirResult = "Removed directory " + deleteDirectoryResult.Item3;
            }
            NHAPI.OutputRetrieval.returnedJobOutput.TryAdd(msg.MessageId, rmdirResult);
            NHAPI.OutputRetrieval.activeScriptJobs.Remove(msg.MessageId);
        }

        public void DeleteFileResultReceived(Protocol.CommsProtocolMessage msg)
        {
            if (msg.Type != Protocol.ECommsProtocolMessageType.CPMT_DELETE_FILE || !NHAPI.OutputRetrieval.activeScriptJobs.Contains(msg.MessageId))
            {
                return;
            }
            string rmResult;
            Tuple<bool, int, string> deleteDirectoryResult = CmdCtrlMsgBinaryFormat.ParseDeleteDirectoryResult(msg);
            if (!deleteDirectoryResult.Item1)
            {
                rmResult = "Failed to delete file " + deleteDirectoryResult.Item3 + " (" + Helpers.LastErrorToString(deleteDirectoryResult.Item2) + ")";
            }
            else
            {
                rmResult = "Deleted file " + deleteDirectoryResult.Item3;
            }
            NHAPI.OutputRetrieval.returnedJobOutput.TryAdd(msg.MessageId, rmResult);
            NHAPI.OutputRetrieval.activeScriptJobs.Remove(msg.MessageId);
        }

        public void RenameFileResultReceived(Protocol.CommsProtocolMessage msg)
        {
            if (msg.Type != Protocol.ECommsProtocolMessageType.CPMT_MOVE_FILE || !NHAPI.OutputRetrieval.activeScriptJobs.Contains(msg.MessageId))
            {
                return;
            }
            string mvResult;
            Tuple<bool, int, string, string> moveFileResult = CmdCtrlMsgBinaryFormat.ParseMoveFileResult(msg);
            if (!moveFileResult.Item1)
            {
                mvResult = "Failed to rename file " + moveFileResult.Item3 + " (" + Helpers.LastErrorToString(moveFileResult.Item2) + ")";
            }
            else
            {
                mvResult = "Renamed file " + moveFileResult.Item3;
            }
            NHAPI.OutputRetrieval.returnedJobOutput.TryAdd(msg.MessageId, mvResult);
            NHAPI.OutputRetrieval.activeScriptJobs.Remove(msg.MessageId);
        }

        public void GetDirectoryResultReceived(Protocol.CommsProtocolMessage msg)
        {
            if (msg.Type != Protocol.ECommsProtocolMessageType.CPMT_GETDIR || !NHAPI.OutputRetrieval.activeScriptJobs.Contains(msg.MessageId))
            {
                return;
            }
            Tuple<bool, int, string, RemoteFileListingDetails[]> enumDirResult = Protocol.CmdCtrlMsgBinaryFormat.ParseGetDirectoryResult(msg);
            
            StringBuilder lsResult = new StringBuilder();

            if(enumDirResult.Item1 == true)
            {
                lsResult.AppendLine("Directory of " + enumDirResult.Item3);
                foreach (RemoteFileListingDetails objectInfo in enumDirResult.Item4)
                {
                    if (objectInfo.IsDir)
                    {
                        lsResult.Append("Directory|");
                    }
                    else
                    {
                        lsResult.Append("File|");
                    }
                    lsResult.Append(objectInfo.Filename);
                    lsResult.Append(objectInfo.FileSize + "|");
                    lsResult.Append(objectInfo.CreationTime + "|");
                    lsResult.Append(objectInfo.LastAccessTime + "|");
                    lsResult.Append(objectInfo.LastWriteTime + "|");
                    lsResult.Append("\n");
                }
            }
            //if false, failed to read directory
            else
            {
                lsResult.AppendLine(string.Format("Failed to list path {0} ({1})", enumDirResult.Item3,Helpers.LastErrorToString(enumDirResult.Item2)));
            }

            NHAPI.OutputRetrieval.returnedJobOutput.TryAdd(msg.MessageId, lsResult.ToString());
            NHAPI.OutputRetrieval.activeScriptJobs.Remove(msg.MessageId);
        }

        public void ExecuteAssemblyResultReceived(Protocol.CommsProtocolMessage msg)
        {
            if (msg.Type != Protocol.ECommsProtocolMessageType.CPMT_EXECUTE_ASSEMBLY || !NHAPI.OutputRetrieval.activeScriptJobs.Contains(msg.MessageId))
            {
                return;
            }
            CmdCtrlMsgBinaryFormat.ExecuteAssemblyResult executeAssemblyResult = CmdCtrlMsgBinaryFormat.ParseExecuteAssemblyResult(msg);
            //if assembly is finished running
            bool failed = false;
            if(executeAssemblyResult.HasCompleted)
            {
                if (!executeAssemblyResult.Success)
                {
                    failed = true;
                }
                NHAPI.OutputRetrieval.activeScriptJobs.Remove(msg.MessageId);
            }
            if (failed)
            {
                //if true output has already been written for this assembly run, so append to whats already there
                if (NHAPI.OutputRetrieval.returnedJobOutput.TryGetValue(msg.MessageId, out string currentOutput))
                {
                    NHAPI.OutputRetrieval.returnedJobOutput[msg.MessageId] = currentOutput + "Assembly execution failed:\n" + executeAssemblyResult.Output;
                }
                //no output has been written for this assembly yet, add to dictionary
                else
                {
                    NHAPI.OutputRetrieval.returnedJobOutput.TryAdd(msg.MessageId, "Assembly execution failed:\n" + executeAssemblyResult.ErrorLog);
                }
            }
            else
            {
                //if true output has already been written for this assembly run, so append to whats already there
                if (NHAPI.OutputRetrieval.returnedJobOutput.TryGetValue(msg.MessageId, out string currentOutput))
                {
                    NHAPI.OutputRetrieval.returnedJobOutput[msg.MessageId] = currentOutput + executeAssemblyResult.Output;
                }
                //no output has been written for this assembly yet, add to dictionary
                else
                {
                    NHAPI.OutputRetrieval.returnedJobOutput.TryAdd(msg.MessageId, executeAssemblyResult.Output);
                }
            }
        }
        public void InjectShellcodeResultReceived(Protocol.CommsProtocolMessage msg)
        {
            if (msg.Type != Protocol.ECommsProtocolMessageType.CPMT_INJECT_SHELLCODE || !NHAPI.OutputRetrieval.activeScriptJobs.Contains(msg.MessageId))
            {
                return;
            }
            string injectResult;
            Tuple<EProcessInjectionResult, uint, int, int, int> injectProcessResult = CmdCtrlMsgBinaryFormat.ParseInjectProcessResult(msg);
            if (injectProcessResult.Item1 == EProcessInjectionResult.Succeeded)
            {
                if (injectProcessResult.Item3 != 0)
                {
                    injectResult = string.Format("Injection of {0} into process {1} succeeded", msg.Type == ECommsProtocolMessageType.CPMT_INJECT_RDLL ? (object)"reflective DLL" : (object)"shellcode", (object)injectProcessResult.Item3);
                }
                else
                {
                    injectResult = string.Format("Injection of {0} into new process {1} (thread {2}) succeeded", msg.Type == ECommsProtocolMessageType.CPMT_INJECT_RDLL ? (object)"reflective DLL" : (object)"shellcode", (object)injectProcessResult.Item4, (object)injectProcessResult.Item5);
                }
            }
            else
            {
                if (injectProcessResult.Item3 != 0)
                {
                    injectResult = string.Format("Injection of {0} into process {1} failed (failure reason: {2}, lasterror: {3})\n", msg.Type == ECommsProtocolMessageType.CPMT_INJECT_RDLL ? (object)"reflective DLL" : (object)"shellcode", (object)injectProcessResult.Item3, (object)injectProcessResult.Item1, (object)Helpers.LastErrorToString((int)injectProcessResult.Item2));
                }
                else
                {
                    injectResult = string.Format("Injection of {0} into process {1} failed (failure reason {2} lasterror {3})\n", msg.Type == ECommsProtocolMessageType.CPMT_INJECT_RDLL ? (object)"reflective DLL" : (object)"shellcode", (object)injectProcessResult.Item3, (object)injectProcessResult.Item1, (object)Helpers.LastErrorToString((int)injectProcessResult.Item2));
                }
            }
            NHAPI.OutputRetrieval.returnedJobOutput.TryAdd(msg.MessageId, injectResult);
            NHAPI.OutputRetrieval.activeScriptJobs.Remove(msg.MessageId);
        }


        public void DownloadFileSuccessResultReceived(FileDownload download)
        {
            if (download == null || !NHAPI.OutputRetrieval.activeScriptJobs.Contains(download.FileTransferId))
            {
                return;
            }
            string downloadRes = "[" + ConsoleWindow.GetLoggedOnUserAlias() + "] " + "Downloaded file " +  download.RemotePath + " to " + download.LocalPath + " successfully.";
            NHAPI.OutputRetrieval.returnedJobOutput.TryAdd(download.FileTransferId, downloadRes);
            NHAPI.OutputRetrieval.activeScriptJobs.Remove(download.FileTransferId);
        }

        public void DownloadFileErrorResultReceived(FileDownload download)
        {
            if (download == null || !NHAPI.OutputRetrieval.activeScriptJobs.Contains(download.FileTransferId))
            {
                return;
            }
            string downloadRes = "[" + ConsoleWindow.GetLoggedOnUserAlias() + "] " + "Error downloading file " + download.RemotePath + " to " + download.LocalPath;
            NHAPI.OutputRetrieval.returnedJobOutput.TryAdd(download.FileTransferId, downloadRes);
            NHAPI.OutputRetrieval.activeScriptJobs.Remove(download.FileTransferId);
        }

        public void UploadFileSuccessResultReceived(FileUpload upload)
        {
            if (upload == null || !NHAPI.OutputRetrieval.activeScriptJobs.Contains(upload.FileTransferId))
            {
                return;
            }
            string uploadRes = "[" + ConsoleWindow.GetLoggedOnUserAlias() + "] " + "Uploaded file " + upload.LocalPath + " to "  + upload.RemotePath + " successfully.";
            NHAPI.OutputRetrieval.returnedJobOutput.TryAdd(upload.FileTransferId, uploadRes);
            NHAPI.OutputRetrieval.activeScriptJobs.Remove(upload.FileTransferId);
        }

        public void UploadFileErrorResultReceived(FileUpload upload)
        {
            if (upload == null || !NHAPI.OutputRetrieval.activeScriptJobs.Contains(upload.FileTransferId))
            {
                return;
            }
            string uploadRes = "[" + ConsoleWindow.GetLoggedOnUserAlias() + "] " + "Error uploading file " + upload.LocalPath + " to " + upload.RemotePath;
            NHAPI.OutputRetrieval.returnedJobOutput.TryAdd(upload.FileTransferId, uploadRes);
            NHAPI.OutputRetrieval.activeScriptJobs.Remove(upload.FileTransferId);
        }


        private object GetSendingConsole(TabPage sendingTabPage)
        {
            Guid activeConsole = Guid.Empty;
            try
            {
                activeConsole = (Guid)tabMetadataType.GetProperty("Metadata", BindingFlags.Public | BindingFlags.Instance).GetValue(sendingTabPage.Tag);
            }
            catch
            {
                MessageBox.Show("Error retrieving Guid associated with active console window");
                return null;
            }
            try
            {
                object consoleWindow = consoleManagerType.GetMethod("Get", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[] { activeConsole });
                return consoleWindow;
            }
            catch
            {
                MessageBox.Show("Error retrieving ConsoleWindow obj for active console window (GetSendingConsole)");
                return null;
            } 
        }
        
        public void PreviewTextInput(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Return)
            {
                PluginManager.ResetOffset();
                string command = ((RichTextBox)sender).Text.ToLower();
                if (command.StartsWith("plugin"))
                {
                    //attempt to retrieve active console window
                    object activeConsole = GetSendingConsole((TabPage)((RichTextBox)sender).Parent.Parent);
                    if(activeConsole == null)
                    {
                        MessageBox.Show("Unable to retrieve active console");
                        return;
                    }

                    PluginManager.ParseCommand(activeConsole, consoleType, historicCommandType, ((RichTextBox)sender).Text);
                    ((RichTextBox)sender).Text = "";
                }
                else if(command.StartsWith("ps --detailed-info") && command.IndexOf("--skip-processes") == -1 && command.IndexOf("--unsafe") == -1 )
                {
                    MessageBox.Show("WARNING: This command in its current state may alert EDR when grabbing a handle to lsass. Use the '--skip-processes' flag to skip high-risk processes. Alternatively, run with the '--unsafe' flag to ignore this warning.");
                    ((RichTextBox)sender).Text = "";
                }
            }

            //auto-complete for scripts
            else if(e.KeyCode == Keys.Tab)
            {
                if(!Directory.Exists("Plugins"))
                {
                    MessageBox.Show("ERROR: Could not locate 'Plugins' folder. Ensure it exists as a folder in the same directory that UI.exe is running from");
                    return;
                }

                string[] command = ((RichTextBox)sender).Text.ToLower().Split(' ');

                //first arg will always be 'plugin' if there are no other args, return
                if(command.Length < 1)
                {
                    return;
                }
                string autoComplete = PluginManager.AutoComplete(command[command.Length - 1]);

                //return if no match
                if(autoComplete == "")
                {
                    return;
                }

                //if match, update text to match
                StringBuilder sb = new StringBuilder();
                for(int i = 0; i < command.Length - 1; i++)
                {
                    sb.Append(command[i] + " ");
                }
                sb.Append(autoComplete);
                ((RichTextBox)sender).Clear();
                ((RichTextBox)sender).Text = sb.ToString();
                ((RichTextBox)sender).Select(((RichTextBox)sender).Text.Length, 0);
            }
        }

        public void RTBCleanup(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return)
            {
                ((RichTextBox)sender).Clear();
            }
        }       
    }
}
