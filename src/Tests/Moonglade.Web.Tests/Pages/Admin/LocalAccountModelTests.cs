using Moonglade.Auth;
using Moonglade.Web.Pages.Admin;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Pages.Admin
{
    [TestFixture]

    public class LocalAccountModelTests
    {
        private MockRepository _mockRepository;
        private Mock<ILocalAccountService> _mockLocalAccountService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockLocalAccountService = _mockRepository.Create<ILocalAccountService>();
        }

        private LocalAccountModel CreateLocalAccountModel()
        {
            return new(_mockLocalAccountService.Object);
        }

        [Test]
        public async Task OnGet_StateUnderTest_ExpectedBehavior()
        {
            IReadOnlyList<Account> accounts = new List<Account>();
            _mockLocalAccountService.Setup(p => p.GetAllAsync()).Returns(Task.FromResult(accounts));

            var localAccountModel = CreateLocalAccountModel();
            await localAccountModel.OnGet();

            Assert.IsNotNull(localAccountModel.Accounts);
        }
    }
}
