using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Reflection;
using System.Threading;

namespace DayBird
{
    static class PluginManager
    {
        private static string _lastAutoCompleteLookup = "";
        private static string _lastSearchTerm = "";
        //offset match allows for multiple results that match current filter (i.e. tabbing multiple times with a search filter of 's' would allow for hits of matching names beyond the first)
        private static int _offsetMatch = 0;
        private static List<AssemblyInstance> _loadedAssemblies = new List<AssemblyInstance>();
        private static List<AssemblyInstance> _loadedAutoruns = new List<AssemblyInstance>();

 
        public static void ResetOffset()
        {
            _offsetMatch = 0;
        }

        public static string AutoComplete(string searchTerm)
        {
            //handle instances of multiple tabs to cycle options in a row
            if (searchTerm == _lastAutoCompleteLookup)
            {
                searchTerm = _lastSearchTerm;
                _offsetMatch++;
            }
            //new search term
            else
            {
                _offsetMatch = 0;
                _lastSearchTerm = searchTerm;
            }

           
            //normalize all plugin names to match search term passed to the method
            List<string> plugins = Directory.GetFiles("Plugins").Where(x => x.EndsWith(".dll")).ToList();
            for (int i = 0; i < plugins.Count; i++)
            {
                plugins[i] = plugins[i].Substring(8);
                plugins[i] = plugins[i].Substring(0, plugins[i].IndexOf('.')).ToLower();
            }
            //special cases for non-plugin commands that have been written for the plugin
            plugins.Add("help");
            //

            //find matches for search term from on-disk plugins
            IEnumerable<string> matchingPlugins = plugins.Where(x => x.StartsWith(searchTerm));

            //no matches for searh term
            if (matchingPlugins.Count() == 0)
            {
                return "";
            }

            //loop around for all possible matches of a given search term after user has hit tab multiple times
            if (_offsetMatch >= matchingPlugins.Count())
            {
                _offsetMatch = 0;
            }

            //found a match in plugins
            string retVal = matchingPlugins.ElementAt(_offsetMatch);
            _lastAutoCompleteLookup = retVal;
            return retVal;
        }

