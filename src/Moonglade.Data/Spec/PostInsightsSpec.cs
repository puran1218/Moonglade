﻿using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using System;
using System.Linq;

namespace Moonglade.Data.Spec
{
    public sealed class PostInsightsSpec : BaseSpecification<PostEntity>
    {
        public PostInsightsSpec(PostInsightsType insightsType, int top) :
            base(p => !p.IsDeleted
                      && p.IsPublished
                      && p.PubDateUtc >= DateTime.UtcNow.AddYears(-1))
        {
            switch (insightsType)
            {
                case PostInsightsType.TopRead:
                    ApplyOrderByDescending(p => p.PostExtension.Hits);
                    break;
                case PostInsightsType.TopCommented:
                    AddCriteria(p => p.Comments.Any(c => c.IsApproved));
                    ApplyOrderByDescending(p => p.Comments.Count);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(insightsType), insightsType, null);
            }

            ApplyPaging(0, top);
        }
    }

    public enum PostInsightsType
    {
        TopRead = 0,
        TopCommented = 1
    }
}