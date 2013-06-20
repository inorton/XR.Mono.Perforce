using System;
using System.IO;
using System.Linq;

using MonoDevelop.Core;
using MonoDevelop.VersionControl;

using XR.Mono.Perforce;
using System.Collections.Generic;

namespace XR.MonoDevelop.Perforce
{
    internal class P4Log {
        static object sync = new object();
        static bool? doLog;
        static string logFile = null;
        public void Emit( string fmt, params object[] args )
        {
            if ( doLog.HasValue && (doLog.Value == false) ) 
                return;

            lock ( sync ){
                if ( !doLog.HasValue ) {
                    if ( logFile == null ){
                        logFile = Environment.GetEnvironmentVariable("XR_MDP4_LOG");
                    }
                    doLog = (logFile != null);
                } 

                if ( doLog.Value ) {
                    System.IO.File.AppendAllText( logFile, string.Format( fmt, args ) + Environment.NewLine );
                }
            }
        }

        public void MonitorStart( IProgressMonitor monitor, int total, string fmt, params string[] args )
        {
            Emit( fmt, args );
            if ( monitor == null ) return;
            monitor.BeginTask( string.Format( fmt, args ), total );
        }

        public void MonitorSuccess( IProgressMonitor monitor, string fmt, params string[] args )
        {
            Emit( fmt, args );
            if ( monitor == null ) return;
            monitor.ReportSuccess( string.Format( fmt, args ) );
        }

        public void MonitorStep( IProgressMonitor monitor, int size )
        {
            if ( monitor == null ) return;
            monitor.Step(size);
        }

        public void MonitorEnd( IProgressMonitor monitor )
        {
            if ( monitor == null ) return;
            monitor.EndTask();
        }
    }

    public class PerforceRepo : UrlBasedRepository
    {
        public const string DefaultConfigFile = ".p4env";

        P4 p4 = null;
        P4Util util = null;
        P4Log Log = new P4Log();
        public PerforceRepo() : base()
        {
            Log.Emit("new PerforceRepo()");
            Url = "p4://";
        }

        public bool ValidRepo { get; set; }

        public PerforceRepo( FilePath someDir ) : this()
        {
            if ( Directory.Exists(someDir) ){
                var cfg = FindP4CONFIGFile( someDir );
                if ( cfg != null ) {
                    if ( File.Exists( cfg ) ){
                        var envs = ReadP4CONFIGFile( cfg );
                        string server = null; // p4 default
                        string user = null;
                        string workspace = null;
                        string password = null;

                        envs.TryGetValue( "P4USER", out user );
                        envs.TryGetValue( "P4PORT", out server );
                        envs.TryGetValue( "P4CLIENT", out workspace );
                        envs.TryGetValue( "P4PASSWD", out password );

                        server = server ?? "perforce:1666";

                        if ( server != null ) {
                            Init( server, workspace, user, password );
                        }
                    }
                }
            }
        }

        static Dictionary<string,string> ReadP4CONFIGFile( string file )
        {
            var rv = new Dictionary<string,string>();
            var lines = File.ReadAllLines( file );
            foreach ( var l in lines )
            {
                var line = l.Trim();
                if ( line.StartsWith("#") ) continue;

                var tmp = line.Split( new char[] { '=' }, 2 );
                if ( tmp.Length == 2 ) {
                    rv[tmp[0]] = tmp[1];
                }
            }

            return rv;
        }

        static string FindP4CONFIGFile( string startpath )
        {
            var env = Environment.GetEnvironmentVariable("P4CONFIG");
            if ( env == null ) env = DefaultConfigFile;
            return FindP4CONFIGFile( startpath, env );
        }

        static string FindP4CONFIGFile( string startpath, string env )
        {
            if ( string.IsNullOrEmpty( startpath ) )
                return null;
            if ( !Directory.Exists(startpath) )
                return null;

            var fp = new FilePath( startpath );
            var tmp = Path.Combine( fp, env );
            if ( File.Exists( tmp ) ){
                return tmp;
            }

            return FindP4CONFIGFile( fp.ParentDirectory, env );
        }

