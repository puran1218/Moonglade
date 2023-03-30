using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Configuration;
using Moonglade.Core;
using Moonglade.Web.Pages.Admin;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Pages.Admin
{
    [TestFixture]

    public class EditPostModelTests
    {
        private MockRepository _mockRepository;

        private Mock<ICategoryService> _mockCategoryService;
        private Mock<IPostQueryService> _mockPostService;
        private Mock<ITimeZoneResolver> _mockTZoneResolver;
        private Mock<IBlogConfig> _mockBlogConfig;

        private static readonly Guid Uid = Guid.Parse("76169567-6ff3-42c0-b163-a883ff2ac4fb");
        private static readonly Category Cat = new()
        {
            DisplayName = "WTF",
            Id = Guid.Parse("6364e9be-2423-44da-bd11-bc6fa9c3fa5d"),
            Note = "A wonderful contry",
            RouteName = "wtf"
        };

        private static readonly Post Post = new()
        {
            Id = Uid,
            Title = FakeData.Title2,
            Slug = FakeData.Slug1,
            ContentAbstract = "Get some fubao",
            RawPostContent = "<p>Get some fubao</p>",
            ContentLanguageCode = "en-us",
            Featured = true,
            ExposedToSiteMap = true,
            IsFeedIncluded = true,
            IsPublished = true,
            CommentEnabled = true,
            PubDateUtc = new(2019, 9, 6, 6, 35, 7),
            LastModifiedUtc = new(2020, 9, 6, 6, 35, 7),
            CreateTimeUtc = new(2018, 9, 6, 6, 35, 7),
            Tags = new[]
            {
                new Tag { DisplayName = "Fubao", Id = FakeData.Int2, NormalizedName = FakeData.ShortString1 },
                new Tag { DisplayName = FakeData.ShortString2, Id = FakeData.Int1, NormalizedName = FakeData.ShortString2 }
            },
            Categories = new[] { Cat }
        };

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockCategoryService = _mockRepository.Create<ICategoryService>();
            _mockPostService = _mockRepository.Create<IPostQueryService>();
            _mockTZoneResolver = _mockRepository.Create<ITimeZoneResolver>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
        }

        private EditPostModel CreateEditPostModel()
        {
            return new(
                _mockCategoryService.Object,
                _mockPostService.Object,
                _mockTZoneResolver.Object);
        }

        [Test]
        public async Task OnGetAsync_CreatePost()
        {
            IReadOnlyList<Category> cats = new List<Category>
            {
                new(){Id = Guid.Empty, DisplayName = FakeData.Title3, Note = "Get into ICU", RouteName = FakeData.Slug2}
            };

            _mockCategoryService.Setup(p => p.GetAllAsync()).Returns(Task.FromResult(cats));

            var editPostModel = CreateEditPostModel();
            var result = await editPostModel.OnGetAsync(null);

            Assert.IsInstanceOf<PageResult>(result);
            Assert.IsNotNull(editPostModel.ViewModel);
            Assert.IsNull(editPostModel.ViewModel.EditorContent);
        }

        [Test]
        public async Task OnGetAsync_NotFound()
        {
            _mockPostService.Setup(p => p.GetAsync(Guid.Empty)).Returns(Task.FromResult((Post)null));

            var editPostModel = CreateEditPostModel();
            var result = await editPostModel.OnGetAsync(Guid.Empty);

            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task OnGetAsync_FoundPost()
        {
            IReadOnlyList<Category> cats = new List<Category> { Cat };

            _mockPostService.Setup(p => p.GetAsync(Uid)).Returns(Task.FromResult(Post));
            _mockCategoryService.Setup(p => p.GetAllAsync()).Returns(Task.FromResult(cats));

            var editPostModel = CreateEditPostModel();
            var result = await editPostModel.OnGetAsync(Uid);

            Assert.IsInstanceOf<PageResult>(result);
            Assert.IsNotNull(editPostModel.ViewModel);
        }
    }
}
