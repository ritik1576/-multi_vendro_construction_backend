using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MultiVendorAPI.Data;
using MultiVendorAPI.Models;
using InframartAPI_New.Models;
using System.Security.Claims;
using System.Text.Json;

namespace InframartAPI_New.Middlewares
{
    public class ImageProxyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly CloudflareR2Settings _r2Settings;
        private readonly IAmazonS3 _s3Client;

        public ImageProxyMiddleware(RequestDelegate next, IOptions<CloudflareR2Settings> r2Settings)
        {
            _next = next;
            _r2Settings = r2Settings.Value;

            var config = new AmazonS3Config
            {
                ServiceURL = $"https://{_r2Settings.AccountId}.r2.cloudflarestorage.com",
                ForcePathStyle = true
            };
            _s3Client = new AmazonS3Client(_r2Settings.AccessKeyId, _r2Settings.SecretAccessKey, config);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            if (path.Equals("/sys/upload", StringComparison.OrdinalIgnoreCase))
            {
                await HandleUploadAsync(context);
                return;
            }
            else if (path.StartsWith("/sys/stream/", StringComparison.OrdinalIgnoreCase))
            {
                await HandleStreamAsync(context, path.Substring("/sys/stream/".Length));
                return;
            }

            await _next(context);
        }

        private async Task HandleUploadAsync(HttpContext context)
        {
            context.Response.ContentType = "application/json";

            // 1. Authentication & Authorization Check
            if (context.User.Identity?.IsAuthenticated != true)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { success = false, message = "Authentication required." });
                return;
            }

            var vendorIdClaim = context.User.FindFirst("vendorId")?.Value;
            var roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value;

            if (roleClaim != "vendor" || string.IsNullOrEmpty(vendorIdClaim) || !long.TryParse(vendorIdClaim, out var vendorId))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { success = false, message = "Only authorized vendors can upload." });
                return;
            }

            // 2. Validate Multipart Form
            if (!context.Request.HasFormContentType)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { success = false, message = "Invalid form content type." });
                return;
            }

            var form = await context.Request.ReadFormAsync();
            var file = form.Files["file"];

            if (file == null || file.Length == 0)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { success = false, message = "File is missing or empty." });
                return;
            }

            // 3. Size Validation (Max 5MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { success = false, message = "File size exceeds 5 MB limit." });
                return;
            }

            // 4. Extension & MIME Validation
            var extension = Path.GetExtension(file.FileName).TrimStart('.').ToLower();
            var allowedExtensions = new[] { "jpg", "jpeg", "png", "webp" };
            if (!allowedExtensions.Contains(extension))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { success = false, message = "Unsupported file type extension." });
                return;
            }

            var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedMimeTypes.Contains(file.ContentType.ToLower()))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { success = false, message = "Unsupported MIME type." });
                return;
            }

            // 5. Upload to Cloudflare R2
            var guid = Guid.NewGuid().ToString();
            var storageKey = $"products/{vendorId}/{guid}.{extension}";

            try
            {
                using (var stream = file.OpenReadStream())
                {
                    var putRequest = new PutObjectRequest
                    {
                        BucketName = _r2Settings.BucketName,
                        Key = storageKey,
                        InputStream = stream,
                        ContentType = file.ContentType
                    };

                    await _s3Client.PutObjectAsync(putRequest);
                }

                // 6. Save Metadata to DB
                var db = context.RequestServices.GetRequiredService<ApplicationDbContext>();
                var imageFile = new ImageFile
                {
                    StorageKey = storageKey,
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    VendorId = vendorId,
                    CreatedAt = DateTime.UtcNow
                };

                db.ImageFiles.Add(imageFile);
                await db.SaveChangesAsync();

                context.Response.StatusCode = StatusCodes.Status200OK;
                await context.Response.WriteAsJsonAsync(new
                {
                    success = true,
                    key = storageKey,
                    imageUrl = $"/sys/stream/{storageKey}"
                });
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { success = false, message = $"Upload failed: {ex.Message}" });
            }
        }

        private async Task HandleStreamAsync(HttpContext context, string key)
        {
            // Path Traversal Mitigation
            if (string.IsNullOrEmpty(key) || key.Contains("..") || key.Contains("\\") || key.Contains(":") || key.Contains("//"))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { success = false, message = "Invalid characters in path key." });
                return;
            }

            try
            {
                var getRequest = new GetObjectRequest
                {
                    BucketName = _r2Settings.BucketName,
                    Key = key
                };

                using (var response = await _s3Client.GetObjectAsync(getRequest))
                {
                    context.Response.ContentType = response.Headers.ContentType;
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    
                    // Stream directly to response body to avoid loading file in memory
                    await response.ResponseStream.CopyToAsync(context.Response.Body, context.RequestAborted);
                }
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { success = false, message = "File not found." });
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { success = false, message = $"Stream failed: {ex.Message}" });
            }
        }
    }
}
