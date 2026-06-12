using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MultiVendorAPI.Data;
using MultiVendorAPI.Models;
using InframartAPI_New.Models;
using System.Security.Claims;
using System.Text.Json;
using System.Net;

namespace InframartAPI_New.Middlewares
{
    public class ImageProxyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly CloudflareR2Settings _r2Settings;
        private readonly IAmazonS3 _s3Client;
        private readonly IConfiguration _configuration;

        public ImageProxyMiddleware(RequestDelegate next, IOptions<CloudflareR2Settings> r2Settings, IConfiguration configuration)
        {
            _next = next;
            _r2Settings = r2Settings.Value;
            _configuration = configuration;

            // FIX: Removed ForcePathStyle = true which breaks Cloudflare R2 structure.
            // ADDED: DisableDefaultChecksumValidation to comply with Cloudflare MD5 rules.
            var config = new AmazonS3Config
            {
                ServiceURL = $"https://{_r2Settings.AccountId}.r2.cloudflarestorage.com",
                DisableDefaultChecksumValidation = true
            };

            _s3Client = new AmazonS3Client(_r2Settings.AccessKeyId, _r2Settings.SecretAccessKey, config);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // Manual token extraction layer (handles casing anomalies safely)
            if (context.User.Identity?.IsAuthenticated != true)
            {
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader))
                {
                    string token = string.Empty;
                    if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        token = authHeader.Substring("Bearer ".Length).Trim();
                    }
                    else if (authHeader.StartsWith("bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        token = authHeader.Substring("bearer ".Length).Trim();
                    }

                    if (!string.IsNullOrEmpty(token))
                    {
                        try
                        {
                            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                            var key = System.Text.Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);
                            tokenHandler.ValidateToken(token, new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                            {
                                ValidateIssuer = false,
                                ValidateAudience = false,
                                ValidateLifetime = true,
                                ValidateIssuerSigningKey = true,
                                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                                ClockSkew = TimeSpan.Zero
                            }, out Microsoft.IdentityModel.Tokens.SecurityToken validatedToken);

                            var jwtToken = (System.IdentityModel.Tokens.Jwt.JwtSecurityToken)validatedToken;
                            var identity = new ClaimsIdentity(jwtToken.Claims, "ManualJwt");
                            context.User = new ClaimsPrincipal(identity);
                        }
                        catch
                        {
                            // Quietly swallow authentication exceptions to let request drop to unauthorized logic below
                        }
                    }
                }
            }

            // Route matching checks
            if (path.Equals("/sys/upload", StringComparison.OrdinalIgnoreCase))
            {
                await HandleUploadAsync(context);
                return;
            }
            // FIX: Added dynamic safety check ensuring keys are split properly from trailing slashes
            else if (path.StartsWith("/sys/stream/", StringComparison.OrdinalIgnoreCase) && path.Length > "/sys/stream/".Length)
            {
                await HandleStreamAsync(context, path.Substring("/sys/stream/".Length));
                return;
            }

            await _next(context);
        }

        private async Task HandleUploadAsync(HttpContext context)
        {
            context.Response.ContentType = "application/json";

            // 1. Authentication & Security Policy Checks
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

            // 2. Validate Multipart Form Details
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

            // 3. Size Limits Validation (Max 5MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { success = false, message = "File size exceeds 5 MB limit." });
                return;
            }

            // 4. File Format Protections
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

            // 5. Stream Transport straight to Cloudflare R2 Node
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
                        ContentType = file.ContentType,
                        DisablePayloadSigning = true // CRITICAL COMPATIBILITY FLAG FOR R2
                    };

                    await _s3Client.PutObjectAsync(putRequest);
                }

                // 6. DB Metadata Sync
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
            catch (AmazonS3Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { success = false, message = $"Cloudflare S3 Error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { success = false, message = $"Upload handler system failure: {ex.Message}" });
            }
        }

        private async Task HandleStreamAsync(HttpContext context, string key)
        {
            // Structural path cleaning against injection strategies
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

                    // Directly pipes bytes downstream to the client interface connection
                    await response.ResponseStream.CopyToAsync(context.Response.Body, context.RequestAborted);
                }
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { success = false, message = "File not found inside R2 container." });
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { success = false, message = $"Stream system failure: {ex.Message}" });
            }
        }
    }
}