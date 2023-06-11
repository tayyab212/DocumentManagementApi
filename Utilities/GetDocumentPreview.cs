using DocumentFormat.OpenXml.Packaging;
using ImageProcessor;
using OfficeOpenXml;
using OfficeOpenXml.Drawing;
using Patagames.Pdf.Net;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Http.Headers;

namespace DocumentManagementApi.Utilities
{
    public static class GetDocumentPreview
    {

        public static byte[] GetPdfPreview(string filePath)
        {
            // Initialize the SDK library
            // You have to call this function before you can call any PDF processing functions.
            PdfCommon.Initialize();

            // Open and load the PDF document from the file
            using (var doc = PdfDocument.Load(filePath))
            {
                // Get the first page of the document
                var firstPage = doc.Pages[0];

                // Extract the preview image
                var previewImageObject = firstPage.PageObjects.FirstOrDefault(obj => obj is PdfImageObject) as PdfImageObject;

                if (previewImageObject != null)
                {
                    // Convert the image to byte array
                    using (var stream = new MemoryStream())
                    {
                        previewImageObject.Bitmap.Image.Save(stream, ImageFormat.Png);
                        return stream.ToArray();
                    }
                }
            }

            return null;
        }


        public static byte[] GetWordPreview(string filePath)
        {
            try
            {
                // Load the Word document using Open XML SDK
                using (var document = WordprocessingDocument.Open(filePath, false))
                {
                    var imagePart = document.MainDocumentPart?.ImageParts?.FirstOrDefault();
                    if (imagePart != null)
                    { 
                        // Extract the image from the Word document
                        using (var stream = imagePart.GetStream())
                        {
                            var image = Image.FromStream(stream);

                            // Create a thumbnail of the image
                            var thumbnail = image.GetThumbnailImage(200, 200, null, IntPtr.Zero);

                            // Convert the thumbnail to a byte array
                            var thumbnailBytes = ImageToByteArray(thumbnail);
                            return thumbnailBytes;
                        }
                    }
                }
            }
            catch (Exception ex)
            {


            }

            return null;

        }

        private static byte[] ImageToByteArray(Image image)
        {
            using (var stream = new MemoryStream())
            {
                image.Save(stream, ImageFormat.Jpeg);
                return stream.ToArray();
            }
        }

        private static byte[]  GetTextPreview(string filePath)
        {
            int width = 200;
            int height = 200;

            // Load the text file contents
            string text = System.IO.File.ReadAllText(filePath);

            // Create a new Bitmap with the specified width and height
            Bitmap bitmap = new Bitmap(width, height);

            // Create a Graphics object from the bitmap
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                // Set the background color
                graphics.Clear(Color.White);

                // Set the font and text color
                Font font = new Font("Arial", 12);
                Brush brush = Brushes.Black;

                // Draw the text on the bitmap
                graphics.DrawString(text, font, brush, new RectangleF(0, 0, width, height));
            }

            // Convert the bitmap to a byte array
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }

        public static byte[] GetImagePreview(string filePath)
        {
            int thumbnailWidth = 50;
            int thumbnailHeight = 50;

            using (Image originalImage = Image.FromFile(filePath))
            {
                // Create a thumbnail image with the specified width and height
                using (Image thumbnailImage = originalImage.GetThumbnailImage(thumbnailWidth, thumbnailHeight, null, IntPtr.Zero))
                {
                    using (MemoryStream stream = new MemoryStream())
                    {
                        thumbnailImage.Save(stream, originalImage.RawFormat);
                        return stream.ToArray();
                    }
                }
            }
        }

        public static byte[] ProcessDocument(string filePath)
        {
            string extension = Path.GetExtension(filePath);
              
            switch (extension.ToLower())
            {
                case ".pdf":
                    return GetPdfPreview(filePath);
                case ".docx":
                case ".doc":
                    return GetWordPreview(filePath);
                case ".txt":
                    return GetTextPreview(filePath);
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".gif":
                    return GetImagePreview(filePath);
                default:
                    return null;
            }
        }

    }
}
