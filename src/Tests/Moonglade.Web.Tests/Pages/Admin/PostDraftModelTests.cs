using Moonglade.Core;
using Moonglade.Data.Spec;
using Moonglade.Web.Pages.Admin;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Pages.Admin
{
    [TestFixture]

    public class PostDraftModelTests
    {
        private MockRepository _mockRepository;
        private Mock<IPostQueryService> _mockPostService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockPostService = _mockRepository.Create<IPostQueryService>();
        }

        private PostDraftModel CreatePostDraftModel()
        {
            return new(_mockPostService.Object);
        }

        [Test]
        public async Task OnGet_StateUnderTest_ExpectedBehavior()
        {
            IReadOnlyList<PostSegment> data = new List<PostSegment>();

            _mockPostService.Setup(p => p.ListSegmentAsync(PostStatus.Draft)).Returns(Task.FromResult(data));

            var postDraftModel = CreatePostDraftModel();
            await postDraftModel.OnGet();

            Assert.IsNotNull(postDraftModel.PostSegments);
        }
    }
}
