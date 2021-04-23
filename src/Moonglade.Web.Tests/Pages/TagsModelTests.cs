﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Moonglade.Core;
using Moonglade.Web.Pages;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Pages
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class TagsModelTests
    {
        private MockRepository _mockRepository;

        private Mock<ITagService> _mockTagService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockTagService = _mockRepository.Create<ITagService>();
        }

        private TagsModel CreateTagsModel()
        {
            return new(_mockTagService.Object);
        }

        [Test]
        public async Task OnGet_StateUnderTest_ExpectedBehavior()
        {
            var fakeTags = new List<KeyValuePair<Tag, int>>
            {
                new(new() { DisplayName = "Huawei", Id = 35, NormalizedName = "aiguo" }, 251),
                new(new() { DisplayName = "Ali", Id = 35, NormalizedName = "fubao" }, 996)
            };

            _mockTagService.Setup(p => p.GetTagCountList())
                .Returns(Task.FromResult((IReadOnlyList<KeyValuePair<Tag, int>>)fakeTags));

            var tagsModel = CreateTagsModel();

            await tagsModel.OnGet();

            Assert.IsNotNull(tagsModel.Tags);
        }
    }
}
