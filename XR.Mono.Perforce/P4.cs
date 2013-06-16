using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace XR.Mono.Perforce
{

    public class P4
    {
        public P4()
        {
        }

        public string this[string k] {
            get {
                return Envs[k] as string;
            }
            set {
                Envs[k] = value;
            }
        }

        public List<P4ShellTag> Connect( string server )
        {
            Envs["P4PORT"] = server;
            return RunTagged( Environment.CurrentDirectory, "info" );
        }

        public string Login( string username, string password )
        {
            try { RunTagged( Environment.CurrentDirectory, "logout" ); } catch { }
            Envs["P4USER"] = username.Trim();
            Envs["P4PASSWD"] = password.Trim();

            var tmp = RunTagged( Environment.CurrentDirectory, "user", "-o" );
            return (from x in tmp where x.Key == "FullName" select x.Value).FirstOrDefault();
        }

        static string QuoteAgruments( params string[] args )
        {
            var sb = new StringBuilder();
            foreach ( var s in args )
                sb.AppendFormat("{0} ", LHCGreg.QuoteCommandLineArg( s ) );

            return sb.ToString();
        }

        StringDictionary environment = new StringDictionary();
        StringDictionary Envs {
            get {
                return environment;
            }
        }

        public Process Run( string workdir, params string[] args )
        {
            var psi = new ProcessStartInfo( "p4", QuoteAgruments( args ) );
            psi.WorkingDirectory = workdir;
            psi.UseShellExecute = false;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardInput = true;
            psi.EnvironmentVariables.Clear();
            lock ( Envs ){
                psi.EnvironmentVariables["PATH"] = Environment.GetEnvironmentVariable("PATH");
                psi.EnvironmentVariables["HOME"] = Environment.GetEnvironmentVariable("HOME");

                foreach ( string k in Envs.Keys )
                    psi.EnvironmentVariables[k.ToUpper()] = (string)Envs[k];
            }
            var p = Process.Start( psi );
            p.WaitForExit();
            return p;
        }

        public List<P4ShellTag> RunTagged( string workdir, params string[] args )
        {
            var rv = new List<P4ShellTag>();
            var argv = new List<string>();
            Process p = null;
            try {
                argv.Add("-ztag");
                argv.AddRange(args);
                p = Run( workdir, argv.ToArray() );

                // p4 -ztag output is a bit odd

                // ...TAG{,N}[0,1] VALUE$

                // ...TAG$
                // VALUELINE1
                // VALUELINE2

                var matchTagName = new Regex("^\\.\\.\\. ([A-Za-z0-9,]+)");
                P4ShellTag st = null;
                string line;
                do {
                    line = p.StandardOutput.ReadLine();
                    if ( line == null ) break;
                    if ( line == string.Empty ){
                        // continue last value
                    } else {
                        if ( line.StartsWith("... ") ) {
                            var mc = matchTagName.Matches( line );
                            foreach ( Match m in mc ) {
                                if ( m.Groups.Count == 2 ) {
                                    var tn = m.Groups[1].Value;;
                                    st = new P4ShellTag();
                                    st.Key = tn;
                                    // this might be a multi-line or might have a value
                                    var tmp = line.Split( new char[] { ' ' }, 3 );
                                    if ( tmp.Length == 3 ) {
                                        st.Value = tmp[2];
                                    }
                                    rv.Add(st);
                                }
                            }
                        } else {
                            if ( st != null ) {
                                st.Value = line;
                            }
                        }
                    }
                } while ( line != null );


            } finally {
                if ( p == null || p.ExitCode != 0 ){
                    throw new P4Exception( p.StandardError.ReadToEnd() );
                }
            }

            return rv;
        }
    }

}

