using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Linq;

namespace Moonglade.Pingback.Tests
{
    [TestFixture]
    public class ServiceCollectionExtensionsTests
    {
        [Test]
        public void AddPingback_OK()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddPingback();

            var obj1 = services.FirstOrDefault(p => p.ServiceType == typeof(IPingSourceInspector));
            Assert.IsNotNull(obj1);

            var obj3 = services.FirstOrDefault(p => p.ServiceType == typeof(IPingbackSender));
            Assert.IsNotNull(obj3);

            var obj4 = services.FirstOrDefault(p => p.ServiceType == typeof(IPingbackService));
            Assert.IsNotNull(obj4);
        }
    }
}
