﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Core;
using Moonglade.Web.Filters;
using Moonglade.Web.Models;
using System;
using System.Threading.Tasks;

namespace Moonglade.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatisticsController : ControllerBase
    {
        private readonly IBlogStatistics _statistics;
        private bool DNT => (bool)HttpContext.Items["DNT"];

        public StatisticsController(IBlogStatistics statistics)
        {
            _statistics = statistics;
        }

        [HttpGet("{postId:guid}")]
        [ProducesResponseType(typeof(Tuple<int, int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Get([NotEmpty] Guid postId)
        {
            var (hits, likes) = await _statistics.GetStatisticAsync(postId);
            return Ok(new { Hits = hits, Likes = likes });
        }

        [HttpPost]
        [DisallowSpiderUA]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Post(StatisticsRequest request)
        {
            if (DNT) return NoContent();

            await _statistics.UpdateStatisticAsync(request.PostId, request.IsLike);
            return NoContent();
        }
    }

    public class StatisticsRequest
    {
        [NotEmpty]
        public Guid PostId { get; set; }

        public bool IsLike { get; set; }
    }
}
