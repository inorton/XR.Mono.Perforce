using System;
using System.Linq;

namespace XR.Mono.Perforce
{
    public class Utils
    {
        P4 sess = null;

        public Utils( P4 session )
        {
            if ( session.Workspace == null )
                throw new InvalidOperationException("no workspace set");
            sess = session;
        }

        public bool IsMapped( string localpath )
        {
            if ( System.IO.Directory.Exists( localpath ) ) {
                return false; // p4 doesn't host empty folders
            }

            try {
                var m = sess.RunTagged( "fstat", localpath );
                return ( from t in m where t.Key == "IsMapped" select t.Key ).Count() == 1;
            } catch ( P4Exception ) {
                return false;
            }
        }
    }
}

