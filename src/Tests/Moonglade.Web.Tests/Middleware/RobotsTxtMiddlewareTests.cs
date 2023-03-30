﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Configuration;
using Moonglade.Web.Middleware;
using Moq;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Middleware
{
    [TestFixture]
    public class RobotsTxtMiddlewareTests
    {
        [Test]
        public async Task Invoke_NonRobotsTxtRequestPath()
        {
            var reqMock = new Mock<HttpRequest>();
            reqMock.SetupGet(r => r.Path).Returns("/996");

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(c => c.Request).Returns(reqMock.Object);

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new RobotsTxtMiddleware(RequestDelegate);

            await middleware.Invoke(httpContextMock.Object, null);

            Assert.Pass();
        }

        [Test]
        public async Task Invoke_RobotsTxtRequestPath_HasContent()
        {
            var blogConfigMock = new Mock<IBlogConfig>();
            blogConfigMock.Setup(c => c.AdvancedSettings).Returns(new AdvancedSettings
            {
                RobotsTxtContent = FakeData.ShortString2
            });

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new RobotsTxtMiddleware(RequestDelegate);

            var ctx = new DefaultHttpContext();
            ctx.Response.Body = new MemoryStream();
            ctx.Request.Path = "/robots.txt";

            await middleware.Invoke(ctx, blogConfigMock.Object);

            Assert.AreEqual("text/plain", ctx.Response.ContentType);

            Assert.Pass();
        }

        [Test]
        public async Task Invoke_RobotsTxtRequestPath_NoContent()
        {
            var blogConfigMock = new Mock<IBlogConfig>();
            blogConfigMock.Setup(c => c.AdvancedSettings).Returns(new AdvancedSettings
            {
                RobotsTxtContent = string.Empty
            });

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new RobotsTxtMiddleware(RequestDelegate);

            var ctx = new DefaultHttpContext();
            ctx.Response.Body = new MemoryStream();
            ctx.Request.Path = "/robots.txt";

            await middleware.Invoke(ctx, blogConfigMock.Object);

            Assert.AreEqual(StatusCodes.Status404NotFound, ctx.Response.StatusCode);

            Assert.Pass();
        }

        [Test]
        public void UseRobotsTxt_Ext()
        {
            var serviceCollection = new ServiceCollection();
            var applicationBuilder = new ApplicationBuilder(serviceCollection.BuildServiceProvider());

            applicationBuilder.UseRobotsTxt();

            var app = applicationBuilder.Build();

            Assert.IsInstanceOf<MapWhenMiddleware>(app.Target);
        }
    }
}
