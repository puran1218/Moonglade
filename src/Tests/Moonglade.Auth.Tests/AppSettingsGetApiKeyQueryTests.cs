using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Moonglade.Auth.Tests
{
    [TestFixture]
    public class AppSettingsGetApiKeyQueryTests
    {
        private MockRepository _mockRepository;
        private Mock<IOptions<AuthenticationSettings>> _mockOptions;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new MockRepository(MockBehavior.Default);
            _mockOptions = _mockRepository.Create<IOptions<AuthenticationSettings>>();
        }

        private AppSettingsGetApiKeyQuery CreateAppSettingsGetApiKeyQuery()
        {
            return new(_mockOptions.Object);
        }

        [Test]
        public async Task Execute_ExpectedBehavior()
        {
            _mockOptions.Setup(p => p.Value).Returns(new AuthenticationSettings
            {
                Provider = AuthenticationProvider.AzureAD,
                ApiKeys = new ApiKey[]
                {
                    new () { Key = "fuck996", Owner = "996fucker" },
                    new () { Key = "pdd007", Owner = "gotoicu" },
                    new () { Key = "251", Owner = "hwaiguo" }
                }
            });

            var appSettingsGetApiKeyQuery = CreateAppSettingsGetApiKeyQuery();
            string providedApiKey = "251";

            var result = await appSettingsGetApiKeyQuery.Execute(providedApiKey);

            Assert.AreEqual("251", result.Key);
        }
    }
}
