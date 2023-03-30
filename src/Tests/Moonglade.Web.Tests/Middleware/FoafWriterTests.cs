using Moonglade.FriendLink;
using Moonglade.Web.Middleware;
using Moonglade.Web.Models;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Middleware
{
    [TestFixture]
    public class FoafWriterTests
    {
        private MockRepository _mockRepository;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
        }

        private FoafWriter CreateFoafWriter()
        {
            return new();
        }

        [Test]
        public async Task GetFoafData_StateUnderTest_ExpectedBehavior()
        {
            var foafWriter = CreateFoafWriter();
            FoafDoc doc = new FoafDoc
            {
                Name = "996.icu",
                BlogUrl = FakeData.Url1,
                Email = "fubao@996.icu",
                PhotoUrl = "https://996.icu/fubao.png"
            };
            string currentRequestUrl = "https://996.icu/fubao";
            IReadOnlyList<Link> friends = new List<Link>
            {
                new(){ Id = Guid.Empty, LinkUrl = "https://greenhat.today", Title = "Work 996 and wear green hat" }
            };

            // Act
            var result = await foafWriter.GetFoafData(
                doc,
                currentRequestUrl,
                friends);

            Assert.IsFalse(string.IsNullOrWhiteSpace(result));
        }
    }
}
