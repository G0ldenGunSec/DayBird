using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;

namespace NHAPI
{
    public class Functions
    {
        private static Type agentType = null;
        private object CurrentAgent;
        private MethodInfo writeColourToDisplay;
        private MethodInfo writeColour;

        public Functions(object CurrentAgent)
        {
            this.CurrentAgent = CurrentAgent;
            if (agentType == null)
            {
                agentType = CurrentAgent.GetType();
            }
            writeColourToDisplay = agentType.GetMethod("WriteColourToDisplay", BindingFlags.NonPublic | BindingFlags.Instance);
            writeColour = agentType.GetMethod("WriteColour", BindingFlags.Public | BindingFlags.Instance);
        }


        //write text to the local console, and optionally sync across all operators' consoles
        public void PrintToConsole(string toPrint, TextColors textColor = TextColors.Yellow, bool saveToHistory = true, bool toSync = true)
        {
            Color textColorObj = Color.FromName(textColor.ToString());
            toPrint = ColorToTag(textColorObj) + toPrint + "\n";
            if (toSync)
            {
                writeColour.Invoke(CurrentAgent, new object[] { toPrint });
            }
            else
            {
                writeColourToDisplay.Invoke(CurrentAgent, new object[] { toPrint, saveToHistory, true });
            }
        }

        public static string ColorToTag(Color color)
        {
            return string.Format("<!color={0}!>", (object)color.ToArgb());
        }

        private Guid GetLastGUID()
        {
            List<Guid> results = (List<Guid>)agentType.GetField("_pendingCommandResults", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(CurrentAgent);

            //if there are GUIDs, return last (newest addition to list)
            if (results.Count > 0)
            {
                return results.Last();
            }
            //return this to avoid errors when querying empty list
            else
            {
                return Guid.Empty;
            }
        }

        //handles actual calling of whatever NH function we want to interact with, added this to avoid copy-pasting the GUID checking code into every method
        private Guid CallFunctionGetGuid(MethodInfo functionToCall, object[] functionArgs)
        {
            //get last GUID in collection of pending GUIDs before running command, then run command, then get the new last GUID. This should be the GUID of the command we just ran.
            Guid initialGuid = GetLastGUID();

            //initiate transfer of target file. If calls fails return guid.empty
            if (!(bool)functionToCall.Invoke(CurrentAgent, functionArgs))
            {
                return Guid.Empty;
            }

            Guid updatedGuid = GetLastGUID();

            //if the GUID hasn't been added yet, keep looping until we find it, wait up to 10 seconds.
            bool foundGuid = false;
            for (int i = 0; i < 10; i++)
            {
                if (initialGuid.Equals(updatedGuid))
                {
                    Thread.Sleep(500);
                    updatedGuid = GetLastGUID();
                }
                else
                {
                    i = 10;
                    foundGuid = true;
                }
            }
            if (foundGuid)
            {
                OutputRetrieval.activeScriptJobs.Add(updatedGuid);
                return updatedGuid;
            }

            return Guid.Empty;
        }

        public Guid CallFileFunctionGetGuid(MethodInfo functionToCall, object[] functionArgs, TransferType transferType)
        {
            Assembly UI = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name.Equals("UI"));
            Type[] uiTypes = UI.GetTypes();
            Type testType = uiTypes.FirstOrDefault(x => x.Name == "FileTransferManager");

            //get all existing guids associated with file transfers
            List<Guid> previousFileTransfers = GetFileTransfers(testType, transferType);

            //initiate transfer of target file. If calls fails return guid.empty
            if(!(bool)functionToCall.Invoke(CurrentAgent, functionArgs))
            {
                return Guid.Empty;
            }

            //get all guids associated with file transfers then do some linq query from StackOverflow to find your new guid val, this is the guid associated with your transfer job
            List<Guid> updatedFileTransfers = GetFileTransfers(testType, transferType);
            Guid transferGuid = updatedFileTransfers.FirstOrDefault(x => !previousFileTransfers.Any(x2 => x2.Equals(x)));
            
            //if the GUID hasn't been added yet, keep looping until we find it, wait up to 10 seconds.
            if (transferGuid == null)
            {                
                bool foundGuid = false;
                for (int i = 0; i < 10; i++)
                {
                    Thread.Sleep(1000);
                    updatedFileTransfers = GetFileTransfers(testType, transferType);
                    transferGuid = updatedFileTransfers.FirstOrDefault(x => !previousFileTransfers.Any(x2 => x2.Equals(x)));

                    if (transferGuid != null)
                    {
                        i = 10;
                        foundGuid = true;
                    }
                }
                if (!foundGuid)
                {
                    return Guid.Empty;
                }
            }
            OutputRetrieval.activeScriptJobs.Add(transferGuid);
            return transferGuid;
        }

