using Moonglade.Configuration;
using Moonglade.Web.Pages.Settings;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages.Settings
{
    [TestFixture]
    public class ImageModelTests
    {
        private MockRepository _mockRepository;
        private Mock<IBlogConfig> _mockBlogConfig;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
        }

        private ImageModel CreateWatermarkModel()
        {
            return new(
                _mockBlogConfig.Object);
        }

        [Test]
        public void OnGet_StateUnderTest_ExpectedBehavior()
        {
            _mockBlogConfig.Setup(p => p.ImageSettings).Returns(new ImageSettings());

            var watermarkModel = CreateWatermarkModel();
            watermarkModel.OnGet();

            Assert.IsNotNull(watermarkModel.ViewModel);
        }
    }
}
