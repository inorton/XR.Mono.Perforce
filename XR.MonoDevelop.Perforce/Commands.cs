using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using MonoDevelop.VersionControl;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Core;
using System.Collections.Generic;

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
        public IWorkspaceObject GetCurrentProject()
        {
            IWorkspaceObject wob = IdeApp.ProjectOperations.CurrentSelectedSolutionItem;

            if ( wob == null ) {
                wob = IdeApp.ProjectOperations.CurrentSelectedWorkspaceItem;
            }
            return wob;
        }

        public ProjectFolder GetCurrentSelectedFolder()
        {
            return IdeApp.ProjectOperations.CurrentSelectedItem as ProjectFolder;
        }

        public FilePath[] GetCurrentSelectedFiles()
        {
            var rv = new List<FilePath>();
            var curr = IdeApp.ProjectOperations.CurrentSelectedItem;

            var pf = curr as ProjectFile;
            if ( pf != null ) {
                rv.Add( pf.FilePath );
            } else {

                var folder = curr as ProjectFolder;
                if ( folder != null ){
                    var fl = folder.Project.Files.GetFilesInPath( folder.Path );
                    foreach ( var f in fl )
                        rv.Add( f.FilePath );

                } else {
                    throw new NotImplementedException( "select " + curr.GetType().ToString() );
                }
            }

            return rv.ToArray();
        }

        public PerforceRepo GetCurrentRepo()
        {
            var wob = GetCurrentProject();
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
            var wob = GetCurrentProject();
            var repo = GetCurrentRepo();
            if ( repo != null && wob != null){
                using ( var m = VersionControlService.GetProgressMonitor( "Sync", VersionControlOperationType.Pull ) )
                    repo.Update( wob.ItemDirectory.CanonicalPath, true, m );
            }
        }
    }

    public class EditCommandHandler : PerforceCommandHandler
    {
        protected override void Run(object dataItem)
        {
            var wob = GetCurrentProject();
            var repo = GetCurrentRepo();
            
            if ( repo != null && wob != null){
                using ( var m = VersionControlService.GetProgressMonitor( "Edit", VersionControlOperationType.Other ) ) {
                    var files = GetCurrentSelectedFiles();
                    foreach ( var f in files ) {
                        repo.Update( f.CanonicalPath, false, m );
                    }

                    repo.OnEdit( files, false, m );
                }
            }
        }
    }


}

