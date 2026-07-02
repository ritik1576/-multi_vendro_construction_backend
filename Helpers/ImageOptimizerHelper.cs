using System;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace MultiVendorAPI.Helpers
{
    public static class ImageOptimizerHelper
    {
        public static bool IsSupportedFormat(string extension, string contentType)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/jpg" };

            return allowedExtensions.Contains(extension.ToLower()) && allowedMimeTypes.Contains(contentType.ToLower());
        }

        public static (byte[] originalWebp, byte[] thumbnailWebp) OptimizeImage(Stream inputStream)
        {
            // Rewind input stream just in case
            if (inputStream.CanSeek)
            {
                inputStream.Position = 0;
            }

            using var image = Image.Load(inputStream);

            // Strip metadata
            image.Metadata.ExifProfile = null;
            image.Metadata.IptcProfile = null;
            image.Metadata.XmpProfile = null;

            // Clone image for original version to avoid modifying same instance
            using var originalImage = image.Clone(x => { });
            
            // Resize original if width exceeds 1920px
            if (originalImage.Width > 1920)
            {
                int newWidth = 1920;
                int newHeight = (int)Math.Round((double)originalImage.Height * newWidth / originalImage.Width);
                originalImage.Mutate(x => x.Resize(newWidth, newHeight));
            }

            using var originalStream = new MemoryStream();
            var originalEncoder = new WebpEncoder { Quality = 80 };
            originalImage.Save(originalStream, originalEncoder);
            var originalBytes = originalStream.ToArray();

            // Create thumbnail from original image clone
            using var thumbnailImage = image.Clone(x => { });

            // Resize thumbnail to 400x400 maintaining aspect ratio
            int maxThumbDimension = 400;
            int thumbWidth, thumbHeight;
            if (thumbnailImage.Width > thumbnailImage.Height)
            {
                thumbWidth = maxThumbDimension;
                thumbHeight = (int)Math.Round((double)thumbnailImage.Height * maxThumbDimension / thumbnailImage.Width);
            }
            else
            {
                thumbHeight = maxThumbDimension;
                thumbWidth = (int)Math.Round((double)thumbnailImage.Width * maxThumbDimension / thumbnailImage.Height);
            }

            thumbnailImage.Mutate(x => x.Resize(thumbWidth, thumbHeight));

            using var thumbnailStream = new MemoryStream();
            var thumbnailEncoder = new WebpEncoder { Quality = 70 };
            thumbnailImage.Save(thumbnailStream, thumbnailEncoder);
            var thumbnailBytes = thumbnailStream.ToArray();

            return (originalBytes, thumbnailBytes);
        }
    }
}
