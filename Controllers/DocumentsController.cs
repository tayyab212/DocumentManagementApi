using DocumentManagementApi.DAL;
using DocumentManagementApi.Models;
using DocumentManagementApi.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;

namespace DocumentManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentsController : ControllerBase
    {

        static string _documentsDirectory = "DocumentVault";
        private readonly DatabaseHelper _databaseHelper;
        string _wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), _documentsDirectory);
        public DocumentsController(IWebHostEnvironment environment, IConfiguration configuration)
        {
            string connectionString = configuration.GetConnectionString("YourConnectionString");
            _databaseHelper = new DatabaseHelper(configuration);
        }


        #region GenerateLink



        [HttpPost]
        [Route("GenerateLink")]
        public async Task<ActionResult<string>> GenerateLinkAsync([FromBody] string documentId)
        {
            // Retrieve the document file path based on the documentId
            string filePath = Path.Combine(_documentsDirectory, documentId);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Document not found.");
            }

            // Generate a unique token for the document
            string uniqueToken = Guid.NewGuid().ToString();

            await _databaseHelper.StoreTokenInDatabaseAsync(uniqueToken, documentId);

            // Generate the public link using the unique token
            string publicLink = $"{Request.Scheme}://{Request.Host}/api/documents/public/{uniqueToken}";

            return Ok(publicLink);
        }

        [HttpGet("Public/{token}")]
        public IActionResult GetPublicDocument(string token)
        {

            (bool isValid, string documentId) = _databaseHelper.CheckTokenValidity(token);

            // Retrieve the document metadata based on the token from the dictionary
            if (isValid)
            {
                string absoluteFilePath = Path.Combine(_wwwrootPath, documentId);
                // Return the document file for download or viewing
                return PhysicalFile(absoluteFilePath, GetContentType(System.IO.Path.GetExtension(documentId)));
            }

            return NotFound();
        }

        #endregion

        [HttpPost]
        [Route("UploadDocumentAsync")]
        public async Task<IActionResult> UploadDocumentAsync(List<IFormFile> files)
        {
            try
            {
                if (files == null || files.Count == 0)
                {
                    return BadRequest("No files selected.");
                }



                // Ensure the uploads directory exists
                Directory.CreateDirectory(_wwwrootPath);

                foreach (var file in files)
                {
                    if (file.Length == 0)
                    {
                        return BadRequest($"File {file.FileName} is empty.");
                    }

                    // Generate a unique file name or use the original file name
                    string fileName = $"{file.FileName}_{DateTime.Now.Ticks}_{Path.GetExtension(file.FileName)}";
                    string documentId = Path.GetFileNameWithoutExtension(fileName);
                    // Combine the wwwroot path with the desired file path
                    string filePath = Path.Combine(_wwwrootPath, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    // Create a DocumentDto object to store in the database
                    var document = new DocumentDto
                    {
                        DocumentId = fileName,
                        Name = file.FileName,
                        Icon = "icon.png",  // Set the appropriate icon path or name
                        ContentPreviewImage = null,  // Set the appropriate content preview image path or name
                        UploadDateTime = DateTime.Now.ToString(),
                        DownloadCount = 0
                    };
                    await _databaseHelper.InsertDocumentAsync(document);
                    // Optionally, you can perform additional processing or save the file information to a database
                }

                return Ok("Files uploaded successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while uploading the files: {ex.Message}");
            }
        }

        // GET api/documents
        [HttpGet("GetDocuments")]
        public ActionResult<IEnumerable<DocumentDto>> GetDocuments()
        {
            // Get the list of document files from the project directory
          
            List<string> documentFiles = GetDocumentFiles(_wwwrootPath);

            // Map the document files to DTOs (Data Transfer Objects) for response
            List<DocumentDto> documentDtos = MapDocumentFilesToDtos(documentFiles);

            return Ok(documentDtos);
        }

        [HttpGet("DownloadDocument/{documentId}")]
        public IActionResult DownloadDocument(string documentId)
        {
            // Get the document file path based on the documentId
            string filePath = GetDocumentFilePath(documentId);

            if (System.IO.File.Exists(filePath))
            {
                // Increment the download count
                IncrementDownloadCount(documentId);

                // Read the file contents
                byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);

                // Set the file content type based on the file extension
                string contentType = GetContentType(System.IO.Path.GetExtension(filePath));

                // Return the file as a response with appropriate headers
                return File(fileBytes, contentType, System.IO.Path.GetFileName(filePath));
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost]
        [Route("DownloadSelectedDocuments")]

        public async Task<IActionResult> DownloadSelectedDocuments([FromBody] List<string> documentIds)
        {
            // Implement the logic to download selected documents based on the provided documentIds
            if (documentIds == null || documentIds.Count == 0)
            {
                // If no documentIds are provided, return a BadRequest response
                return BadRequest("No documentIds specified.");
            }

            // Create a new in-memory ZIP archive
            using (var memoryStream = new MemoryStream())
            {
                using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var documentId in documentIds)
                    {
                        // Retrieve the document file path based on the documentId
                        string filePath = GetDocumentFilePath(documentId);

                        if (!string.IsNullOrEmpty(filePath))
                        {
                            // Add the document file to the ZIP archive
                            zipArchive.CreateEntryFromFile(filePath, Path.GetFileName(filePath));
                            // Increment the download count
                            IncrementDownloadCount(documentId);
                        }
                    }
                }

                // Set the position of the memory stream to the beginning
                memoryStream.Position = 0;

                // Perform the download operation for the ZIP archive file
                return File(memoryStream.ToArray(), "application/octet-stream", "documents.zip");
            }
        }


        private int GetDownloadCount(string documentId)
        {
            // Retrieve the download count from the database using the DatabaseHelper class
            int downloadCount = _databaseHelper.GetDownloadCountFromDatabase(documentId);
            return downloadCount;
        }

        private string GetDocumentFilePath(string documentId)
        {
            // Logic to retrieve the file path based on the documentId
            // Replace this with your own implementation
            string documentsPath = Path.Combine(Directory.GetCurrentDirectory(), _documentsDirectory);
            string filePath = Path.Combine(documentsPath, documentId);
            return filePath;
        }

        private void IncrementDownloadCount(string documentId)
        {
            _databaseHelper.UpdateDownloadCountInDatabase(documentId);
        }

        private string GetContentType(string fileExtension)
        {
            // Map file extensions to content types
            switch (fileExtension.ToLower())
            {
                case ".pdf":
                    return "application/pdf";
                case ".xlsx":
                case ".xls":
                    return "application/vnd.ms-excel";
                case ".docx":
                case ".doc":
                    return "application/msword";
                case ".txt":
                    return "text/plain";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                default:
                    return "application/octet-stream";
            }
        }

        // Method to get the list of document files from the directory
        private List<string> GetDocumentFiles(string documentsPath)
        {
            if (!Directory.Exists(documentsPath))
                return new List<string>();

            string[] files = Directory.GetFiles(documentsPath);
            List<string> documentFiles = new List<string>(files);

            return documentFiles;
        }

        // Method to map document files to DTOs
        private List<DocumentDto> MapDocumentFilesToDtos(List<string> documentFiles)
        {
            List<DocumentDto> documentDtos = new List<DocumentDto>();
            foreach (var file in documentFiles)
            {
                string filePath = Path.Combine(_documentsDirectory, file);

                DocumentDto documentDto = new DocumentDto
                {
                    DocumentId = Path.GetFileName(file),
                    Name = Path.GetFileName(file),
                    Icon = GetIconByDocumentType(Path.GetExtension(file)),
                    type = Path.GetExtension(file),
                    ContentPreviewImage = GetDocumentPreview.ProcessDocument(file),
                    UploadDateTime = System.IO.File.GetCreationTimeUtc(filePath).ToString(),
                    DownloadCount = GetDownloadCount(Path.GetFileName(file))
                };

                documentDtos.Add(documentDto);
            }

            return documentDtos;
        }

        // Method to get the icon based on file extension
        private string GetIconByDocumentType(string fileExtension)
        {
            // Replace this with your code to determine the icon based on the document type
            // You might use a switch statement or a dictionary to map the file extensions to icons

            // Dummy implementation
            switch (fileExtension.ToLower())
            {
                case ".pdf":
                    return "pdf-icon.png";
                case ".doc":
                case ".docx":
                    return "word-icon.png";
                case ".xls":
                case ".xlsx":
                    return "excel-icon.png";
                case ".txt":
                    return "txt-icon.png";
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".gif":
                    return "image-icon.png";
                default:
                    return "default-icon.png";
            }

        }

        // Method to get the content preview image (1st page) of a document (replace with your implementation)
       

    }

}
