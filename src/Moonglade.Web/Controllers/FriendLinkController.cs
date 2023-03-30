using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.FriendLink;
using Moonglade.Web.Models;
using System;
using System.Threading.Tasks;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FriendLinkController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FriendLinkController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IActionResult> Create(FriendLinkEditModel model)
        {
            await _mediator.Send(new AddLinkCommand(model));
            return Created(string.Empty, model);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(Link), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get([NotEmpty] Guid id)
        {
            var link = await _mediator.Send(new GetLinkQuery(id));
            if (null == link) return NotFound();

            return Ok(link);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Update([NotEmpty] Guid id, FriendLinkEditModel model)
        {
            await _mediator.Send(new UpdateLinkCommand(id, model));
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete([NotEmpty] Guid id)
        {
            await _mediator.Send(new DeleteLinkCommand(id));
            return NoContent();
        }
    }
}
