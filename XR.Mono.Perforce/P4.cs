using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.IO;

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
            if ( string.IsNullOrEmpty(server) )
                server = Environment.GetEnvironmentVariable("P4PORT");
            Envs["P4PORT"] = server;
            return SessionInfo();
        }

        public List<P4ShellTag> SessionInfo()
        {
            return RunTagged( Environment.CurrentDirectory, "info" );
        }

        public string GetCurrentP4Username()
        {
            var si = SessionInfo();
            return FetchTagValue( si, "userName" );
        }

        public string FetchTagValue( List<P4ShellTag> tags, string key )
        {
            return ( from x in tags where x.Key == key select x.Value ).FirstOrDefault();
        }

        public string Login( string username, string password )
        {
            if ( string.IsNullOrEmpty(username) )
                username = Environment.GetEnvironmentVariable("P4USER");
            if ( string.IsNullOrEmpty(password) )
                password = Environment.GetEnvironmentVariable("P4PASSWD");

            if ( !string.IsNullOrEmpty(username) )
                Envs["P4USER"] = username.Trim();

            if ( !string.IsNullOrEmpty(password) )
                Envs["P4PASSWD"] = password.Trim();

            var tmp = RunTagged( Environment.CurrentDirectory, "user", "-o" );
            return (from x in tmp where x.Key == "FullName" select x.Value).FirstOrDefault();
        }

        public void CreateWorkspace( string clientname, string rootdir )
        {
            var wsd = RunStdout(rootdir, "client", "-o", clientname );

            RunInput( rootdir, wsd, "client", "-i" );
            SetWorkspace( clientname );
        }

        public void DeleteWorkspace( string clientname )
        {
            RunTagged( Environment.CurrentDirectory, "client", "-d", clientname );
        }

        public void SetWorkspace( string clientname ) 
        {
            if ( clientname == null )
                clientname = Environment.GetEnvironmentVariable("P4CLIENT");

            if ( clientname == null )
                clientname = Environment.MachineName;

            Envs["P4CLIENT"] = clientname;
            var wsi = RunTagged( Environment.CurrentDirectory, "info" );
            var cr = from x in wsi where x.Key == "clientRoot" select x.Value;
            WorkspaceRoot = cr.FirstOrDefault();
        }

        public string Workspace {
            get {
                if ( Envs.ContainsKey("P4CLIENT") )
                    return Envs["P4CLIENT"];
                return null;
            }
        }

        public string WorkspaceRoot {
            get; private set;
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

        Process Run( string workdir, params string[] args )
        {
            var p = RunStart( workdir, null, args );
            p.WaitForExit();
            return p;
        }

        public void RunInput( string workdir, byte[] input, params string[] args )
        {
            var p = RunStart( workdir, input, args );
            p.WaitForExit();
            if ( p == null || p.ExitCode != 0 ){
                throw new P4Exception( string.Format("cmd: {0}\nerror:{1}",
                                                     string.Join(" ", args ), 
                                                     p.StandardError.ReadToEnd() ) );
            }
        }

        Process RunStart( string workdir, byte[] input, params string[] args )
        {
            var psi = new ProcessStartInfo( "p4", QuoteAgruments( args ) );
            psi.WorkingDirectory = workdir;
            psi.UseShellExecute = false;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardInput = input != null;
            psi.EnvironmentVariables.Clear();
            lock ( Envs ){
                psi.EnvironmentVariables["PATH"] = Environment.GetEnvironmentVariable("PATH");
                psi.EnvironmentVariables["HOME"] = Environment.GetEnvironmentVariable("HOME");

                foreach ( string k in Envs.Keys )
                    psi.EnvironmentVariables[k.ToUpper()] = (string)Envs[k];
            }
            var p = Process.Start( psi );

            if ( input != null ) {
                System.Threading.Thread.Sleep(1000);
                p.StandardInput.BaseStream.Write( input, 0, input.Length );
                p.StandardInput.Flush();
                System.Threading.Thread.Sleep(1000);
                p.StandardInput.Close();
            }

            return p;
        }

        public void RunTaggedInput( string workdir, List<P4ShellTag> tags, params string[] args )
        {
            throw new NotImplementedException("TODO: tagged input");
        }

        public byte[] RunStdout( string workdir, params string[] args )
        {
            using (var ms = new MemoryStream() ){
                var p = Run( workdir, args );
                if ( p == null || p.ExitCode != 0 ){
                    throw new P4Exception( p.StandardError.ReadToEnd() );
                }
                byte[] buf = new byte[1024];
                int count = 0;
                do {
                    count = p.StandardOutput.BaseStream.Read( buf, 0, buf.Length );
                    if ( count < 1 ) break;
                    ms.Write( buf, 0, count );
                } while ( true );
                return ms.ToArray();
            }
        }

        public string RunStdoutText( string workdir, params string[] args )
        {
            var p = Run( workdir, args );
            if ( p == null || p.ExitCode != 0 ){
                throw new P4Exception( p.StandardError.ReadToEnd() );
            }
            return p.StandardOutput.ReadToEnd();
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

                // repeating groups of items start with an index of 1 (not zero)

                var matchTagName = new Regex("^\\.\\.\\. ([A-Za-z0-9,]+)");
                P4ShellTag st = null;
                int index = 0;
                string line;
                do {
                    line = p.StandardOutput.ReadLine();
                    if ( line == null ) break;

                    if ( line.StartsWith("... ") ) {
                        var mc = matchTagName.Matches( line );
                        foreach ( Match m in mc ) {
                            if ( m.Groups.Count == 2 ) {
                                var tn = m.Groups[1].Value;

                                if ( tn == "depotFile" || tn == "dir" || tn == "upper" )
                                    index++;

                                st = new P4ShellTag();
                                st.Index = index;
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
                            st.Value += line + Environment.NewLine;
                        }
                    }

                } while ( line != null );


            } finally {
                if ( p == null || p.ExitCode != 0 ){
                    throw new P4Exception( string.Format("cmd: {0}\nerror:{1}",
                                                         string.Join(" ", args ), 
                                                         p.StandardError.ReadToEnd() ) );
                }
            }

            return rv;
        }
    }

}

