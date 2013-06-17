using System;
using MonoDevelop.Core;
using MonoDevelop.VersionControl;

using XR.Mono.Perforce;
using System.Linq;

namespace XR.Monodevelop.Perforce
{
    public class PerforceVersionControl : VersionControlSystem
    {
        #region implemented abstract members of VersionControlSystem

        protected override Repository OnCreateRepositoryInstance()
        {
            throw new NotImplementedException();
        }

        public override IRepositoryEditor CreateRepositoryEditor(Repository repo)
        {
            throw new NotImplementedException();
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

