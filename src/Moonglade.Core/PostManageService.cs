﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Caching;
using Moonglade.Configuration.Settings;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Moonglade.Core
{
    public interface IPostManageService
    {
        Task<PostEntity> CreateAsync(UpdatePostRequest request);
        Task<PostEntity> UpdateAsync(Guid id, UpdatePostRequest request);
        Task RestoreAsync(Guid id);
        Task DeleteAsync(Guid id, bool softDelete = false);
        Task PurgeRecycledAsync();
    }

    public class PostManageService : IPostManageService
    {
        private readonly IBlogAudit _audit;
        private readonly ILogger<PostManageService> _logger;
        private readonly AppSettings _settings;
        private readonly IRepository<TagEntity> _tagRepo;
        private readonly IRepository<PostEntity> _postRepo;
        private readonly IBlogCache _cache;

        private readonly IDictionary<string, string> _tagNormalizationDictionary;

        public PostManageService(
            IBlogAudit audit,
            ILogger<PostManageService> logger,
            IConfiguration configuration,
            IOptions<AppSettings> settings,
            IRepository<TagEntity> tagRepo,
            IRepository<PostEntity> postRepo,
            IBlogCache cache)
        {
            _audit = audit;
            _logger = logger;
            _tagRepo = tagRepo;
            _postRepo = postRepo;
            _cache = cache;
            _settings = settings.Value;

            _tagNormalizationDictionary =
                configuration.GetSection("TagNormalization").Get<Dictionary<string, string>>();
        }

        public async Task<PostEntity> CreateAsync(UpdatePostRequest request)
        {
            var abs = ContentProcessor.GetPostAbstract(
                    string.IsNullOrEmpty(request.Abstract) ? request.EditorContent : request.Abstract.Trim(),
                    _settings.PostAbstractWords,
                    _settings.Editor == EditorChoice.Markdown);

            var post = new PostEntity
            {
                CommentEnabled = request.EnableComment,
                Id = Guid.NewGuid(),
                PostContent = request.EditorContent,
                ContentAbstract = abs,
                CreateTimeUtc = DateTime.UtcNow,
                LastModifiedUtc = DateTime.UtcNow, // Fix draft orders
                Slug = request.Slug.ToLower().Trim(),
                Author = request.Author?.Trim(),
                Title = request.Title.Trim(),
                ContentLanguageCode = request.ContentLanguageCode,
                ExposedToSiteMap = request.ExposedToSiteMap,
                IsFeedIncluded = request.IsFeedIncluded,
                PubDateUtc = request.IsPublished ? DateTime.UtcNow : null,
                IsDeleted = false,
                IsPublished = request.IsPublished,
                IsFeatured = request.IsFeatured,
                IsOriginal = request.IsOriginal,
                OriginLink = string.IsNullOrWhiteSpace(request.OriginLink) ? null : Helper.SterilizeLink(request.OriginLink),
                HeroImageUrl = string.IsNullOrWhiteSpace(request.HeroImageUrl) ? null : Helper.SterilizeLink(request.HeroImageUrl),
                PostExtension = new()
                {
                    Hits = 0,
                    Likes = 0
                }
            };

            // check if exist same slug under the same day
            var todayUtc = DateTime.UtcNow.Date;
            if (_postRepo.Any(new PostSpec(post.Slug, todayUtc)))
            {
                var uid = Guid.NewGuid();
                post.Slug += $"-{uid.ToString().ToLower()[..8]}";
                _logger.LogInformation($"Found conflict for post slug, generated new slug: {post.Slug}");
            }

            // compute hash
            var input = $"{post.Slug}#{post.PubDateUtc.GetValueOrDefault():yyyyMMdd}";
            var checkSum = Helper.ComputeCheckSum(input);
            post.HashCheckSum = checkSum;

            // add categories
            if (request.CategoryIds is { Length: > 0 })
            {
                foreach (var id in request.CategoryIds)
                {
                    post.PostCategory.Add(new()
                    {
                        CategoryId = id,
                        PostId = post.Id
                    });
                }
            }

            // add tags
            if (request.Tags is { Length: > 0 })
            {
                foreach (var item in request.Tags)
                {
                    if (!TagService.ValidateTagName(item))
                    {
                        continue;
                    }

                    var tag = await _tagRepo.GetAsync(q => q.DisplayName == item) ?? await CreateTag(item);
                    post.Tags.Add(tag);
                }
            }

            await _postRepo.AddAsync(post);
            await _audit.AddEntry(BlogEventType.Content, BlogEventId.PostCreated, $"Post created, id: {post.Id}");

            return post;
        }

        private async Task<TagEntity> CreateTag(string item)
        {
            var newTag = new TagEntity
            {
                DisplayName = item,
                NormalizedName = TagService.NormalizeTagName(item, _tagNormalizationDictionary)
            };

            var tag = await _tagRepo.AddAsync(newTag);
            await _audit.AddEntry(BlogEventType.Content, BlogEventId.TagCreated,
                $"Tag '{tag.NormalizedName}' created.");
            return tag;
        }

        public async Task<PostEntity> UpdateAsync(Guid id, UpdatePostRequest request)
        {
            var post = await _postRepo.GetAsync(id);
            if (null == post)
            {
                throw new InvalidOperationException($"Post {id} is not found.");
            }

            post.CommentEnabled = request.EnableComment;
            post.PostContent = request.EditorContent;
            post.ContentAbstract = ContentProcessor.GetPostAbstract(
                string.IsNullOrEmpty(request.Abstract) ? request.EditorContent : request.Abstract.Trim(),
                _settings.PostAbstractWords,
                _settings.Editor == EditorChoice.Markdown);

            // Address #221: Do not allow published posts back to draft status
            // postModel.IsPublished = request.IsPublished;
            // Edit draft -> save and publish, ignore false case because #221
            bool isNewPublish = false;
            if (request.IsPublished && !post.IsPublished)
            {
                post.IsPublished = true;
                post.PubDateUtc = DateTime.UtcNow;

                isNewPublish = true;
            }

            // #325: Allow changing publish date for published posts
            if (request.PublishDate is not null && post.PubDateUtc.HasValue)
            {
                var tod = post.PubDateUtc.Value.TimeOfDay;
                var adjustedDate = request.PublishDate.Value;
                post.PubDateUtc = adjustedDate.AddTicks(tod.Ticks);
            }

            post.Author = request.Author?.Trim();
            post.Slug = request.Slug.ToLower().Trim();
            post.Title = request.Title;
            post.ExposedToSiteMap = request.ExposedToSiteMap;
            post.LastModifiedUtc = DateTime.UtcNow;
            post.IsFeedIncluded = request.IsFeedIncluded;
            post.ContentLanguageCode = request.ContentLanguageCode;
            post.IsFeatured = request.IsFeatured;
            post.IsOriginal = request.IsOriginal;
            post.OriginLink = string.IsNullOrWhiteSpace(request.OriginLink) ? null : Helper.SterilizeLink(request.OriginLink);
            post.HeroImageUrl = string.IsNullOrWhiteSpace(request.HeroImageUrl) ? null : Helper.SterilizeLink(request.HeroImageUrl);

            // compute hash
            var input = $"{post.Slug}#{post.PubDateUtc.GetValueOrDefault():yyyyMMdd}";
            var checkSum = Helper.ComputeCheckSum(input);
            post.HashCheckSum = checkSum;

            // 1. Add new tags to tag lib
            foreach (var item in request.Tags.Where(item => !_tagRepo.Any(p => p.DisplayName == item)))
            {
                await _tagRepo.AddAsync(new()
                {
                    DisplayName = item,
                    NormalizedName = TagService.NormalizeTagName(item, _tagNormalizationDictionary)
                });

                await _audit.AddEntry(BlogEventType.Content, BlogEventId.TagCreated,
                    $"Tag '{item}' created.");
            }

            // 2. update tags
            post.Tags.Clear();
            if (request.Tags.Any())
            {
                foreach (var tagName in request.Tags)
                {
                    if (!TagService.ValidateTagName(tagName))
                    {
                        continue;
                    }

                    var tag = await _tagRepo.GetAsync(t => t.DisplayName == tagName);
                    if (tag is not null) post.Tags.Add(tag);
                }
            }

            // 3. update categories
            post.PostCategory.Clear();
            if (request.CategoryIds is { Length: > 0 })
            {
                foreach (var cid in request.CategoryIds)
                {
                    post.PostCategory.Add(new()
                    {
                        PostId = post.Id,
                        CategoryId = cid
                    });
                }
            }

            await _postRepo.UpdateAsync(post);

            await _audit.AddEntry(
                BlogEventType.Content,
                isNewPublish ? BlogEventId.PostPublished : BlogEventId.PostUpdated,
                $"Post updated, id: {post.Id}");

            _cache.Remove(CacheDivision.Post, id.ToString());
            return post;
        }

        public async Task RestoreAsync(Guid id)
        {
            var pp = await _postRepo.GetAsync(id);
            if (null == pp) return;

            pp.IsDeleted = false;
            await _postRepo.UpdateAsync(pp);
            await _audit.AddEntry(BlogEventType.Content, BlogEventId.PostRestored, $"Post restored, id: {id}");

            _cache.Remove(CacheDivision.Post, id.ToString());
        }

        public async Task DeleteAsync(Guid id, bool softDelete = false)
        {
            var post = await _postRepo.GetAsync(id);
            if (null == post) return;

            if (softDelete)
            {
                post.IsDeleted = true;
                await _postRepo.UpdateAsync(post);
                await _audit.AddEntry(BlogEventType.Content, BlogEventId.PostRecycled, $"Post '{id}' moved to Recycle Bin.");
            }
            else
            {
                await _postRepo.DeleteAsync(post);
                await _audit.AddEntry(BlogEventType.Content, BlogEventId.PostDeleted, $"Post '{id}' deleted from Recycle Bin.");
            }

            _cache.Remove(CacheDivision.Post, id.ToString());
        }

        public async Task PurgeRecycledAsync()
        {
            var spec = new PostSpec(true);
            var posts = await _postRepo.GetAsync(spec);
            await _postRepo.DeleteAsync(posts);
            await _audit.AddEntry(BlogEventType.Content, BlogEventId.EmptyRecycleBin, "Emptied Recycle Bin.");

            foreach (var guid in posts.Select(p => p.Id))
            {
                _cache.Remove(CacheDivision.Post, guid.ToString());
            }
        }
    }
}