        public PerforceRepo( string p4server, string workspace, string username, string password ) : this()
        {
            Url = "p4://{0}:{1}@{2}/{3}".Fmt( username, password, p4server, workspace );
        }

        string connectedServer = null;
        string connectedUser = null;

        void Init( string p4server, string workspace, string username, string password )
        {
            lock ( Log ){
                ValidRepo = false;

                if ( connectedServer == null || connectedServer != p4server ){
                    p4 = new P4();
                    p4.Connect( p4server );

                    if ( username == null ) {
                        // get the username from p4, bypass login
                        connectedUser = p4.GetCurrentP4Username();
                    }
                }

                if ( password != null ){
                    if ( connectedUser == null || connectedUser != username ){
                        p4.Login( username, password );
                        connectedUser = username;
                    }
                }

                p4.SetWorkspace( workspace ); // set to requested or default

                util = new P4Util( p4 );
                ValidRepo = true;
            }
        }

        public string WorkspaceRoot {
            get {
                if ( p4 != null )
                    return p4.WorkspaceRoot;
                return null;
            }
        }

        public override string Url
        {
            get
            {
                return base.Url;
            }
            set
            {
                Log.Emit("set Url = {0}", value );
                if ( value != null && value == lastValidUrl ) // only bother to connect if the caller validated this address
                {
                    var tmp = MakeUrl( value );
                    if ( tmp != null )
                    {
                        int port = 0;
                        if ( tmp.IsDefaultPort ) { 
                            port = 1666;
                        } else {
                            port = tmp.Port;
                        }

                        if ( tmp.UserInfo != null )
                        {
                            var up = tmp.UserInfo.Split( new char[] {':'}, 2);
                            if ( up.Length == 2 ) {
                                Init( tmp.Host + ":" + port, tmp.AbsolutePath.Trim( '/' ), up[0], up[1] );
                            }
                        }
                    }
                }
                base.Url = value;
            }
        }

        protected override VersionControlOperation GetSupportedOperations(VersionInfo vinfo)
        {
            if ( vinfo.IsDirectory ) {
                return VersionControlOperation.Update;
            }
            VersionControlOperation rv = VersionControlOperation.None;
            if ( File.Exists( vinfo.LocalPath ) )
            {
                if ( !vinfo.IsVersioned ) {
                    rv = VersionControlOperation.Add;
                } else {
                    rv |= VersionControlOperation.Annotate|VersionControlOperation.Log;
                }
                if ( (vinfo.Status & VersionStatus.Modified )== VersionStatus.Modified ) {
                    rv |= VersionControlOperation.Revert|VersionControlOperation.Commit;
                } else {
                    rv |= VersionControlOperation.Update|VersionControlOperation.Remove;
                }
            }
            return rv;
        }

        public override Annotation[] GetAnnotations(FilePath repositoryPath)
        {
            if ( File.Exists(repositoryPath.FullPath) ){
                if ( util.IsMapped(repositoryPath.FullPath) ) {
                    var tags = p4.RunTagged( WorkspaceRoot, "annotate", repositoryPath.FullPath );
                    var rv = new List<Annotation>();

                    foreach ( var t in tags ) {
                        if ( t.Key == "lower" ) {
                            var r = util.GetRevisions( repositoryPath.FullPath + "#" + t.Value, 1).FirstOrDefault();
                            if ( r != null ) {
                                var a = new Annotation( t.Value, r.User, r.Timestamp );
                                rv.Add(a);
                            }
                        }
                    }

                    return rv.ToArray();
                }
            }

            return base.GetAnnotations(repositoryPath);
        }

        #region implemented abstract members of UrlBasedRepository

        public override string[] SupportedProtocols
        {
            get
            {
                return new string[] {"p4"};
            }
        }

