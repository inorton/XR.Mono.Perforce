using System;
using System.Text.RegularExpressions;

namespace XR.Mono.Perforce
{
    /// <summary>
    /// LHC greg shell utility methods.
    /// </summary>
    /// <remarks>
    /// Borrowed from https://bitbucket.org/LHCGreg/dbsc/src/c3cca47e6b190f7b6fad47c12d781e445e962acc/mydbsc/MySqlDbscEngine.cs?at=master
    /// </remarks>
    public class LHCGreg
    {


        public static string QuoteCommandLineArg(string arg)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                return QuoteCommandLineArgUnix(arg);
            }
            else
            {
                return QuoteCommandLineArgWindows(arg);
            }
        }
        
        internal static string QuoteCommandLineArgWindows(string arg)
        {
            // If a double quotation mark follows two or an even number of backslashes,
            // each proceeding backslash pair is replaced with one backslash and the double quotation mark is removed.
            // If a double quotation mark follows an odd number of backslashes, including just one,
            // each preceding pair is replaced with one backslash and the remaining backslash is removed;
            // however, in this case the double quotation mark is not removed. 
            // - http://msdn.microsoft.com/en-us/library/system.environment.getcommandlineargs.aspx
            //
            // Windows command line processing is funky
            
            string escapedArg;
            Regex backslashSequenceBeforeQuotes = new Regex(@"(\\+)""");
            // Double \ sequences before "s, Replace " with \", double \ sequences at end
            escapedArg = backslashSequenceBeforeQuotes.Replace(arg, (match) => new string('\\', match.Groups[1].Length * 2) + "\"");
            escapedArg = escapedArg.Replace("\"", @"\""");
            Regex backslashSequenceAtEnd = new Regex(@"(\\+)$");
            escapedArg = backslashSequenceAtEnd.Replace(escapedArg, (match) => new string('\\', match.Groups[1].Length * 2));
            // C:\blah\"\\
            // "C:\blah\\\"\\\\"
            escapedArg = "\"" + escapedArg + "\"";
            return escapedArg;
        }
        
        internal static string QuoteCommandLineArgUnix(string arg)
        {
            // Mono uses the GNOME g_shell_parse_argv() function to convert the arg string into an argv
            // Just prepend " and \ with \ and enclose in quotes.
            // Much simpler than Windows!
            
            Regex backslashOrQuote = new Regex(@"\\|""");
            return "\"" + backslashOrQuote.Replace(arg, (match) => @"\" + match.ToString()) + "\"";
        }
    }
}

