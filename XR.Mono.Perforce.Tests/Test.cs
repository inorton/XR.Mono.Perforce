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
            var p4 = new P4();
            p4.Connect("perforce.ncipher.com:1666");
            var name = p4.Login("inb", System.IO.File.ReadAllText( "/home/inb/p4pw.txt") );
            Assert.IsFalse( string.IsNullOrEmpty( name ) );
            Console.Error.WriteLine(name);
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
    }
}

