using System;
using MonoDevelop.Core;
using MonoDevelop.VersionControl;

using XR.Mono.Perforce;
using System.Linq;

namespace XR.MonoDevelop.Perforce
{
    public class PerforceVersionControl : VersionControlSystem
    {
        #region implemented abstract members of VersionControlSystem

        protected override Repository OnCreateRepositoryInstance()
        {
            return new PerforceRepo();
        }

        public override IRepositoryEditor CreateRepositoryEditor(Repository repo)
        {
            return new UrlBasedRepositoryEditor( repo as PerforceRepo );
        }

        public override string Name
        {
            get
            {
                return "Perforce";
            }
        }

        public override bool IsInstalled
        {
            get
            {
                return true;
            }
        }

        #endregion


    }
}

