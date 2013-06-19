using System;
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
                    System.IO.File.AppendAllText( logFile, string.Format( fmt, args ) );
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
        P4 p4 = null;
        P4Util util = null;
        P4Log Log = new P4Log();
        public PerforceRepo() 
        {
            Url = "p4://";
        }

        public PerforceRepo( string p4server, string workspace, string username, string password )
        {
            p4 = new P4();
            p4.Connect( p4server );
            p4.Login( username, password );
            p4.SetWorkspace( workspace );
            util = new P4Util( p4 );
            Url = "p4://{0}@{1}/{2}".Fmt( username, p4server, workspace );
        }

        #region implemented abstract members of UrlBasedRepository

        public override string[] SupportedProtocols
        {
            get
            {
                return new string[] {"p4"};
            }
        }

        public override bool IsUrlValid(string url)
        {
            Uri tmp;
            if ( Uri.TryCreate( url, UriKind.Absolute, out tmp ) ){
                if ( tmp.UserInfo != null && !tmp.UserInfo.Contains(":") ) {
                    if ( !tmp.AbsolutePath.Contains("/") ){
                        return true;
                    }
                }
            }
            return false;
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
                    var shortdesc = r.Description.Replace( Environment.NewLine, " " ).Substring(0,40);
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
            throw new NotImplementedException();
        }

        protected override VersionInfo[] OnGetDirectoryVersionInfo(FilePath localDirectory, bool getRemoteStatus, bool recursive)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        protected override void OnRevert(FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
        {
            throw new NotImplementedException();
        }

        protected override void OnRevertRevision(FilePath localPath, Revision revision, IProgressMonitor monitor)
        {
            throw new NotImplementedException();
        }

        protected override void OnRevertToRevision(FilePath localPath, Revision revision, IProgressMonitor monitor)
        {
            throw new NotImplementedException();
        }

        protected override void OnAdd(FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
        {
            throw new NotImplementedException();
        }

        protected override string OnGetTextAtRevision(FilePath repositoryPath, Revision revision)
        {
            return util.GetRevisionText( repositoryPath.FullPath, "#" + ((PerforceRevision)revision).P4Rev.ToString() );
        }

        protected override RevisionPath[] OnGetRevisionChanges(Revision revision)
        {
            throw new NotImplementedException();
        }

        #endregion


    }
}

