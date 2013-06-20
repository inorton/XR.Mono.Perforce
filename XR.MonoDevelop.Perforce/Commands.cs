using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using MonoDevelop.VersionControl;

namespace XR.MonoDevelop.Perforce
{
    public enum Commands
    {
        Edit,
        Delete,
        Sync,
        Submit,
        Revert,
        RevertUnchanged,
    }

    public class PerforceCommandHandler : CommandHandler
    {
        public IWorkspaceObject GetCurrentSelectedObject()
        {
            IWorkspaceObject wob = IdeApp.ProjectOperations.CurrentSelectedSolutionItem;
            if ( wob == null )
                wob = IdeApp.ProjectOperations.CurrentSelectedWorkspaceItem;
            return wob;
        }

        public PerforceRepo GetCurrentRepo()
        {
            var wob = GetCurrentSelectedObject();
            if ( wob != null )
                return VersionControlService.GetRepository(wob) as PerforceRepo;
            return null;
        }

        protected override void Update(CommandInfo info)
        {
            info.Visible = ( GetCurrentRepo() != null );
        }
    }

    public class SyncCommandHandler : PerforceCommandHandler 
    {
        protected override void Run()
        {
            var wob = GetCurrentSelectedObject();
            var repo = GetCurrentRepo();

            if ( repo != null && wob != null){
                repo.Update( wob.ItemDirectory.CanonicalPath, true, null );
            }
        }
    }

    public class EditCommandHandler : PerforceCommandHandler
    {
        protected override void Run(object dataItem)
        {
            var wob = GetCurrentSelectedObject();
            var repo = GetCurrentRepo();
            
            if ( repo != null && wob != null){

                repo.Update( wob.ItemDirectory.CanonicalPath, true, null );
            }
        }
    }

    public class SubmitCommandHandler : PerforceCommandHandler 
    {
        protected override void Run()
        {
            var wob = GetCurrentSelectedObject();
            var repo = GetCurrentRepo();
            
            throw new NotImplementedException("Commit");
        }
    }
}

