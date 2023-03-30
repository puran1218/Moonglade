﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Data.Porting;
using Moonglade.Utils;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DataPortingController : ControllerBase
    {
        private readonly IExportManager _expman;

        public DataPortingController(IExportManager expman)
        {
            _expman = expman;
        }

        [HttpGet("export/{type}")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ExportDownload(ExportDataType type, CancellationToken cancellationToken)
        {
            var exportResult = await _expman.ExportData(type, cancellationToken);
            switch (exportResult.ExportFormat)
            {
                case ExportFormat.SingleJsonFile:
                    return new FileContentResult(exportResult.Content, exportResult.ContentType)
                    {
                        FileDownloadName = $"moonglade-{type.ToString().ToLowerInvariant()}-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.json"
                    };
                case ExportFormat.SingleCSVFile:
                    Response.Headers.Add("Content-Disposition", $"attachment;filename={Path.GetFileName(exportResult.FilePath)}");
                    return PhysicalFile(exportResult.FilePath, exportResult.ContentType, Path.GetFileName(exportResult.FilePath));
                case ExportFormat.ZippedJsonFiles:
                    return PhysicalFile(exportResult.FilePath, exportResult.ContentType, Path.GetFileName(exportResult.FilePath));
                default:
                    return BadRequest(ModelState.CombineErrorMessages());
            }
        }
    }
}
