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

        public async Task<string> UploadProductImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("No file was uploaded.");
            }

            // Validate image types
            var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/jpg" };
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

            var contentType = file.ContentType.ToLower();
            var extension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedContentTypes.Contains(contentType) || !allowedExtensions.Contains(extension))
            {
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

            // Generate unique filename: products/{guid}.{extension}
            var cleanExtension = extension.TrimStart('.');
            var uniqueFileName = $"products/{Guid.NewGuid()}.{cleanExtension}";

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
                throw new Exception($"Failed to upload image to Cloudflare R2. S3 Response status: {response.HttpStatusCode}");
            }

            // Construct return URL using the proxy stream path
            return $"/sys/stream/{uniqueFileName}";
        }
    }
}
