using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using ImageProcessor;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.Formats;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;

namespace Cogworks.UmbracoFlare.Core.FileSystemPickerControllers
{
    [PluginController("FileSystemPicker")]
    public class FileSystemThumbnailApiController : UmbracoAuthorizedApiController
    {
        public HttpResponseMessage GetThumbnail(string imagePath, int width)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || imagePath.IndexOf("{{") >= 0) { return null; }

            var image =  Image.FromFile(System.Web.Hosting.HostingEnvironment.MapPath(imagePath));
            var outStream = new MemoryStream();
            var photoBytes = File.ReadAllBytes(System.Web.Hosting.HostingEnvironment.MapPath(imagePath)); // change imagePath with a valid image path

            ISupportedImageFormat format = new JpegFormat { Quality = 70 }; // convert to jpg
                
            var inStream = new MemoryStream(photoBytes);
            var imageFactory = new ImageFactory(true);
            var size = ResizeKeepAspect(image.Size, width, width);

            var resizeLayer = new ResizeLayer(size, ResizeMode.Max);

            imageFactory.Load(inStream)
                .Resize(resizeLayer)
                .Format(format)
                .Save(outStream);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(outStream)
            };

            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");

            return response;

        }

        public static Size ResizeKeepAspect(Size currentDimensions, int maxWidth, int maxHeight)
        {
            var newHeight = currentDimensions.Height;
            var newWidth = currentDimensions.Width;

            if (maxWidth > 0 && newWidth > maxWidth)
            {
                var divider = Math.Abs((decimal)newWidth / maxWidth);
                newWidth = maxWidth;

                newHeight = (int)Math.Round(newHeight / divider);
            }

            if (maxHeight > 0 && newHeight > maxHeight)
            {
                var divider = Math.Abs((decimal)newHeight / maxHeight);
                newHeight = maxHeight;

                newWidth = (int)Math.Round(newWidth / divider);
            }

            return new Size(newWidth, newHeight);
        }
    }
}