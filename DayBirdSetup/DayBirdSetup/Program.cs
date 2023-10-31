using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DayBirdSetup
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //initial checks to ensure all directories + files we need exist
            if(args.Length < 1 || !Directory.Exists(args[0]))
            {
                Console.WriteLine("[!] Please include path to the folder containing UI.exe as the only arg");
                return;
            }
            
            if(!File.Exists("DayBird.dll") || !File.Exists("NHAPI.dll"))
            {
                Console.WriteLine("[!] Please ensure DayBird.dll and NHAPI.dll exist in the directory you're attempting to run this from");
                return;
            }

            if (args[0].Last() != '\\')
            {
                args[0] += "\\";
            }

            if (!File.Exists(args[0] + "UI.exe"))
            {
                Console.WriteLine("[!] Please ensure UI.exe exists in the target directory passed in");
                return;
            }

            //copy files to target directory

            Console.WriteLine("[*] Writing files into NH client directory");
            try
            {
                File.Copy("DayBird.dll", args[0] + "DayBird.dll");
                File.Copy("NHAPI.dll", args[0] + "NHAPI.dll");
            }
            catch
            {
                Console.WriteLine("[X] Error copying files into NH client directory - aborting");
                return;
            }

            Console.WriteLine("[*] Creating scripts and autoruns directories");
            try
            {
                Directory.CreateDirectory(args[0] + "Plugins");
                Directory.CreateDirectory(args[0] + "Autoruns");
            }
            catch
            {
                Console.WriteLine("[X] Error creating directories - aborting");
                return;
            }

            Console.WriteLine("[*] Populating UI.exe.config file");
            try
            {
                string[] fileNames = Directory.GetFiles(args[0]);

                if(fileNames.Contains("UI.exe.config"))
                {
                    File.Move(args[0] + "UI.exe.config", args[0] + "ORIGINAL_UI.exe.config");
                }

                string noTrail = args[0].Remove(args[0].Length - 1,1);
                string configFileCode = string.Format("<configuration>\r\n   <runtime>\r\n      <assemblyBinding xmlns=\"urn:schemas-microsoft-com:asm.v1\">\r\n         <probing privatePath=\"{0}\"/>\r\n      </assemblyBinding> \r\n\t  <appDomainManagerAssembly value=\"DayBird, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\" />  \r\n\t  <appDomainManagerType value=\"MyAppDomainManager\" />  \r\n   </runtime>\r\n</configuration>", noTrail);

                File.WriteAllText(args[0] + "UI.exe.config", configFileCode);
            }
            catch
            {
                Console.WriteLine("[X] Error creating UI.exe.config file - aborting");

            }
            Console.WriteLine("[+] Successfully performed all setup - exiting");
        }
    }
}
