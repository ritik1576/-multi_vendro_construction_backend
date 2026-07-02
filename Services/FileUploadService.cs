using Amazon.S3;
using Amazon.S3.Model;
using InframartAPI_New.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace InframartAPI_New.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IConfiguration _configuration;

        public FileUploadService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<InframartAPI_New.DTOs.ProductImageUploadResult> UploadProductImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("No file was uploaded.");
            }

            Console.WriteLine($"[Image Upload] Starting upload for file: {file.FileName}");

            var extension = Path.GetExtension(file.FileName).ToLower();
            var contentType = file.ContentType.ToLower();

            if (!MultiVendorAPI.Helpers.ImageOptimizerHelper.IsSupportedFormat(extension, contentType))
            {
                Console.WriteLine($"[Image Upload Error] Unsupported file type: {extension} / {contentType}");
                throw new ArgumentException("Unsupported file type. Only JPEG, PNG, and WEBP images are allowed.");
            }

            // Retrieve R2 configurations
            var accessKey = _configuration["CloudflareR2:AccessKey"];
            var secretKey = _configuration["CloudflareR2:SecretKey"];
            var endpoint = _configuration["CloudflareR2:Endpoint"];
            var bucketName = _configuration["CloudflareR2:BucketName"];

            if (string.IsNullOrWhiteSpace(accessKey) ||
                string.IsNullOrWhiteSpace(secretKey) ||
                string.IsNullOrWhiteSpace(endpoint) ||
                string.IsNullOrWhiteSpace(bucketName))
            {
                throw new InvalidOperationException("Cloudflare R2 configuration is incomplete or missing.");
            }

            byte[] originalWebp;
            byte[] thumbnailWebp;

            try
            {
                using var uploadStream = file.OpenReadStream();
                (originalWebp, thumbnailWebp) = MultiVendorAPI.Helpers.ImageOptimizerHelper.OptimizeImage(uploadStream);
                Console.WriteLine("[Image Optimization Success] Compressed original image and generated thumbnail successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Image Optimization Error] Failed to optimize image: {ex.Message}");
                throw;
            }

            // Generate matching filename: products/original/product-{guid}.webp & products/thumbnails/product-{guid}.webp
            var guid = Guid.NewGuid().ToString();
            var originalKey = $"products/original/product-{guid}.webp";
            var thumbnailKey = $"products/thumbnails/product-{guid}.webp";

            var config = new AmazonS3Config
            {
                ServiceURL = endpoint,
                ForcePathStyle = true,
                AuthenticationRegion = "auto"
            };

            using var client = new AmazonS3Client(accessKey, secretKey, config);

            // Upload Original
            try
            {
                using var originalStream = new MemoryStream(originalWebp);
                var putOriginalRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = originalKey,
                    InputStream = originalStream,
                    ContentType = "image/webp",
                    DisablePayloadSigning = true,
                    DisableDefaultChecksumValidation = true
                };

                var response = await client.PutObjectAsync(putOriginalRequest);
                if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception($"S3 original upload responded with: {response.HttpStatusCode}");
                }
                Console.WriteLine($"[Cloudflare Upload Success] Uploaded original image to R2 key: {originalKey}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cloudflare Upload Error] Failed to upload original image to R2: {ex.Message}");
                throw;
            }

            // Upload Thumbnail
            try
            {
                using var thumbnailStream = new MemoryStream(thumbnailWebp);
                var putThumbnailRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = thumbnailKey,
                    InputStream = thumbnailStream,
                    ContentType = "image/webp",
                    DisablePayloadSigning = true,
                    DisableDefaultChecksumValidation = true
                };

                var response = await client.PutObjectAsync(putThumbnailRequest);
                if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception($"S3 thumbnail upload responded with: {response.HttpStatusCode}");
                }
                Console.WriteLine($"[Cloudflare Upload Success] Uploaded thumbnail image to R2 key: {thumbnailKey}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cloudflare Upload Error] Failed to upload thumbnail image to R2: {ex.Message}");
                throw;
            }

            return new InframartAPI_New.DTOs.ProductImageUploadResult
            {
                OriginalUrl = $"/sys/stream/{originalKey}",
                ThumbnailUrl = $"/sys/stream/{thumbnailKey}"
            };
        }

        public async Task<string> UploadKycDocumentAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("No file was uploaded.");
            }

            // Max size 10 MB
            const long maxFileSize = 10 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                throw new ArgumentException("File size exceeds the 10 MB limit.");
            }

            // Validate document types: PDF, PNG, JPG, JPEG
            var allowedContentTypes = new[] { "application/pdf", "image/jpeg", "image/png", "image/jpg" };
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };

            var contentType = file.ContentType.ToLower();
            var extension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedContentTypes.Contains(contentType) || !allowedExtensions.Contains(extension))
            {
                throw new ArgumentException("Unsupported file type. Only PDF, PNG, JPG, and JPEG files are allowed.");
            }

            // Retrieve R2 configurations
            var accessKey = _configuration["CloudflareR2:AccessKey"];
            var secretKey = _configuration["CloudflareR2:SecretKey"];
            var endpoint = _configuration["CloudflareR2:Endpoint"];
            var bucketName = _configuration["CloudflareR2:BucketName"];

            if (string.IsNullOrWhiteSpace(accessKey) ||
                string.IsNullOrWhiteSpace(secretKey) ||
                string.IsNullOrWhiteSpace(endpoint) ||
                string.IsNullOrWhiteSpace(bucketName))
            {
                throw new InvalidOperationException("Cloudflare R2 configuration is incomplete or missing.");
            }

            // Generate unique filename: kyc/{guid}.{extension}
            var cleanExtension = extension.TrimStart('.');
            var uniqueFileName = $"kyc/{Guid.NewGuid()}.{cleanExtension}";

            var config = new AmazonS3Config
            {
                ServiceURL = endpoint,
                ForcePathStyle = true,
                AuthenticationRegion = "auto"
            };

            using var client = new AmazonS3Client(accessKey, secretKey, config);
            using var stream = file.OpenReadStream();

            var putRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = uniqueFileName,
                InputStream = stream,
                ContentType = contentType,
                DisablePayloadSigning = true,
                DisableDefaultChecksumValidation = true
            };

            var response = await client.PutObjectAsync(putRequest);

            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"Failed to upload KYC document to Cloudflare R2. S3 Response status: {response.HttpStatusCode}");
            }

            // Construct return URL using the proxy stream path
            return $"/sys/stream/{uniqueFileName}";
        }
    }
}
