using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

// This is based on a similar parser from
// http://www.codeproject.com/KB/recipes/command_line.aspx
// 
namespace Utilities.Helpers
{
    /// <summary>
    /// Class to parse command line parameters
    /// </summary>
    public class CommandLineParser
    {
        private const string SPLIT_CHARS = @"^-{1,2}|^/|=|:";
        private const string REMOVED_CHARS = @"^['""]?(.*?)['""]?$";

        private static Regex Spliter = new Regex(SPLIT_CHARS, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static Regex Remover = new Regex(REMOVED_CHARS, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Parses the command line into manageable lookupable pieces of code
        public static StringDictionary Parse(string[] arguments)
        {
            StringDictionary commandLineParams = new StringDictionary();
            string paramName = null;
            string[] paramParts;

            // Valid parameters forms:
            // {-,/,--}param{ ,=,:}((",')value(",'))
            // Examples: -param1 value1 --param2 /param3:"Test-:-work" /param4=happy -param5 '--=nice=--'
            foreach (string arg in arguments)
            {
                // Look for new parameters (-,/ or --) and a possible enclosed value (=,:)
                paramParts = Spliter.Split(arg, 3);
                switch (paramParts.Length)
                {
                    // Found a value (for the last parameter found (space separator))
                    case 1:
                        if (paramName != null)
                        {
                            if (!commandLineParams.ContainsKey(paramName))
                            {
                                paramParts[0] = Remover.Replace(paramParts[0], "$1");
                                commandLineParams.Add(paramName, paramParts[0]);
                            }
                            paramName = null;
                        }
                        // else Error: no parameter waiting for a value (skipped)
                        break;
                    // Found just a parameter
                    case 2:
                        // The last parameter is still waiting. With no value, set it to true.
                        if (paramName != null)
                        {
                            if (!commandLineParams.ContainsKey(paramName)) commandLineParams.Add(paramName, "true");
                        }
                        paramName = paramParts[1];
                        break;
                    // Parameter with enclosed value
                    case 3:
                        // The last parameter is still waiting. With no value, set it to true.
                        if (paramName != null)
                        {
                            if (!commandLineParams.ContainsKey(paramName)) commandLineParams.Add(paramName, "true");
                        }
                        paramName = paramParts[1];
                        // Remove possible enclosing characters (",')
                        if (!commandLineParams.ContainsKey(paramName))
                        {
                            paramParts[2] = Remover.Replace(paramParts[2], "$1");
                            commandLineParams.Add(paramName, paramParts[2]);
                        }
                        paramName = null;
                        break;
                }
            }
            // In case a parameter is still waiting
            if (paramName != null)
            {
                if (!commandLineParams.ContainsKey(paramName))
                {
                    commandLineParams.Add(paramName, "true");
                }
            }
            return commandLineParams;
        }
    }
}