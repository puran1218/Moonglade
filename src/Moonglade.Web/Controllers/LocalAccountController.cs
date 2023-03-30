﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Auth;
using Moonglade.Web.Models;
using Moonglade.Web.Models.Settings;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class LocalAccountController : ControllerBase
    {
        private readonly ILocalAccountService _accountService;

        public LocalAccountController(ILocalAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(AccountEditModel model)
        {
            if (_accountService.Exist(model.Username))
            {
                ModelState.AddModelError("username", $"User '{model.Username}' already exist.");
                return Conflict(ModelState);
            }

            await _accountService.CreateAsync(model.Username, model.Password);
            return Ok();
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Delete([NotEmpty] Guid id)
        {
            var uidClaim = User.Claims.FirstOrDefault(c => c.Type == "uid");
            if (null == uidClaim || string.IsNullOrWhiteSpace(uidClaim.Value))
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Can not get current uid.");
            }

            if (id.ToString() == uidClaim.Value)
            {
                return Conflict("Can not delete current user.");
            }

            var count = _accountService.Count();
            if (count == 1)
            {
                return Conflict("Can not delete last account.");
            }

            await _accountService.DeleteAsync(id);
            return NoContent();
        }

        [HttpPut("{id:guid}/password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ResetPassword([NotEmpty] Guid id, [FromBody][Required] string newPassword)
        {
            if (!Regex.IsMatch(newPassword, @"^(?=.*[A-Za-z])(?=.*\d)[!@#$%^&*A-Za-z\d]{8,}$"))
            {
                return Conflict("Password must be minimum eight characters, at least one letter and one number");
            }

            await _accountService.UpdatePasswordAsync(id, newPassword);
            return NoContent();
        }
    }
}
