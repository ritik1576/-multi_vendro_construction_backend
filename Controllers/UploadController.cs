using InframartAPI_New.DTOs;
using InframartAPI_New.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace InframartAPI_New.Controllers
{
    [ApiController]
    [Route("upload")]
    public class UploadController : ControllerBase
    {
        private readonly IFileUploadService _fileUploadService;

        public UploadController(IFileUploadService fileUploadService)
        {
            _fileUploadService = fileUploadService;
        }

        [HttpPost("product-image")]
        public async Task<IActionResult> UploadProductImage(IFormFile file)
        {
            if (file == null)
            {
                return BadRequest(new { message = "No file was uploaded." });
            }

            try
            {
                var fileUrl = await _fileUploadService.UploadProductImageAsync(file);
                var response = new UploadResponseDto
                {
                    Url = fileUrl
                };
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Incomplete/missing configuration
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Upload or client execution failure
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"An error occurred during file upload: {ex.Message}" });
            }
        }
    }
}