        private List<Guid> GetFileTransfers(Type testType, TransferType transferType)
        {
            if (transferType == TransferType.download)
            {
                return (List<Guid>)testType.GetMethod("GetDownloadTransferIds", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
            }
            else
            {
                return (List<Guid>)testType.GetMethod("GetUploadTransferIds", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
            }
        }

        
        public static string PS(object CurrentAgent)
        {
            return "";
        }

        public bool WhoAmI()
        {
            return (bool)agentType.GetMethod("Whoami", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(CurrentAgent, new object[] { "" });
        }

        public Guid WhoAmI_RetrieveResults()
        {
            return CallFunctionGetGuid(agentType.GetMethod("Whoami", BindingFlags.Instance | BindingFlags.NonPublic), new object[] { "" });
        }



        private string ProcessListArgBuilder(bool detailed = false, bool onlyInjectable = false, string skipProcs = "lsass.exe")
        {
            string jobParams = "";
            if (detailed)
            {
                jobParams += "--detailed-info ";
            }

            if (onlyInjectable)
            {
                jobParams += "--injectable ";
            }

            if (skipProcs != "")
            {
                jobParams += "--skip-processes=";
                string[] allSkips = skipProcs.Split(',');
                for (int i = 0; i < allSkips.Length; i++)
                {
                    if (!allSkips[i].ToLower().EndsWith(".exe"))
                    {
                        allSkips[i] += ".exe";
                    }
                    jobParams += allSkips[i];
                    if (i < allSkips.Length - 1)
                    {
                        jobParams += ",";
                    }
                }
            }
            return jobParams;
        }

        public bool ProcessList(bool detailed = false, bool onlyInjectable = false, string skipProcs = "lsass.exe")
        {
            return (bool)agentType.GetMethod("EnumProcesses", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(CurrentAgent, new object[] { ProcessListArgBuilder(detailed, onlyInjectable, skipProcs) });
        }

        public Guid ProcessList_RetrieveResults(bool detailed = false, bool onlyInjectable = false, string skipProcs = "lsass.exe")
        {
            return CallFunctionGetGuid(agentType.GetMethod("EnumProcesses", BindingFlags.Instance | BindingFlags.NonPublic), new object[] { ProcessListArgBuilder(detailed, onlyInjectable, skipProcs) });
        }






        public Guid ExecuteBof_RetrieveResults(string bofName, string entryPoint, string packedBofArgs = null)
        {
            string fullCommand = PrepBofArgs(bofName, entryPoint, packedBofArgs);
            if (fullCommand == null)
            {
                return Guid.Empty;
            }
            return CallFunctionGetGuid(agentType.GetMethod("ExecuteBof", BindingFlags.Instance | BindingFlags.NonPublic), new object[] { fullCommand });
        }


        public bool ExecuteBof(string bofName, string entryPoint, string packedBofArgs = null)
        {
            string fullCommand = PrepBofArgs(bofName, entryPoint, packedBofArgs);
            if (fullCommand == null)
            {
                return false;
            }
            bool status = (bool)agentType.GetMethod("ExecuteBof", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(CurrentAgent, new object[] { fullCommand });
            return status;
        }

        private string PrepBofArgs(string bofName, string entryPoint, string packedBofArgs = null)
        {
            //locate BOF
            string fullPath = Directory.GetCurrentDirectory();
            if(bofName.IndexOf(":\\") > -1 && File.Exists(fullPath))
            {
                fullPath = bofName;
            }
            else if (File.Exists(fullPath + "\\Plugins\\" + bofName))
            {
                fullPath = fullPath + "\\Plugins\\" + bofName;
            }
            else if (File.Exists(fullPath + "\\Autoruns\\" + bofName))
            {
                fullPath = fullPath + "\\Autoruns\\" + bofName;
            }
            else if (File.Exists(fullPath + "\\" + bofName))
            {
                fullPath = fullPath + "\\" + bofName;
            }
            else
            {
                MessageBox.Show("Error unable to locate compiled BOF script. Make sure you're using full file name including extension in script and it's located in Plugins folder");
                return null;
            }

            string fullCommand = fullPath + " " + entryPoint;

            if (packedBofArgs != null)
            {
                fullCommand += " " + packedBofArgs;
            }
            return fullCommand;
        }

        public string BofPack(List<BofArg> argsToPack)
        {
            StringBuilder packedArgs = new StringBuilder();

            foreach (BofArg toPack in argsToPack)
            {
                if (toPack.argType == DataType.ascii_string)
                {
                    packedArgs.Append("z\"" + toPack.strVal + "\"");
                }
                else if (toPack.argType == DataType.wide_string)
                {
                    packedArgs.Append("Z\"" + toPack.strVal + "\"");
                }
                else if (toPack.argType == DataType.int_val)
                {
                    packedArgs.Append("i" + toPack.strVal);
                }
                else if (toPack.argType == DataType.short_val)
                {
                    packedArgs.Append("s" + toPack.strVal);
                }
                else if (toPack.argType == DataType.bytes_as_hexStr)
                {
                    packedArgs.Append("b" + toPack.strVal);
                }
                packedArgs.Append(" ");
            }
            packedArgs.Length--;

            return packedArgs.ToString();
        }

        public bool Sleep(int intervalSeconds, int jitterPercentage)
        {
            string sleepParams = string.Format(intervalSeconds + " " + jitterPercentage);
            return (bool)agentType.GetMethod("UpdateSleep", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(CurrentAgent, new object[] { sleepParams });
        }

        public bool ImpersonateUser(string user, string pw, ImpersonationType impersonationContext)
        {
            string impersonationParams = "";

            if (impersonationContext == ImpersonationType.network)
            {
                impersonationParams += "--network ";
            }
            impersonationParams += user + " " + pw;

            return (bool)agentType.GetMethod("ImpersonateUser", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(CurrentAgent, new object[] { impersonationParams });
        }
        public Guid ImpersonateUser_RetrieveResults(string user, string pw, ImpersonationType impersonationContext)
        {
            string impersonationParams = "";

            if (impersonationContext == ImpersonationType.network)
            {
                impersonationParams += "--network ";
            }
            impersonationParams += user + " " + pw;

            return CallFunctionGetGuid(agentType.GetMethod("ImpersonateUser", BindingFlags.Instance | BindingFlags.NonPublic), new object[] { impersonationParams });
        }

        public bool RevertToSelf()
        {
            return (bool)agentType.GetMethod("RevertToSelf", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(CurrentAgent, new object[] { "" });
        }

        public bool Link(string remoteHost, string PortOrPipe, LinkType linkType)
        {
            string linkParams;
            if (linkType == LinkType.smb)
            {
                linkParams = string.Format("smb://{0}/{1}", remoteHost, PortOrPipe);
            }
            else
            {
                linkParams = string.Format("tcp://{0}/{1}", remoteHost, PortOrPipe);
            }
            return (bool)agentType.GetMethod("LinkPeerToPeerAgent", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(CurrentAgent, new object[] { linkParams });
        }

        public Guid Link_RetrieveResults(string remoteHost, string PortOrPipe, LinkType linkType)
        {
            string linkParams;
            if (linkType == LinkType.smb)
            {
                linkParams = string.Format("smb://{0}/{1}", remoteHost, PortOrPipe);
            }
            else
            {
                linkParams = string.Format("tcp://{0}/{1}", remoteHost, PortOrPipe);
            }
            return CallFunctionGetGuid(agentType.GetMethod("LinkPeerToPeerAgent", BindingFlags.Instance | BindingFlags.NonPublic), new object[] { linkParams });
        }

        public bool CD(string dirPath)
        {
            return (bool)agentType.GetMethod("ChangeDirectory", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(CurrentAgent, new object[] { dirPath });
        }

        public Guid CD_RetrieveResults(string dirPath)
        {
            return CallFunctionGetGuid(agentType.GetMethod("ChangeDirectory", BindingFlags.Instance | BindingFlags.NonPublic), new object[] { dirPath });
        }

        public Guid Pwd_RetrieveResults()
        {
            return CallFunctionGetGuid(agentType.GetMethod("PrintCurrentDirectory", BindingFlags.Instance | BindingFlags.NonPublic), new object[] { "" });
        }

        public bool LS(string remoteDir = "")
        {
            if (remoteDir == ".")
            {
                remoteDir = "";
            }
            return (bool)agentType.GetMethod("GetDirectoryCommand", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(CurrentAgent, new object[] { remoteDir });
        }

        public Guid LS_RetrieveResults(string remoteDir = "")
        {
            if (remoteDir == ".")
            {
                remoteDir = "";
            }
            return CallFunctionGetGuid(agentType.GetMethod("GetDirectoryCommand", BindingFlags.Instance | BindingFlags.NonPublic), new object[] { remoteDir });
        }

        public bool MkDir(string dirToMake)
        {
            return (bool)agentType.GetMethod("CreateDirectory", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(CurrentAgent, new object[] { dirToMake });
        }

        public Guid MkDir_RetrieveResults(string dirToMake)
        {
            return CallFunctionGetGuid(agentType.GetMethod("CreateDirectory", BindingFlags.Instance | BindingFlags.NonPublic), new object[] { dirToMake });
        }

        public bool RmDir(string dirToRemove)
        {
            return (bool)agentType.GetMethod("DeleteDirectory", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(CurrentAgent, new object[] { dirToRemove });
        }

        public Guid RmDir_RetrieveResults(string dirToRemove)
        {
            return CallFunctionGetGuid(agentType.GetMethod("DeleteDirectory", BindingFlags.Instance | BindingFlags.NonPublic), new object[] { dirToRemove });
        }

        public bool RM(string fileToRemove)
        {
            return (bool)agentType.GetMethod("DeleteFile", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(CurrentAgent, new object[] { fileToRemove });
        }

        public Guid RM_RetrieveResults(string fileToRemove)
        {
            return CallFunctionGetGuid(agentType.GetMethod("DeleteFile", BindingFlags.Instance | BindingFlags.NonPublic), new object[] { fileToRemove });
        }

        public bool MV(string existingFile, string updatedFile)
        {
            string cmdStr = existingFile + " " + updatedFile;
            return (bool)agentType.GetMethod("RenameFile", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(CurrentAgent, new object[] { cmdStr });
        }

        public Guid MV_RetrieveResults(string existingFile, string updatedFile)
        {
            string cmdStr = existingFile + " " + updatedFile;
            return CallFunctionGetGuid(agentType.GetMethod("RenameFile", BindingFlags.Instance | BindingFlags.NonPublic), new object[] { cmdStr });
        }

        public bool InProcExecuteAssembly(string assemblyLocalPath, string assemblyArgs = "", bool realtimeOutput = false, bool preventExit = false, bool patchAMSI = true, bool patchETW = true, int pid = 0)
        {
            string assemblyFullPath = GetAssemblyPath(assemblyLocalPath);
            string cmdArgs = ParseExecAssemblyArgs(assemblyFullPath, assemblyArgs, realtimeOutput, preventExit, patchAMSI, patchETW, pid);
            return (bool)agentType.GetMethod("InprocExecuteAssembly", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(CurrentAgent, new object[] { cmdArgs });
        }

        public Guid InProcExecuteAssembly_RetrieveResults(string assemblyLocalPath, string assemblyArgs = "", bool realtimeOutput = false, bool preventExit = false, bool patchAMSI = true, bool patchETW = true, int pid = 0)
        {
            string assemblyFullPath = GetAssemblyPath(assemblyLocalPath);
            string cmdArgs = ParseExecAssemblyArgs(assemblyFullPath, assemblyArgs, realtimeOutput, preventExit, patchAMSI, patchETW, pid);
            return CallFunctionGetGuid(agentType.GetMethod("InprocExecuteAssembly", BindingFlags.Instance | BindingFlags.NonPublic), new object[] { cmdArgs });
        }

        private string GetAssemblyPath(string assemblyLocalPath)
        {
            //check if a full path is passed in to the assembly
            if(assemblyLocalPath.IndexOf('\\') > -1)
            {
                if (File.Exists(assemblyLocalPath))
                {
                    return assemblyLocalPath;
                }
                //if we cant find file, grab the name of it and try to search in expected directory locations
                assemblyLocalPath = assemblyLocalPath.Split('\\').Last();
            }
            string fullPath = Directory.GetCurrentDirectory();
            if (File.Exists(fullPath + "\\Plugins\\" + assemblyLocalPath))
            {
                fullPath = fullPath + "\\Plugins\\" + assemblyLocalPath;
            }
            else if (File.Exists(fullPath + "\\Autoruns\\" + assemblyLocalPath))
            {
                fullPath = fullPath + "\\Autoruns\\" + assemblyLocalPath;
            }
            else if (File.Exists(fullPath + "\\" + assemblyLocalPath))
            {
                fullPath = fullPath + "\\" + assemblyLocalPath;
            }
            return fullPath;
        }

        private string ParseExecAssemblyArgs(string assemblyLocalPath, string assemblyArgs, bool realtimeOutput, bool preventExit, bool patchAMSI, bool patchETW, int pid)
        {
            StringBuilder sb = new StringBuilder();

            if (realtimeOutput)
            {
                sb.Append("--realtime-output ");
            }
            if (preventExit)
            {
                sb.Append("--prevent-exit ");
            }
            if (!patchAMSI)
            {
                sb.Append("--no-amsi-patch ");
            }
            if (patchETW)
            {
                sb.Append("--no-etw-patch ");
            }
            if (pid > 0)
            {
                sb.Append(string.Format("--pid={0} ", pid.ToString()));
            }

            sb.Append(assemblyLocalPath);

            if (assemblyArgs != "")
            {
                sb.Append(" " + assemblyArgs);
            }
            return sb.ToString();
        }

        public bool InjectShellcode(string binPath, int targetPid)
        {
            string cmdStr = binPath + " " + targetPid.ToString();
            return (bool)agentType.GetMethod("InjectShellcodeProcess", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(CurrentAgent, new object[] { cmdStr });
        }

        public Guid InjectShellcode_RetrieveResults(string binPath, int targetPid)
        {
            string cmdStr = binPath + " " + targetPid.ToString();
            return CallFunctionGetGuid(agentType.GetMethod("InjectShellcodeProcess", BindingFlags.Instance | BindingFlags.NonPublic), new object[] { cmdStr });
        }

        public bool SpawnShellcode(string binPath, string processToSpawnPath = "", string parentProcessPidOrName = "", string spawnProcCmdLine = "")
        {
            StringBuilder sb = new StringBuilder();

            if(processToSpawnPath != "")
            {
                sb.Append(string.Format("--process-path={0} ", processToSpawnPath));
            }
            if(parentProcessPidOrName != "")
            {
                sb.Append(string.Format("--parent={0} ", parentProcessPidOrName));
            }
            if(spawnProcCmdLine != "")
            {
                sb.Append(string.Format("--cmdline={0} ",spawnProcCmdLine));
            }
            sb.Append(binPath);
            
            return (bool)agentType.GetMethod("SpawnShellcodeProcess", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(CurrentAgent, new object[] { sb.ToString() });
        }

        public Guid SpawnShellcode_RetrieveResults(string binPath, string processToSpawnPath = "", string parentProcessPidOrName = "", string spawnProcCmdLine = "")
        {
            StringBuilder sb = new StringBuilder();

            if (processToSpawnPath != "")
            {
                sb.Append(string.Format("--process-path={0} ", processToSpawnPath));
            }
            if (parentProcessPidOrName != "")
            {
                sb.Append(string.Format("--parent={0} ", parentProcessPidOrName));
            }
            if (spawnProcCmdLine != "")
            {
                sb.Append(string.Format("--cmdline={0} ", spawnProcCmdLine));
            }
            sb.Append(binPath);
            return CallFunctionGetGuid(agentType.GetMethod("SpawnShellcodeProcess", BindingFlags.Instance | BindingFlags.NonPublic), new object[] { sb.ToString() });
        }

        public bool Download(string remoteFilePath, string localFilePath = "")
        {
            if(localFilePath != "")
            {
                remoteFilePath += " " + localFilePath;
            }
            return (bool)agentType.GetMethod("DownloadFile", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(CurrentAgent, new object[] { remoteFilePath });
        }

        public Guid Download_RetrieveResults(string remoteFilePath, string localFilePath = "")
        {
            if (localFilePath != "")
            {
                remoteFilePath += " " + localFilePath;
            }
            return CallFileFunctionGetGuid(agentType.GetMethod("DownloadFile", BindingFlags.Instance | BindingFlags.NonPublic), new object[] { remoteFilePath }, TransferType.download);
        }

        public bool Upload(string localFilePath, string remoteFilePath = "")
        {
            if (remoteFilePath != "")
            {
                localFilePath += " " + remoteFilePath;  
            }
            return (bool)agentType.GetMethod("UploadFile", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(CurrentAgent, new object[] { localFilePath });
        }

        public Guid Upload_RetrieveResults(string localFilePath, string remoteFilePath = "")
        {
            if (remoteFilePath != "")
            {
                localFilePath += " " + remoteFilePath;
            }
            return CallFileFunctionGetGuid(agentType.GetMethod("UploadFile", BindingFlags.Instance | BindingFlags.NonPublic), new object[] { localFilePath }, TransferType.upload);
        }
    }

    public enum TransferType
    {
        download,
        upload
    }

    public enum ImpersonationType
    {
        network,
        interactive
    }

    public enum LinkType
    {
        smb,
        tcp
    }

    public class BofArg
    {
        public DataType argType { get; set; }
        public string strVal { get; set; } = "";

        public BofArg(DataType argType, string strVal)
        {
            this.argType = argType;
            this.strVal = strVal;
        }
    }

    public enum TextColors
    {
        Yellow,
        White,
        Red,
        LightGreen,
        Purple,
        LightSalmon
    }

    public enum DataType
    {
        ascii_string,
        wide_string,
        short_val,
        int_val,
        bytes_as_hexStr   
    }

    public class AgentInstance
    {
        public object CurrentAgent;
        Guid AgentID;
    }

    public class Test
    {
        public string name;
    }

    public class CommandResult
    {
        public bool successful;
        public string data = "";
    }
}
