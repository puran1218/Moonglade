﻿using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Syndication.Tests
{
    [TestFixture]
    public class FileSystemOpmlWriterTests
    {
        [Test]
        public async Task WriteOpmlFile()
        {
            var catInfos = new List<KeyValuePair<string, string>>
            {
                new("Work 996", "work-996")
            };
            var siteRootUrl = "https://996.icu";

            var info = new OpmlDoc
            {
                SiteTitle = $"Work 996 - OPML",
                ContentInfo = catInfos,
                HtmlUrl = $"{siteRootUrl}/post",
                XmlUrl = $"{siteRootUrl}/rss",
                XmlUrlTemplate = $"{siteRootUrl}/rss/[catTitle]",
                HtmlUrlTemplate = $"{siteRootUrl}/category/[catTitle]"
            };

            var writer = new StringOpmlWriter();
            var xml = await writer.GetOpmlDataAsync(info);

            Assert.IsNotNull(xml);
        }
    }
}