        public Uri MakeUrl( string url )
        {
            Log.Emit("perforce {0}", url);
            Uri tmp;
            if ( Uri.TryCreate( url, UriKind.Absolute, out tmp ) ){
                if ( tmp.UserInfo != null && tmp.UserInfo.Contains(":") ) {
                    var path = tmp.AbsolutePath.TrimStart( '/' );
                    Log.Emit("check url path '{0}'", path);
                    if ( !path.Contains("/") ){
                        return tmp;
                    }
                }
            }
            return null;
        }

        string lastValidUrl = null;

        public override bool IsUrlValid(string url)
        {
            var ok = MakeUrl( url ) != null;
            Log.Emit("url {0} valid = {1}", url, ok);
            if (ok) lastValidUrl = url;
            return ok;
        }

        #endregion

        #region implemented abstract members of Repository

        public override string GetBaseText(FilePath localFile)
        {
            return util.GetHeadRevisionText( localFile.FullPath );
        }

        protected override Revision[] OnGetHistory(FilePath localFile, Revision since)
        {
            return GetHistory( localFile, since, 1000 );
        }

        public Revision[] GetHistory(FilePath localFile, Revision since, int count )
        {
            var earliest = since as PerforceRevision;
            var revs = util.GetRevisions( localFile.FullPath, count ).ToArray();
            var rl = new List<Revision>();
            if ( revs != null ){
                if ( earliest != null ) {
                    var tmp = ( from x in revs where x.Rev > earliest.P4Rev select x ).ToArray();
                    revs = tmp;
                }
                foreach ( var r in revs ) 
                {
                    var shortdesc = r.Description;
                    if ( shortdesc.Length > 40 )
                        shortdesc = r.Description.Replace( Environment.NewLine, " " ).Substring(0,40);
                    if ( r.Description.Length > 40 ) shortdesc += "..";
                    var rr = new PerforceRevision( this, r.Timestamp, r.User, shortdesc ) {
                        P4Rev = r.Rev,
                        P4Change = r.Change,
                        P4Action = r.Action,
                        FilePath = localFile,
                    };
                    rl.Add( rr );
                }
            }

            return rl.ToArray();
        }

        protected override IEnumerable<VersionInfo> OnGetVersionInfo(IEnumerable<FilePath> paths, bool getRemoteStatus)
        {
            var rv = new List<VersionInfo>();
            foreach ( var f in paths )
            {
                if ( Directory.Exists(f.FullPath) ) continue;
                Log.Emit("OnGetVersionInfo() {0}", f);
                var stat = util.FStat( f.FullPath );
                // mapped or edited
                if ( stat != null && p4.HasTagValue( stat, "isMapped" ) ) {
                    var haverev = Int32.Parse( p4.FetchTagValue( stat, "haveRev" ) );
                    var headrev = Int32.Parse( p4.FetchTagValue( stat, "headRev" ) );
                    var _have = util.GetRevisions( f.FullPath + "#" + haverev, 1 ).FirstOrDefault();
                    var _head = util.GetRevisions( f.FullPath + "#" + headrev, 1 ).FirstOrDefault();

                    if ( _head != null && _have != null ){
                        var state = VersionStatus.Versioned;
                        var head = new PerforceRevision( this, _head.Timestamp, _head.User, _head.Description ) {
                            P4Action = _head.Action,
                            P4Rev = _head.Rev,
                            P4Change = _head.Change,
                        };
                        var have = new PerforceRevision( this, _have.Timestamp, _have.User, _have.Description ){
                            P4Action = _have.Action,
                            P4Rev = _have.Rev,
                            P4Change = _have.Change,
                        };

                        if ( have.P4Action == "edit" ) 
                            state |= VersionStatus.Modified;

                        rv.Add( new VersionInfo( f, p4.FetchTagValue( stat, "depotFile" ), false, 
                                                state, 
                                                have, 
                                                VersionStatus.Versioned,
                                                head ));
                    }
                }
            }
            return rv;
        }

