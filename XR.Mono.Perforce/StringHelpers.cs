using System;

namespace XR.Mono.Perforce
{
    public static class StringHelpers
    {
        public static string Fmt( this string fmt, params object[] args )
        {
            if ( string.IsNullOrEmpty(fmt) ) return string.Empty;
            return String.Format( fmt, args );
        }
    }
}

