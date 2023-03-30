﻿using Microsoft.Extensions.Logging;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace Moonglade.Pingback
{
    public class PingbackService : IPingbackService
    {
        private readonly ILogger<PingbackService> _logger;
        private readonly IPingSourceInspector _pingSourceInspector;
        private readonly IRepository<PingbackEntity> _pingbackRepo;
        private readonly IRepository<PostEntity> _postRepo;

        private string _sourceUrl;
        private string _targetUrl;

        public PingbackService(
            ILogger<PingbackService> logger,
            IPingSourceInspector pingSourceInspector,
            IRepository<PingbackEntity> pingbackRepo,
            IRepository<PostEntity> postRepo)
        {
            _logger = logger;
            _pingSourceInspector = pingSourceInspector;
            _pingbackRepo = pingbackRepo;
            _postRepo = postRepo;
        }

        public async Task<PingbackResponse> ReceivePingAsync(string requestBody, string ip, Action<PingbackEntity> pingSuccessAction)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    _logger.LogError("Pingback requestBody is null");
                    return PingbackResponse.GenericError;
                }

                var valid = ValidateRequest(requestBody);
                if (!valid) return PingbackResponse.InvalidPingRequest;

                _logger.LogInformation($"Processing Pingback from: {_sourceUrl} ({ip}) to {_targetUrl}");

                var pingRequest = await _pingSourceInspector.ExamineSourceAsync(_sourceUrl, _targetUrl);
                if (null == pingRequest) return PingbackResponse.InvalidPingRequest;
                if (!pingRequest.SourceHasLink)
                {
                    _logger.LogError("Pingback error: The source URI does not contain a link to the target URI.");
                    return PingbackResponse.Error17SourceNotContainTargetUri;
                }
                if (pingRequest.ContainsHtml)
                {
                    _logger.LogWarning("Spam detected on current Pingback...");
                    return PingbackResponse.SpamDetectedFakeNotFound;
                }

                var (slug, pubDate) = GetSlugInfoFromUrl(pingRequest.TargetUrl);
                var spec = new PostSpec(pubDate, slug);
                var (id, title) = await _postRepo.SelectFirstOrDefaultAsync(spec, p => new Tuple<Guid, string>(p.Id, p.Title));
                if (id == Guid.Empty)
                {
                    _logger.LogError($"Can not get post id and title for url '{pingRequest.TargetUrl}'");
                    return PingbackResponse.Error32TargetUriNotExist;
                }

                _logger.LogInformation($"Post '{id}:{title}' is found for ping.");

                var pinged = _pingbackRepo.Any(p =>
                    p.TargetPostId == id &&
                    p.SourceUrl == pingRequest.SourceUrl &&
                    p.SourceIp.Trim() == ip);

                if (pinged) return PingbackResponse.Error48PingbackAlreadyRegistered;

                _logger.LogInformation("Adding received pingback...");

                var uri = new Uri(_sourceUrl);
                var obj = new PingbackEntity
                {
                    Id = Guid.NewGuid(),
                    PingTimeUtc = DateTime.UtcNow,
                    Domain = uri.Host,
                    SourceUrl = _sourceUrl,
                    SourceTitle = pingRequest.Title,
                    TargetPostId = id,
                    TargetPostTitle = title,
                    SourceIp = ip
                };

                await _pingbackRepo.AddAsync(obj);
                pingSuccessAction?.Invoke(obj);

                return PingbackResponse.Success;
            }
            catch (Exception e)
            {
                _logger.LogError(e, nameof(ReceivePingAsync));
                return PingbackResponse.GenericError;
            }
        }

        private static (string Slug, DateTime PubDate) GetSlugInfoFromUrl(string url)
        {
            var blogSlugRegex = new Regex(@"^https?:\/\/.*\/post\/(?<yyyy>\d{4})\/(?<MM>\d{1,12})\/(?<dd>\d{1,31})\/(?<slug>.*)$");
            Match match = blogSlugRegex.Match(url);
            if (!match.Success)
            {
                throw new FormatException("Invalid Slug Format");
            }

            int year = int.Parse(match.Groups["yyyy"].Value);
            int month = int.Parse(match.Groups["MM"].Value);
            int day = int.Parse(match.Groups["dd"].Value);
            string slug = match.Groups["slug"].Value;
            var date = new DateTime(year, month, day);

            return (slug, date);
        }

        public async Task<IReadOnlyList<PingbackEntity>> GetPingbacksAsync()
        {
            var list = await _pingbackRepo.GetAsync();
            return list;
        }

        public async Task DeletePingback(Guid id)
        {
            await _pingbackRepo.DeleteAsync(id);
        }

        private bool ValidateRequest(string requestBody)
        {
            _logger.LogInformation($"Pingback received xml: {requestBody}");

            if (!requestBody.Contains("<methodName>pingback.ping</methodName>"))
            {
                _logger.LogWarning("Could not find pingback method, request has been terminated.");
                return false;
            }

            var doc = new XmlDocument();
            doc.LoadXml(requestBody);

            var list = doc.SelectNodes("methodCall/params/param/value/string") ??
                       doc.SelectNodes("methodCall/params/param/value");

            if (list is null)
            {
                _logger.LogWarning("Could not find Pingback sourceUrl and targetUrl, request has been terminated.");
                return false;
            }

            _sourceUrl = list[0]?.InnerText.Trim();
            _targetUrl = list[1]?.InnerText.Trim();

            return true;
        }
    }
}
