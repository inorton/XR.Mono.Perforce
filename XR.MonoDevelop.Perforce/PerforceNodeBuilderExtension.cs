using System;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.VersionControl;

namespace XR.MonoDevelop.Perforce
{
    public class PerforceNodeBuilderExtension : NodeBuilderExtension
    {

        Dictionary<string,IWorkspaceObject> repos = new Dictionary<string, IWorkspaceObject> ();
                
                
        public override bool CanBuildNode (Type dataType)
        {
            return typeof(IWorkspaceObject).IsAssignableFrom (dataType);
        }

        public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
        {
            IWorkspaceObject ob = (IWorkspaceObject) dataObject;
            PerforceRepo rep = VersionControlService.GetRepository (ob) as PerforceRepo;
            if (rep != null) {
                IWorkspaceObject rob;
                if (repos.TryGetValue (rep.WorkspaceRoot, out rob)) {
                    if (ob == rob)
                        label += " (perforce)";
                }
            }
        }
                
        public override void OnNodeAdded (object dataObject)
        {
            IWorkspaceObject ob = (IWorkspaceObject) dataObject;
            PerforceRepo rep = VersionControlService.GetRepository (ob) as PerforceRepo;
            if (rep != null && !repos.ContainsKey (rep.WorkspaceRoot)) {
                repos [rep.WorkspaceRoot] = ob;
            }
        }

        public override void OnNodeRemoved (object dataObject)
        {
            IWorkspaceObject ob = (IWorkspaceObject) dataObject;
            PerforceRepo rep = VersionControlService.GetRepository (ob) as PerforceRepo;
            IWorkspaceObject rob;
            if (rep != null && repos.TryGetValue (rep.WorkspaceRoot, out rob)) {
                if (ob == rob)
                    repos.Remove (rep.WorkspaceRoot);
            }
        }

        void HandleApplicationFocusIn (object sender, EventArgs e)
        {
            foreach (object ob in repos.Values) {
                ITreeBuilder tb = Context.GetTreeBuilder (ob);
                if (tb != null)
                    tb.Update ();
            }
        }

        void HandleBranchSelectionChanged (object sender, EventArgs e)
        {
            HandleApplicationFocusIn (null, null);
        }
    }
}