        public static void ParseCommand(object activeConsole, Type consoleType, Type historicCommandType, string commandStr)
        {

            commandStr = commandStr.Trim();
            string[] strArray = commandStr.Split(new char[1] { ' ' }, 3);
            string command = null;
            string parameters = null;
            if (strArray.Length > 1)
            {
                command = strArray[1].ToLower();
            }
            if (strArray.Length > 2)
            {
                parameters = strArray[2];
            }

            //return if no command is passed in after 'plugin'
            if (command == null)
            {
                MessageBox.Show("ERROR: Incomplete command, include plugin to load or 'help' / 'list'");
                return;
            }
            NHAPI.Functions consoleOperationsHandle = new NHAPI.Functions(activeConsole);

            //help command, either for plugin or a specific plugin
            if (command == "help")
            {
                //print command out to console + save to console history
                consoleOperationsHandle.PrintToConsole("[" + UI.ConsoleWindow.GetLoggedOnUserAlias() + "]> " + NHAPI.Functions.ColorToTag(UI.Config.ConsoleUI.Layout.HighlightedTextColor) + commandStr, NHAPI.TextColors.LightSalmon,true,false);

                //update command history (for up / down arrow scrolling through commands)

                //get command history list obj (List<HistoricCommand>)
                object commandHistory = consoleType.GetField("_commandHistory", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(activeConsole);
                //get add method for list obj
                MethodInfo addMethod = commandHistory.GetType().GetMethod("Add");
                //create a new HistoricCommand obj to add to the list
                object historicObject = Activator.CreateInstance(historicCommandType, new object[] { });
                //set values for newly created object
                historicCommandType.GetProperty("Command").SetValue(historicObject,commandStr);
                historicCommandType.GetProperty("MessageId").SetValue(historicObject, Guid.Empty);
                //add to the command history list
                addMethod.Invoke(commandHistory, new object[] {historicObject});
                //update current index in list for back/forwards scrolling
                int collectionCount = (int)commandHistory.GetType().GetProperty("Count").GetValue(commandHistory);              
                consoleType.GetField("_commandHistoryIndex", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(activeConsole, collectionCount - 1);

                //general help command
                if (parameters == null)
                {
                    MessageBox.Show("Tab-complete to find your plugin to run. Check README for further info.");
                }

                //load help for a specific plugin
                else
                {
                    AssemblyInstance toQuery = RetrieveAssembly(parameters.Split(' ')[0].ToLower());

                    if (toQuery != null)
                    {
                        ExecPlugin(activeConsole, toQuery.AssemblyObj, "help", null);
                    }
                    else
                    {
                        MessageBox.Show("Error loading assembly to query");
                    }
                }
            }
            //command is attempting to run a plugin
            else
            {
                consoleOperationsHandle.PrintToConsole("[" + UI.ConsoleWindow.GetLoggedOnUserAlias() + "]> " + NHAPI.Functions.ColorToTag(UI.Config.ConsoleUI.Layout.HighlightedTextColor) + commandStr, NHAPI.TextColors.LightSalmon, true, true);

                //update command history (for up / down arrow scrolling through commands)

                //get command history list obj (List<HistoricCommand>)
                object commandHistory = consoleType.GetField("_commandHistory", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(activeConsole);
                //get add method for list obj
                MethodInfo addMethod = commandHistory.GetType().GetMethod("Add");
                //create a new HistoricCommand obj to add to the list
                object historicObject = Activator.CreateInstance(historicCommandType, new object[] { });
                //set values for newly created object
                historicCommandType.GetProperty("Command").SetValue(historicObject, commandStr);
                historicCommandType.GetProperty("MessageId").SetValue(historicObject, Guid.Empty);
                //add to the command history list
                addMethod.Invoke(commandHistory, new object[] { historicObject });
                //update current index in list for back/forwards scrolling
                int collectionCount = (int)commandHistory.GetType().GetProperty("Count").GetValue(commandHistory);
                consoleType.GetField("_commandHistoryIndex", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(activeConsole, collectionCount - 1);

                //start assembly execution functionality here
                //attempt to get assembly from disk
                AssemblyInstance toRun = RetrieveAssembly(command);

                if (toRun != null)
                {
                    Task.Run(() => ExecPlugin(activeConsole, toRun.AssemblyObj, "run", parameters));                    
                }
                else
                {
                    MessageBox.Show("Error loading assembly to run");
                }
            }
        }

        //check if most up-to-date version of assembly has already been loaded, if not loaded or a newer version exists, load it
        private static AssemblyInstance RetrieveAssembly(string pluginName, bool autoRun = false)
        {
            try
            {
                FileInfo pluginInfo = null;
                if (!autoRun && !File.Exists(string.Format(@"Plugins\{0}.dll", pluginName)))
                {
                    MessageBox.Show(string.Format("ERROR: Unable to locate a plugin named '{0}'", pluginName));
                    return null;
                }

                if (autoRun)
                {
                    pluginInfo = new FileInfo(string.Format(@"Autoruns\{0}.dll", pluginName));
                }
                else
                {
                    pluginInfo = new FileInfo(string.Format(@"Plugins\{0}.dll", pluginName));
                }

                AssemblyInstance targetAssembly;

                if (autoRun)
                {
                    targetAssembly = _loadedAutoruns.FirstOrDefault(x => x.AssemblyName == pluginName);
                }
                else
                {
                    targetAssembly = _loadedAssemblies.FirstOrDefault(x => x.AssemblyName == pluginName);
                }

                //no matching loaded assembly, load
                if (targetAssembly == null)
                {
                    targetAssembly = LoadPluginFromDisk(pluginInfo, false, autoRun);
                }
                //matching assembly name has already been loaded
                else
                {
                    //if modified time of plugin on disk is newer than one already loaded, overwrite one in memory
                    if (pluginInfo.LastWriteTime > targetAssembly.LastModified)
                    {
                        targetAssembly = LoadPluginFromDisk(pluginInfo, true, autoRun);
                    }
                }
                return targetAssembly;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private static AssemblyInstance LoadPluginFromDisk(FileInfo pluginInfo, bool overwrite, bool autoRun)
        {
            try
            {
                AssemblyInstance loadedAssembly;

                if (overwrite)
                {
                    if (autoRun)
                    {
                        loadedAssembly = _loadedAutoruns.First(x => x.AssemblyName == pluginInfo.Name.ToLower());
                    }
                    else
                    {
                        loadedAssembly = _loadedAssemblies.First(x => x.AssemblyName == pluginInfo.Name.ToLower());
                    }
                }
                else
                {
                    loadedAssembly = new AssemblyInstance(pluginInfo.Name.ToLower());
                }

                loadedAssembly.AssemblyObj = Assembly.LoadFrom(pluginInfo.FullName);
                loadedAssembly.LastModified = pluginInfo.LastWriteTime;
                loadedAssembly.AssemblySize = pluginInfo.Length;
                if(autoRun)
                {
                    loadedAssembly.RequiredArgs = GetRequiredArgs(loadedAssembly.AssemblyObj).ToDictionary(x => x.ToString());
                }

                if (!overwrite)
                {
                    if (autoRun)
                    {
                        _loadedAutoruns.Add(loadedAssembly);
                    }
                    else
                    {
                        _loadedAssemblies.Add(loadedAssembly);
                    }
                }
                return loadedAssembly;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private static void ExecPlugin(object activeConsole, Assembly toRun, string method, string args)
        {
            try
            {                
                string assemblyNamespace = toRun.FullName.Substring(0, toRun.FullName.IndexOf(','));
                assemblyNamespace.ToString();
                Type pluginType = toRun.GetType(assemblyNamespace + ".Plugin");

                //ideally the plugin author will keep their class name as "Plugin". If this doesn't happen we'll find the first exported type with a "Run" method and use that
                if(pluginType == null)
                {
                    foreach(Type t in toRun.GetExportedTypes())
                    {
                        foreach( MethodInfo m in t.GetMethods())
                        {
                            if(m.Name.StartsWith("Run"))
                            {
                                pluginType = t;
                            }
                        }
                    }
                }
                
                NHAPI.PluginBase plugin = (NHAPI.PluginBase)Activator.CreateInstance(pluginType, new object[] { activeConsole });
                
                // retrieve + display help
                if (method == "help")
                {
                    plugin.Help();
                }
                //run
                else
                {
                    Type consoleType = typeof(UI.ConsoleWindow);
                    Guid agentGuid = (Guid)consoleType.GetField("_clientId", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(activeConsole);
                    Protocol.DetailedMachineInfo uiAgentInfo = Managers.AgentManager.GetAgent(agentGuid).DetailedInfo;
                    NHAPI.DetailedMachineInfo agentInfo = ConvertDetailedInfo(uiAgentInfo);

                    if(agentInfo == null)
                    {
                        MessageBox.Show("ERROR: unable to retrieve agent info for connection, aborting plugin run. If this is a new agent connection wait for detailed info to populate before running a plugin");
                        return;
                    }

                    plugin.Run(args, agentInfo);
                }
            }
            catch(Exception e)
            {
                MessageBox.Show("Error executing plugin: " + e.ToString());            
            }            
        }

        private static NHAPI.DetailedMachineInfo ConvertDetailedInfo(Protocol.DetailedMachineInfo uiInfo)
        {
            try
            {
                NHAPI.DetailedMachineInfo detailedInfo = new NHAPI.DetailedMachineInfo();
                detailedInfo.WindowsVersion = uiInfo.WindowsVersion;
                detailedInfo.MachineName = uiInfo.MachineName;
                detailedInfo.UserName = uiInfo.UserName;
                detailedInfo.ProcessName = uiInfo.ProcessName;
                detailedInfo.PID = uiInfo.PID;
                detailedInfo.Arch = (NHAPI.EDetailedInfoProcessType)uiInfo.Arch;
                detailedInfo.IPAddresses = uiInfo.IPAddresses;
                detailedInfo.IntegrityLevel = (NHAPI.EIntegrityLevel)uiInfo.IntegrityLevel;
                detailedInfo.Tunnelled = uiInfo.Tunnelled;

                return detailedInfo;
            }
            catch
            {
                return null;
            }
        }

        public static List<AssemblyInstance> LoadAutoRuns()
        {
            //if autoruns have already been loaded, return the already-populated collection
            if(_loadedAutoruns.Count > 0)
            {
                return _loadedAutoruns;
            }

            List<string> AutoRunPlugins = Directory.GetFiles("Autoruns").Where(x => x.EndsWith(".dll")).ToList();
            for (int i = 0; i < AutoRunPlugins.Count; i++)
            {
                //remove "autoruns\" from front of filename
                AutoRunPlugins[i] = AutoRunPlugins[i].Substring(9);
                //remove .dll from end of filename
                AutoRunPlugins[i] = AutoRunPlugins[i].Substring(0, AutoRunPlugins[i].IndexOf('.')).ToLower();
                try
                {
                    RetrieveAssembly(AutoRunPlugins[i], true);
                }
                catch
                {
                    MessageBox.Show("Error loading autorun plugin: " + AutoRunPlugins[i]);
                }
            }
            return _loadedAutoruns;
        }

        private static string[] GetRequiredArgs(Assembly toRun)
        {
            try
            {
                foreach (Type t in toRun.GetTypes())
                {
                    Console.WriteLine(t.Name);
                }
                string assemblyNamespace = toRun.FullName.Substring(0, toRun.FullName.IndexOf(','));
                assemblyNamespace.ToString();
                Type pluginType = toRun.GetType(assemblyNamespace + ".Plugin");

                //ideally the plugin author will keep their class name as "Plugin". In case this isn't the case we'll just grab the first exported type and hope for the best
                if (pluginType == null)
                {
                    pluginType = toRun.GetExportedTypes()[0];
                }

                NHAPI.PluginBase plugin = (NHAPI.PluginBase)Activator.CreateInstance(pluginType, new object[] { null });

                return plugin.RequiredArgs;

            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static void ExecAutoRuns(object activeConsole, Type consoleType, Type historicCommandType)
        {
            //create a list of all enabled autoruns, sorted by priority
            List<AssemblyInstance> enabledAutoRuns = _loadedAutoruns.Where(x => x.Priority > -1).OrderBy(y => y.Priority).ToList();

            NHAPI.Functions consoleOperationsHandle = new NHAPI.Functions(activeConsole);
            consoleOperationsHandle.PrintToConsole("[" + UI.ConsoleWindow.GetLoggedOnUserAlias() + "]> " + NHAPI.Functions.ColorToTag(UI.Config.ConsoleUI.Layout.HighlightedTextColor) + "***Executing AutoRun Plugins***", NHAPI.TextColors.LightSalmon, true, false);

            //update command history (for up / down arrow scrolling through commands)

            //get command history list obj (List<HistoricCommand>)
            object commandHistory = consoleType.GetField("_commandHistory", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(activeConsole);
            //get add method for list obj
            MethodInfo addMethod = commandHistory.GetType().GetMethod("Add");
            //create a new HistoricCommand obj to add to the list
            object historicObject = Activator.CreateInstance(historicCommandType, new object[] { });
            //set values for newly created object
            historicCommandType.GetProperty("Command").SetValue(historicObject, "");
            historicCommandType.GetProperty("MessageId").SetValue(historicObject, Guid.Empty);
            //add to the command history list
            addMethod.Invoke(commandHistory, new object[] { historicObject });
            //update current index in list for back/forwards scrolling
            int collectionCount = (int)commandHistory.GetType().GetProperty("Count").GetValue(commandHistory);
            consoleType.GetField("_commandHistoryIndex", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(activeConsole, collectionCount - 1);


            foreach (AssemblyInstance enabledAutoRun in enabledAutoRuns) 
            {
                StringBuilder argStr = new StringBuilder();
                //create args string based on any required args and our known command structure
                foreach(KeyValuePair<string,string> requiredArg in enabledAutoRun.RequiredArgs)
                {
                    argStr.Append(string.Format("/{0}:{1} ", requiredArg.Key, requiredArg.Value));
                }
                //if args have been added, remove trailing space behind final arg
                if(argStr.Length > 0)
                {
                    argStr.Length--;
                }
                //execute plugins in current thread to avoid stacking all autoruns on top of eachother
                ExecPlugin(activeConsole, enabledAutoRun.AssemblyObj, "run", argStr.ToString());                
            }
        }
    }    
}
