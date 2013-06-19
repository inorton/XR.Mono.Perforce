using System;
using MonoDevelop.Core;
using MonoDevelop.VersionControl;

using XR.Mono.Perforce;
using System.Linq;

namespace XR.MonoDevelop.Perforce
{
    public class PerforceRevision : Revision
    {
        public PerforceRevision( PerforceRepo repo, DateTime time, string author, string msg )
            : base( repo, time, author, msg )
        {
        }

        public PerforceRevision( PerforceRepo repo ) 
            : base ( repo )
        {
        }

        internal PerforceRepo P4Repo {
            get {
                return Repository as PerforceRepo;
            }
        }

        public FilePath FilePath { get; set; }

        public int P4Rev { get; set; }

        public int P4Change { get; set; }

        public string P4Action { get ;set;}

        #region implemented abstract members of Revision

        public override Revision GetPrevious()
        {
            if ( P4Rev > 1 )
                return P4Repo.GetHistory( FilePath.FullPath, new PerforceRevision( P4Repo ) { P4Rev = P4Rev - 1 }, 1 ).FirstOrDefault();
            return null;
        }

        #endregion

    }
}

