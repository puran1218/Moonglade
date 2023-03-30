﻿using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Pingback.Tests
{
    [TestFixture]
    public class PingbackSenderTests
    {
        private MockRepository _mockRepository;

        private Mock<HttpMessageHandler> _handlerMock;
        private HttpClient _magicHttpClient;

        private Mock<IPingbackWebRequest> _mockPingbackWebRequest;
        private Mock<ILogger<PingbackSender>> _mockLogger;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _handlerMock = _mockRepository.Create<HttpMessageHandler>();
            _mockPingbackWebRequest = _mockRepository.Create<IPingbackWebRequest>();
            _mockLogger = _mockRepository.Create<ILogger<PingbackSender>>();
        }

        private PingbackSender CreatePingbackSender()
        {
            _magicHttpClient = new(_handlerMock.Object);

            return new(
                _magicHttpClient,
                _mockPingbackWebRequest.Object,
                _mockLogger.Object);
        }

        [Test]
        public async Task TrySendPingAsync_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(""),
                    Headers =
                    {
                        { "x-pingback", "https://greenhat.today/ping" }
                    }
                })
                .Verifiable();

            var mockWebResponse = _mockRepository.Create<WebResponse>();
            _mockPingbackWebRequest.Setup(p => p.GetReponse(It.IsAny<HttpWebRequest>())).Returns(mockWebResponse.Object);

            var pingbackSender = CreatePingbackSender();
            string postUrl = "https://996.icu/work-996-sick-icu";
            string postContent = "996 is fubao, reject fubao and you will get <a href=\"https://251.today\">251</a> today!";

            // Act
            await pingbackSender.TrySendPingAsync(postUrl, postContent);

            _mockPingbackWebRequest.Verify(p => p.BuildHttpWebRequest(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<Uri>()));
            _mockPingbackWebRequest.Verify(p => p.GetReponse(It.IsAny<HttpWebRequest>()));

            Assert.Pass();
        }
    }
}
