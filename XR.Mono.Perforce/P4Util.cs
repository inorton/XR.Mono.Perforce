using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace XR.Mono.Perforce
{
    public class P4Util
    {
        P4 sess = null;

        public P4Util( P4 session )
        {
            if ( session.Workspace == null )
                throw new InvalidOperationException("no workspace set");
            sess = session;
        }

        public static string PathJoin( params string[] parts )
        {
            return string.Join( Path.DirectorySeparatorChar.ToString(), parts );
        }

        public bool IsMapped( string localpath )
        {
            return IsMapped( localpath, new List<P4ShellTag>() );
        }

        bool IsMapped( string localpath, List<P4ShellTag> fstat )
        {
            if ( Directory.Exists( localpath ) ) {
                return false; // p4 doesn't host empty folders
            }

            try {
                var m = sess.RunTagged( sess.WorkspaceRoot, "fstat", localpath );
                fstat.Clear();
                fstat.AddRange( m );
                return ( from t in m where t.Key == "isMapped" select t.Key ).Count() == 1;
            } catch ( P4Exception ) {
                return false;
            }
        }

        public List<P4ShellTag> FStat( string localpath )
        {
            var rv = new List<P4ShellTag>();
            IsMapped( localpath, rv );
            return rv;
        }

        public string[] Dirs( string localpatt )
        {
            var rv = sess.RunTagged( sess.WorkspaceRoot, "dirs", localpatt );
            return ( from x in rv where x.Key == "dir" select x.Value ).ToArray();
        }

        public string DirWhere( string localpath )
        {
            return Dirs( localpath ).FirstOrDefault();
        }

        public bool IsPendingDelete( string localpath )
        {
            var fstat = new List<P4ShellTag>();
            if ( IsMapped( localpath, fstat ) ){
                var act = ( from x in fstat where x.Key == "action" select x.Value ).FirstOrDefault();
                return act == "delete";
            }
            return false;
        }

        public bool IsEdited( string localpath )
        {
            var fstat = new List<P4ShellTag>();
            if ( IsMapped( localpath, fstat ) ){
                var act = ( from x in fstat where x.Key == "action" select x.Value ).FirstOrDefault();
                return act == "edit";
            }
            return false;
        }

        public void Edit( string localpath ) {
            sess.RunTagged( sess.WorkspaceRoot, "edit", localpath );
        }

        public void Add( string localpath ) {
            sess.RunTagged( sess.WorkspaceRoot, "add", localpath );
        }

        public void Revert( string localpath, bool discardChanges ) 
        {
            if ( discardChanges ) {
                sess.RunTagged( sess.WorkspaceRoot, "revert", localpath );
            } else  {
                sess.RunTagged( sess.WorkspaceRoot, "revert", "-a", localpath );
            }
        }

        public void Delete( string localpath )
        {
            sess.RunTagged( sess.WorkspaceRoot, "delete", localpath );
        }

        public void Move( string currentlocalpath, string newlocalpath )
        {
            if ( IsMapped( currentlocalpath ) )
            {
                Revert( currentlocalpath, false );
                if ( IsEdited( currentlocalpath ) ){
                    throw new P4Exception("you cannot rename/move this file as it is marked for edit");
                }
                sess.RunTagged( sess.WorkspaceRoot, "integrate", currentlocalpath, newlocalpath );
                Delete( currentlocalpath );
            }
        }

        public void Sync( string localpath, string rev, bool force, bool recurse ) 
        {
            var args = new List<string>() { "sync" };
            if ( force ) 
                args.Add("-f");

            if ( recurse ) 
            {
                localpath = localpath.TrimEnd( Path.DirectorySeparatorChar );
                localpath += "...";
            }

            localpath += rev;
            args.Add( localpath );

            sess.RunTagged( sess.WorkspaceRoot, args.ToArray() );
        }

        public string GetRevisionText( string localpath, string rev )
        {
            if ( string.IsNullOrEmpty(rev) )
                throw new ArgumentNullException("rev");
            if ( !rev.StartsWith("#") )
                throw new ArgumentException("rev must start with a #");

            return sess.RunStdoutText( sess.WorkspaceRoot, "print", "-q", localpath + rev );
        }

        public string GetHeadRevisionText( string localpath ) 
        {
            return GetRevisionText( localpath, "#head" );
        }

        public List<P4Revision> GetRevisions( string localpath )
        {
            return GetRevisions( localpath, 1000 );
        }

        public List<P4Revision> GetRevisions( string localpath, int max )
        {
            if ( IsMapped( localpath ) ) {
                var tags = sess.RunTagged( sess.WorkspaceRoot, "filelog", "-m", max.ToString(), "-l", localpath + "#head" );
                return P4Revision.FromTags( tags );
            }
            return null;
        }
    }
}

