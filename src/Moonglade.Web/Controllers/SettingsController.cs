﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement.Mvc;
using Moonglade.Caching;
using Moonglade.Caching.Filters;
using Moonglade.Configuration;
using Moonglade.Configuration.Settings;
using Moonglade.Core;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Setup;
using Moonglade.Notification.Client;
using Moonglade.Utils;
using Moonglade.Web.Models;
using Moonglade.Web.Models.Settings;
using NUglify;
using System;
using System.Data;
using System.Reflection;
using System.Threading.Tasks;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        #region Private Fields

        private readonly IBlogConfig _blogConfig;
        private readonly IBlogAudit _blogAudit;
        private readonly ILogger<SettingsController> _logger;

        #endregion

        public SettingsController(
            IBlogConfig blogConfig,
            IBlogAudit blogAudit,
            ILogger<SettingsController> logger)
        {
            _blogConfig = blogConfig;
            _blogAudit = blogAudit;

            _logger = logger;
        }

        [HttpGet("release/check")]
        [ProducesResponseType(typeof(CheckNewReleaseResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> CheckNewRelease([FromServices] IReleaseCheckerClient releaseCheckerClient)
        {
            var info = await releaseCheckerClient.CheckNewReleaseAsync();

            var asm = Assembly.GetEntryAssembly();
            var currentVersion = new Version(asm.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version);
            var latestVersion = new Version(info.TagName.Replace("v", string.Empty));

            var hasNewVersion = latestVersion > currentVersion && !info.PreRelease;

            var result = new CheckNewReleaseResult
            {
                HasNewRelease = hasNewVersion,
                CurrentAssemblyFileVersion = currentVersion.ToString(),
                LatestReleaseInfo = info
            };

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("set-lang")]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(culture)) return BadRequest();

                Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new(culture)),
                    new() { Expires = DateTimeOffset.UtcNow.AddYears(1) }
                );

                return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "~/" : returnUrl);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message, culture, returnUrl);

                // We shall not respect the return URL now, because the returnUrl might be hacking.
                return LocalRedirect("~/");
            }
        }

        [HttpPost("general")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { CacheDivision.General, "theme" })]
        public async Task<IActionResult> General([FromForm] MagicWrapper<GeneralSettingsViewModel> wrapperModel, [FromServices] ITimeZoneResolver timeZoneResolver)
        {
            var model = wrapperModel.ViewModel;

            var settings = _blogConfig.GeneralSettings;
            settings.MetaKeyword = model.MetaKeyword;
            settings.MetaDescription = model.MetaDescription;
            settings.CanonicalPrefix = model.CanonicalPrefix;
            settings.SiteTitle = model.SiteTitle;
            settings.Copyright = model.Copyright;
            settings.LogoText = model.LogoText;
            settings.SideBarCustomizedHtmlPitch = model.SideBarCustomizedHtmlPitch;
            settings.SideBarOption = Enum.Parse<SideBarOption>(model.SideBarOption);
            settings.FooterCustomizedHtmlPitch = model.FooterCustomizedHtmlPitch;
            settings.TimeZoneUtcOffset = timeZoneResolver.GetTimeSpanByZoneId(model.SelectedTimeZoneId).ToString();
            settings.TimeZoneId = model.SelectedTimeZoneId;
            settings.ThemeId = model.SelectedThemeId;
            settings.OwnerName = model.OwnerName;
            settings.OwnerEmail = model.OwnerEmail;
            settings.Description = model.OwnerDescription;
            settings.ShortDescription = model.OwnerShortDescription;
            settings.AutoDarkLightTheme = model.AutoDarkLightTheme;

            await _blogConfig.SaveAsync(_blogConfig.GeneralSettings);

            AppDomain.CurrentDomain.SetData("CurrentThemeColor", null);

            await _blogAudit.AddEntry(BlogEventType.Settings, BlogEventId.SettingsSavedGeneral, "General Settings updated.");

            return NoContent();
        }

        [HttpPost("content")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Content([FromForm] MagicWrapper<ContentSettingsViewModel> wrapperModel)
        {
            var model = wrapperModel.ViewModel;

            var settings = _blogConfig.ContentSettings;
            settings.DisharmonyWords = model.DisharmonyWords;
            settings.EnableComments = model.EnableComments;
            settings.RequireCommentReview = model.RequireCommentReview;
            settings.EnableWordFilter = model.EnableWordFilter;
            settings.WordFilterMode = Enum.Parse<WordFilterMode>(model.WordFilterMode);
            settings.PostListPageSize = model.PostListPageSize;
            settings.HotTagAmount = model.HotTagAmount;
            settings.EnableGravatar = model.EnableGravatar;
            settings.ShowCalloutSection = model.ShowCalloutSection;
            settings.CalloutSectionHtmlPitch = model.CalloutSectionHtmlCode;
            settings.ShowPostFooter = model.ShowPostFooter;
            settings.PostFooterHtmlPitch = model.PostFooterHtmlCode;

            await _blogConfig.SaveAsync(_blogConfig.ContentSettings);
            await _blogAudit.AddEntry(BlogEventType.Settings, BlogEventId.SettingsSavedContent, "Content Settings updated.");

            return NoContent();
        }

        [HttpPost("notification")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Notification([FromForm] MagicWrapper<NotificationSettingsViewModel> wrapperModel)
        {
            var model = wrapperModel.ViewModel;

            var settings = _blogConfig.NotificationSettings;
            settings.EmailDisplayName = model.EmailDisplayName;
            settings.EnableEmailSending = model.EnableEmailSending;
            settings.SendEmailOnCommentReply = model.SendEmailOnCommentReply;
            settings.SendEmailOnNewComment = model.SendEmailOnNewComment;
            settings.AzureFunctionEndpoint = model.AzureFunctionEndpoint;

            await _blogConfig.SaveAsync(settings);
            await _blogAudit.AddEntry(BlogEventType.Settings, BlogEventId.SettingsSavedNotification, "Notification Settings updated.");

            return NoContent();
        }

        [HttpPost("test-email")]
        [IgnoreAntiforgeryToken]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> TestEmail([FromServices] IBlogNotificationClient notificationClient)
        {
            await notificationClient.TestNotificationAsync();
            return Ok(true);
        }

        [HttpPost("subscription")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Subscription([FromForm] MagicWrapper<SubscriptionSettingsViewModel> wrapperModel)
        {
            var model = wrapperModel.ViewModel;

            var settings = _blogConfig.FeedSettings;
            settings.AuthorName = model.AuthorName;
            settings.RssCopyright = model.RssCopyright;
            settings.RssItemCount = model.RssItemCount;
            settings.RssTitle = model.RssTitle;
            settings.UseFullContent = model.UseFullContent;

            await _blogConfig.SaveAsync(settings);
            await _blogAudit.AddEntry(BlogEventType.Settings, BlogEventId.SettingsSavedSubscription, "Subscription Settings updated.");

            return NoContent();
        }

        [HttpPost("watermark")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Image([FromForm] MagicWrapper<ImageSettingsViewModel> wrapperModel)
        {
            var model = wrapperModel.ViewModel;

            var settings = _blogConfig.ImageSettings;
            settings.IsWatermarkEnabled = model.IsWatermarkEnabled;
            settings.KeepOriginImage = model.KeepOriginImage;
            settings.WatermarkFontSize = model.WatermarkFontSize;
            settings.WatermarkText = model.WatermarkText;
            settings.UseFriendlyNotFoundImage = model.UseFriendlyNotFoundImage;
            settings.FitImageToDevicePixelRatio = model.FitImageToDevicePixelRatio;
            settings.EnableCDNRedirect = model.EnableCDNRedirect;

            if (model.EnableCDNRedirect)
            {
                if (string.IsNullOrWhiteSpace(model.CDNEndpoint))
                {
                    settings.EnableCDNRedirect = false;

                    ModelState.AddModelError(nameof(model.CDNEndpoint), $"{nameof(model.CDNEndpoint)} must be specified when {nameof(model.EnableCDNRedirect)} is enabled.");

                    return BadRequest(ModelState.CombineErrorMessages());
                }

                _logger.LogWarning("Images are configured to use CDN, the endpoint is out of control, use it on your own risk.");

                // Validate endpoint Url to avoid security risks
                // But it still has risks:
                // e.g. If the endpoint is compromised, the attacker could return any kind of response from a image with a big fuck to a script that can attack users.

                var endpoint = model.CDNEndpoint;
                var isValidEndpoint = endpoint.IsValidUrl(UrlExtension.UrlScheme.Https);
                if (!isValidEndpoint)
                {
                    ModelState.AddModelError(nameof(model.CDNEndpoint), "CDN Endpoint is not a valid HTTPS Url.");
                    return BadRequest(ModelState.CombineErrorMessages());
                }

                settings.CDNEndpoint = model.CDNEndpoint;
            }

            await _blogConfig.SaveAsync(settings);
            await _blogAudit.AddEntry(BlogEventType.Settings, BlogEventId.SettingsSavedImage, "Image Settings updated.");

            return NoContent();
        }

        [HttpPost("advanced")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Advanced([FromForm] MagicWrapper<AdvancedSettingsViewModel> wrapperModel)
        {
            var model = wrapperModel.ViewModel;

            var settings = _blogConfig.AdvancedSettings;
            settings.RobotsTxtContent = model.RobotsTxtContent;
            settings.EnablePingBackSend = model.EnablePingbackSend;
            settings.EnablePingBackReceive = model.EnablePingbackReceive;
            settings.EnableOpenGraph = model.EnableOpenGraph;
            settings.EnableOpenSearch = model.EnableOpenSearch;
            settings.EnableMetaWeblog = model.EnableMetaWeblog;
            settings.WarnExternalLink = model.WarnExternalLink;
            settings.AllowScriptsInPage = model.AllowScriptsInPage;
            settings.ShowAdminLoginButton = model.ShowAdminLoginButton;

            if (!string.IsNullOrWhiteSpace(model.MetaWeblogPassword))
            {
                settings.MetaWeblogPasswordHash = Helper.HashPassword(model.MetaWeblogPassword);
            }

            await _blogConfig.SaveAsync(settings);
            await _blogAudit.AddEntry(BlogEventType.Settings, BlogEventId.SettingsSavedAdvanced, "Advanced Settings updated.");
            return NoContent();
        }

        [HttpPost("shutdown")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        public IActionResult Shutdown([FromServices] IHostApplicationLifetime applicationLifetime)
        {
            _logger.LogWarning($"Shutdown is requested by '{User.Identity?.Name}'.");
            applicationLifetime.StopApplication();
            return Accepted();
        }

        [HttpPost("reset")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        public async Task<IActionResult> Reset([FromServices] IDbConnection dbConnection, [FromServices] IHostApplicationLifetime applicationLifetime)
        {
            _logger.LogWarning($"System reset is requested by '{User.Identity?.Name}', IP: {HttpContext.Connection.RemoteIpAddress}.");

            var setupHelper = new SetupRunner(dbConnection);
            setupHelper.ClearData();

            await _blogAudit.AddEntry(BlogEventType.Settings, BlogEventId.SettingsSavedAdvanced, "System reset.");

            applicationLifetime.StopApplication();
            return Accepted();
        }

        [HttpPost("custom-css")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CustomStyleSheet([FromForm] MagicWrapper<CustomStyleSheetSettingsViewModel> wrapperModel)
        {
            var model = wrapperModel.ViewModel;
            var settings = _blogConfig.CustomStyleSheetSettings;

            if (model.EnableCustomCss && string.IsNullOrWhiteSpace(model.CssCode))
            {
                ModelState.AddModelError(nameof(CustomStyleSheetSettingsViewModel.CssCode), "CSS Code is required");
                return BadRequest(ModelState.CombineErrorMessages());
            }

            var uglifyTest = Uglify.Css(model.CssCode);
            if (uglifyTest.HasErrors)
            {
                foreach (var err in uglifyTest.Errors)
                {
                    ModelState.AddModelError(model.CssCode, err.ToString());
                }
                return BadRequest(ModelState.CombineErrorMessages());
            }

            settings.EnableCustomCss = model.EnableCustomCss;
            settings.CssCode = model.CssCode;

            await _blogConfig.SaveAsync(settings);
            await _blogAudit.AddEntry(BlogEventType.Settings, BlogEventId.SettingsSavedAdvanced, "Custom Style Sheet Settings updated.");
            return NoContent();
        }

        [HttpGet("password/generate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GeneratePassword()
        {
            var password = Helper.GeneratePassword(10, 3);
            return Ok(new
            {
                ServerTimeUtc = DateTime.UtcNow,
                Password = password
            });
        }

        [HttpDelete("auditlogs/clear")]
        [FeatureGate(FeatureFlags.EnableAudit)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> ClearAuditLogs()
        {
            await _blogAudit.ClearAuditLog();
            return NoContent();
        }
    }
}