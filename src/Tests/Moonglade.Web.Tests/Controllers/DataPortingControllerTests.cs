using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Data.Porting;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    public class DataPortingControllerTests
    {
        private MockRepository _mockRepository;
        private Mock<IExportManager> _mockExportManager;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockExportManager = _mockRepository.Create<IExportManager>();
        }

        private DataPortingController CreateDataPortingController()
        {
            return new(
                _mockExportManager.Object);
        }

        [Test]
        public async Task ExportDownload_SingleJsonFile()
        {
            _mockExportManager.Setup(p => p.ExportData(ExportDataType.Tags, CancellationToken.None))
                .Returns(Task.FromResult(new ExportResult
                {
                    ExportFormat = ExportFormat.SingleJsonFile,
                    Content = Array.Empty<byte>()
                }));

            var settingsController = CreateDataPortingController();
            ExportDataType type = ExportDataType.Tags;

            var result = await settingsController.ExportDownload(type, CancellationToken.None);
            Assert.IsInstanceOf<FileContentResult>(result);
        }

        [Test]
        public async Task ExportDownload_SingleCSVFile()
        {
            _mockExportManager.Setup(p => p.ExportData(ExportDataType.Categories, CancellationToken.None))
                .Returns(Task.FromResult(new ExportResult
                {
                    ExportFormat = ExportFormat.SingleCSVFile,
                    FilePath = @"C:\996\icu.csv"
                }));

            var settingsController = CreateDataPortingController();
            settingsController.ControllerContext = new()
            {
                HttpContext = new DefaultHttpContext()
            };

            ExportDataType type = ExportDataType.Categories;

            var result = await settingsController.ExportDownload(type, CancellationToken.None);
            Assert.IsInstanceOf<PhysicalFileResult>(result);
        }

        [Test]
        public async Task ExportDownload_ZippedJsonFiles()
        {
            _mockExportManager.Setup(p => p.ExportData(ExportDataType.Posts, CancellationToken.None))
                .Returns(Task.FromResult(new ExportResult
                {
                    ExportFormat = ExportFormat.ZippedJsonFiles,
                    FilePath = @"C:\996\icu.zip"
                }));

            var settingsController = CreateDataPortingController();
            settingsController.ControllerContext = new()
            {
                HttpContext = new DefaultHttpContext()
            };

            ExportDataType type = ExportDataType.Posts;

            var result = await settingsController.ExportDownload(type, CancellationToken.None);
            Assert.IsInstanceOf<PhysicalFileResult>(result);
        }

    }
}
