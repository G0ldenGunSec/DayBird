using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHAPI
{
    public static class ArgUtil
    {
        private const string _CALLING_CONVENTION = "/";
        private const char _VALUE_SEPARATOR = ':';

        public static Dictionary<string, string> ParseArgumentsFromString(string argsStr)
        {
            List<string> parsedArgs = new List<string>();
            bool inQuotes = false;
            bool literal = false;
            StringBuilder singleArg = new StringBuilder();

            for (int i = 0; i < argsStr.Length; i++)
            {
                if (argsStr[i] == '"')
                {
                    //if literal, append a quote but don't treat it as part of a quoted string
                    if (literal)
                    {
                        singleArg.Append('"');
                        literal = false;
                    }
                    //flip between opening / closing of quotes here if not a literal
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (argsStr[i] == ' ')
                {
                    if (inQuotes)
                    {
                        singleArg.Append(' ');
                    }
                    //if not in quotes, a space ends the current arg
                    else
                    {
                        parsedArgs.Add(singleArg.ToString());
                        singleArg.Clear();
                    }
                }
                //backslashes can be used for dir paths etc, so need to append unless its being used as a literal (i.e. before a double quote)
                else if (argsStr[i] == '\\')
                {
                    if (i < argsStr.Length - 1 && argsStr[i + 1] == '"')
                    {
                        literal = true;
                    }
                    else
                    {
                        singleArg.Append('\\');
                    }
                }
                //else normal char, append
                else
                {
                    singleArg.Append(argsStr[i]);
                }
            }
            parsedArgs.Add(singleArg.ToString());
            singleArg.Clear();

            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (string arg in parsedArgs)
            {
                if (!arg.StartsWith(_CALLING_CONVENTION))
                {
                    continue;
                }
                string[] parts = arg.Split(new char[] { _VALUE_SEPARATOR }, 2);
                if (parts.Length == 2)
                {
                    result[parts[0].ToLower().Substring(1)] = parts[1];
                }
                else
                {
                    result[parts[0].ToLower().Substring(1)] = "";
                }
            }
            return result;
        }
    }
}