        protected override VersionInfo[] OnGetDirectoryVersionInfo(FilePath localDirectory, bool getRemoteStatus, bool recursive)
        {
            return new VersionInfo[] { VersionInfo.CreateUnversioned( localDirectory, true ) };
        }

        protected override Repository OnPublish(string serverPath, FilePath localPath, FilePath[] files, string message, IProgressMonitor monitor)
        {
            throw new NotImplementedException();
        }

        protected override void OnUpdate(FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
        {
            Log.Emit("OnUpdate recurse={0}", recurse );
            if ( recurse ) return;
            if ( monitor != null ) monitor.BeginTask("p4 sync", localPaths.Length);

            foreach ( var f in localPaths ){
                util.Sync( f.FullPath, "#head", false, recurse );
                if ( monitor != null ) monitor.Step( 1 );
            }
            if ( monitor != null ) monitor.EndTask();
            if ( monitor != null ) monitor.ReportSuccess( string.Format( "p4 synced {0} files", localPaths.Length));
        }

        protected override void OnCommit(ChangeSet changeSet, IProgressMonitor monitor)
        {
            throw new NotImplementedException();
        }

        protected override void OnCheckout(FilePath targetLocalPath, Revision rev, bool recurse, IProgressMonitor monitor)
        {
            if ( p4 == null ){
                // someone only set the Url.
                var now = Url.ToString();
                var tmp = IsUrlValid( now );
                if ( tmp ) Url = now;
            }

            Log.Emit("OnCheckout target = {0}", targetLocalPath );
            // check that the target folder is a child (or same) as the workspace root
            // lets ignore altroots for now

            if ( targetLocalPath.CanonicalPath == p4.WorkspaceRoot )
                return;

            var dc = System.IO.Path.DirectorySeparatorChar;
            var ttmp = targetLocalPath.CanonicalPath.ToString().TrimEnd( dc );
            var rtmp = p4.WorkspaceRoot.TrimEnd( dc ) + dc.ToString();
            if ( !ttmp.StartsWith( rtmp ) ) {
                throw new InvalidOperationException("target directory must be within the workspace root");
            }

            // since we don't necessarily want to sync the entire world, do nothing about that here.
            
        }

        protected override void OnRevert(FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
        {
            foreach ( var f in localPaths ) 
            {
                if ( util.IsEdited( f.FullPath ) ){
                    util.Revert( f.FullPath, true );
                }
            }
        }

        protected override void OnRevertRevision(FilePath localPath, Revision revision, IProgressMonitor monitor)
        {
            throw new NotImplementedException();
        }

        protected override void OnRevertToRevision(FilePath localPath, Revision revision, IProgressMonitor monitor)
        {
            throw new NotImplementedException();
        }

        public virtual void OnEdit(FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
        {
            Log.MonitorStart( monitor, localPaths.Length, "Edit files" );
            foreach ( var f in localPaths )
            {
                if ( File.Exists(f.FullPath) ) {
                    if ( util.IsMapped( f.FullPath ) ) {
                        util.Edit( f.FullPath );
                        Log.MonitorStep( monitor, 1 );
                    }
                }
            }
            Log.MonitorEnd( monitor );
        }

        protected override void OnAdd(FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
        {
            foreach( var f in localPaths ) {
                if ( !util.IsMapped( f.FullPath ) ){
                    util.Add( f.FullPath );
                }
            }
        }

        protected override string OnGetTextAtRevision(FilePath repositoryPath, Revision revision)
        {
            return util.GetRevisionText( repositoryPath.FullPath, "#" + ((PerforceRevision)revision).P4Rev.ToString() );
        }

        protected override RevisionPath[] OnGetRevisionChanges(Revision revision)
        {
            var rv = new List<RevisionPath>();
            var pr = revision as PerforceRevision;
            if ( pr != null )
            {
                var ch = pr.P4Change;
                if ( ch > 0 ) {
                    // possibly we do p4 describe here?
                }
            }

            return rv.ToArray();
        }

        #endregion


    }
}

