using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.Logging;
using Moonglade.Core;
using Moonglade.Web.ViewComponents;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.ViewComponents
{
    [TestFixture]
    public class RssListViewComponentTests
    {
        private MockRepository _mockRepository;

        private Mock<ILogger<RssListViewComponent>> _mockLogger;
        private Mock<ICategoryService> _mockCategoryService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockLogger = _mockRepository.Create<ILogger<RssListViewComponent>>();
            _mockCategoryService = _mockRepository.Create<ICategoryService>();
        }

        private RssListViewComponent CreateComponent()
        {
            return new(
                _mockLogger.Object,
                _mockCategoryService.Object);
        }

        [Test]
        public async Task InvokeAsync_Exception()
        {
            _mockCategoryService.Setup(p => p.GetAllAsync()).Throws(new(FakeData.ShortString2));

            var component = CreateComponent();
            var result = await component.InvokeAsync();

            Assert.IsInstanceOf<ContentViewComponentResult>(result);
        }

        [Test]
        public async Task InvokeAsync_View()
        {
            IReadOnlyList<Category> cats = new List<Category>
            {
                new() {DisplayName = "Fubao", Id = Guid.Empty, Note = FakeData.ShortString2, RouteName = FakeData.Slug2}
            };

            _mockCategoryService.Setup(p => p.GetAllAsync()).Returns(Task.FromResult(cats));

            var component = CreateComponent();
            var result = await component.InvokeAsync();

            Assert.IsInstanceOf<ViewViewComponentResult>(result);

            var model = ((ViewViewComponentResult)result).ViewData.Model;
            Assert.IsNotNull(model);
        }
    }
}
