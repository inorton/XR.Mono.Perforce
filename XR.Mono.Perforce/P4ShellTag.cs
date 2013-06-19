using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace XR.Mono.Perforce
{

    public class P4ShellTag {
        public int Index { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }

        public override string ToString()
        {
            return string.Format("[P4ShellTag: {0}={1}]", Key, Value);
        }
    }
}
