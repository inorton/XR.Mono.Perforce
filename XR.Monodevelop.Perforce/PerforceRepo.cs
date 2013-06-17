using System;
using System.Linq;

using MonoDevelop.Core;
using MonoDevelop.VersionControl;

using XR.Mono.Perforce;
using System.Collections.Generic;

namespace XR.Monodevelop.Perforce
{
    public class PerforceRepo : Repository
    {
        P4 p4 = null;
        P4Util util = null;

        public PerforceRepo( string p4server, string workspace, string username, string password )
        {
            p4 = new P4();
            p4.Connect( p4server );
            p4.Login( username, password );
            p4.SetWorkspace( workspace );
            util = new P4Util( p4 );
        }

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

        protected override System.Collections.Generic.IEnumerable<VersionInfo> OnGetVersionInfo(System.Collections.Generic.IEnumerable<FilePath> paths, bool getRemoteStatus)
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
            throw new NotImplementedException();
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

