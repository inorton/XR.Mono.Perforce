using System;
using NUnit.Framework;
using XR.Mono.Perforce;

namespace XR.Mono.Perforce.Tests
{
    [TestFixture]
    public class Test
    {
        [Test]
        public void Connect()
        {
            var p4 = new P4();
            var server = p4.Connect("perforce.ncipher.com:1666");
            Assert.IsNotNull( server );
            Assert.IsTrue( server.Count > 0 );
        }

        [Test]
        [ExpectedException(typeof(P4Exception))]
        public void NoConnect() {
            var p4 = new P4();
            p4.Connect("nosuchhost:999");
        }

        [Test]
        public void Login()
        {
            CallLogin();
        }

        public P4 CallLogin()
        {
            var p4 = new P4();
            p4.Connect("perforce.ncipher.com:1666");
            var name = p4.Login("inb", System.IO.File.ReadAllText( Environment.GetEnvironmentVariable("HOME") + "/p4pw.txt") );
            Assert.IsFalse( string.IsNullOrEmpty( name ) );
            Console.Error.WriteLine(name);
            return p4;
        }

        [Test]
        [ExpectedException(typeof(P4Exception))]
        public void NoLogin()
        {
            var p4 = new P4();
            p4.Connect("perforce.ncipher.com:1666");
            var name = p4.Login("nosuchusername", "garbage" );
            Assert.IsFalse( string.IsNullOrEmpty( name ) );
            Console.Error.WriteLine(name);
        }

        [Test]
        public void CreateWorkspace()
        {
            var p4 = CallLogin();
            var wsn = Environment.MachineName + "-" + Guid.NewGuid().ToString().Substring(0,5);
            try {
                p4.CreateWorkspace( wsn, Environment.CurrentDirectory );
                Console.WriteLine( p4.WorkspaceRoot );
                var util = new P4Util( p4 );
                var dl = util.Dirs( "*" );
                foreach ( var d in dl ){
                    Console.Error.WriteLine(d);
                }
            } finally {
                try {
                    p4.RunTagged( Environment.CurrentDirectory, "client", "-d", wsn );
                } catch ( P4Exception ) {}
            }
        }
    }
}

