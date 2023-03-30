﻿using Microsoft.Extensions.Caching.Memory;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Core;
using Moonglade.Web.Pages;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Pages
{
    [TestFixture]

    public class IndexModelTests
    {
        private MockRepository _mockRepository;

        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<IPostQueryService> _mockPostQueryService;
        private Mock<IBlogCache> _mockBlogCache;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockPostQueryService = _mockRepository.Create<IPostQueryService>();
            _mockBlogCache = _mockRepository.Create<IBlogCache>();

            _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings
            {
                PostListPageSize = 10
            });
        }

        private IndexModel CreateIndexModel()
        {
            return new(
                _mockBlogConfig.Object,
                _mockPostQueryService.Object,
                _mockBlogCache.Object);
        }

        [Test]
        public async Task OnGet_StateUnderTest_ExpectedBehavior()
        {
            _mockPostQueryService.Setup(p => p.ListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>()))
                .Returns(Task.FromResult(FakeData.FakePosts));

            _mockPostQueryService.Setup(p => p.CountPublic()).Returns(FakeData.Int2);

            _mockBlogCache.Setup(p =>
                    p.GetOrCreate(CacheDivision.General, "postcount", It.IsAny<Func<ICacheEntry, int>>()))
                .Returns(FakeData.Int2);

            var indexModel = CreateIndexModel();
            int p = 1;

            await indexModel.OnGet(p);

            Assert.IsNotNull(indexModel.Posts);
            Assert.AreEqual(FakeData.Int2, indexModel.Posts.TotalItemCount);
        }
    }
}
