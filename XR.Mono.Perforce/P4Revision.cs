using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace XR.Mono.Perforce
{
    public class P4Revision
    {
        public string DepotFile { get; set; }
        public string Description { get; set; }
        public int Rev { get; set; }
        public int Change { get; set; }
        public string Action { get; set; }
        public string User { get; set; }
        public DateTime Timestamp { get; set; }

        public static List<P4Revision> FromTags( List<P4ShellTag> tags )
        {
            var rv = new List<P4Revision>();
            var dpf = ( from x in tags where x.Key == "depotFile" select x.Value ).FirstOrDefault();
            if ( string.IsNullOrEmpty(dpf) ) throw new ArgumentException("tags did not contain depotFile");

            P4Revision r = null;

            foreach ( var t in tags ) {
                if ( t.Key.StartsWith("rev") ){
                    r = new P4Revision() {
                        Rev = Int32.Parse( t.Value ),
                        DepotFile = dpf
                    };
                    rv.Add(r);
                    break;
                }
                if ( t.Key.StartsWith("change") ){
                    r.Change = Int32.Parse( t.Value );
                    break;
                }
                if ( t.Key.StartsWith("desc") ){
                    r.Description = t.Value.Trim();
                    break;
                }
                if ( t.Key.StartsWith("user") ){
                    r.User = t.Value;
                    break;
                }
                if ( t.Key.StartsWith("time") ) {
                    var sec = Int32.Parse( t.Value );
                    var dt = new DateTime( 1970, 1, 1, 0, 0, 0 );
                    r.Timestamp = dt.AddSeconds( sec );
                    break;
                }
                if ( t.Key.StartsWith("action") ) {
                    r.Action = t.Value;
                    break;
                }

            }

            return rv;
        }
    }
}

