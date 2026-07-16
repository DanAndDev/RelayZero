using NUnit.Framework;
using RelayZero.Bootstrap;
using RelayZero.Client.Application;
using RelayZero.Foundation;
using RelayZero.Server;

namespace RelayZero.Tests.EditMode.Architecture
{
    public sealed class CompositionRootTests
    {
        [Test]
        public void ClientCompositionRootOwnsClientRole()
        {
            ClientCompositionRoot root = ClientCompositionRoot.CreateDefault();

            Assert.That(root.BuildInfo.Role, Is.EqualTo(ApplicationRole.Client));
            Assert.That(root.IsRunning, Is.False);

            root.Start();
            Assert.That(root.IsRunning, Is.True);

            root.Stop();
            Assert.That(root.IsRunning, Is.False);
        }

        [Test]
        public void DedicatedServerCompositionRootOwnsServerRole()
        {
            DedicatedServerCompositionRoot root = DedicatedServerCompositionRoot.CreateDefault();

            Assert.That(root.BuildInfo.Role, Is.EqualTo(ApplicationRole.DedicatedServer));
            Assert.That(root.IsRunning, Is.False);

            root.Start();
            Assert.That(root.IsRunning, Is.True);

            root.Stop();
            Assert.That(root.IsRunning, Is.False);
        }

        [Test]
        public void TestCompositionRootOwnsTestRole()
        {
            TestCompositionRoot root = TestCompositionRoot.CreateDefault();

            Assert.That(root.BuildInfo.Role, Is.EqualTo(ApplicationRole.Test));
            Assert.That(root.IsRunning, Is.False);

            root.Start();
            Assert.That(root.IsRunning, Is.True);

            root.Stop();
            Assert.That(root.IsRunning, Is.False);
        }
    }
}
