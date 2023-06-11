using DocumentManagementApi.DAL;
using DocumentManagementApi.Models;
using DocumentManagementApi.Utilities;
using System.Data;
using System.Data.SqlClient;
using System.IO.Compression;

namespace DocumentManagementApi.Repository
{

    public interface IDocumentRepository
    {
        List<DocumentDto> GetDocuments();
        DocumentDto GetDocument(string documentId);
        byte[] GetDocumentFile(string documentId);
        byte[] DownloadSelectedDocuments(List<string> documentIds);
        Task<List<DocumentDto>> UploadDocumentsAsync(List<IFormFile> files);
        Task<string> GenerateDocumentToken(string documentId);
        (bool, string) CheckDocumentToken(string token);
    }
    public class DocumentRepository : IDocumentRepository
    {
        private readonly DatabaseHelper _databaseHelper;
        private readonly string _documentsDirectory;
        private readonly string _wwwrootPath;

        public DocumentRepository(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _documentsDirectory = "DocumentVault";
            string connectionString = configuration.GetConnectionString("YourConnectionString");
            _databaseHelper = new DatabaseHelper(configuration);
            _wwwrootPath = Path.Combine(environment.ContentRootPath, _documentsDirectory);
        }

        public List<DocumentDto> GetDocuments()
        {
            List<string> documentFiles = GetDocumentFiles(_wwwrootPath);
            List<DocumentDto> documentDtos = MapDocumentFilesToDtos(documentFiles);
            return documentDtos;
        }

        public DocumentDto GetDocument(string documentId)
        {
            string filePath = GetDocumentFilePath(documentId);

            if (System.IO.File.Exists(filePath))
            {
                string fileName = Path.GetFileName(filePath);
                string uploadDateTime = System.IO.File.GetCreationTimeUtc(filePath).ToString();
                int downloadCount = GetDownloadCount(fileName);

                DocumentDto document = new DocumentDto
                {
                    DocumentId = fileName,
                    Name = fileName,
                    Icon = GetIconByDocumentType(Path.GetExtension(fileName)),
                    ContentPreviewImage = GetDocumentPreview.ProcessDocument(filePath),
                    UploadDateTime = uploadDateTime,
                    DownloadCount = downloadCount
                };

                return document;
            }

            return null;
        }

        public byte[] GetDocumentFile(string documentId)
        {
            string filePath = GetDocumentFilePath(documentId);

            if (System.IO.File.Exists(filePath))
            {
                byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
                return fileBytes;
            }

            return null;
        }

        public byte[] DownloadSelectedDocuments(List<string> documentIds)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var documentId in documentIds)
                    {
                        string filePath = GetDocumentFilePath(documentId);

                        if (!string.IsNullOrEmpty(filePath))
                        {
                            zipArchive.CreateEntryFromFile(filePath, Path.GetFileName(filePath));
                            IncrementDownloadCount(documentId);
                        }
                    }
                }

                memoryStream.Position = 0;
                return memoryStream.ToArray();
            }
        }

        public async Task<List<DocumentDto>> UploadDocumentsAsync(List<IFormFile> files)
        {
            List<DocumentDto> uploadedDocuments = new List<DocumentDto>();

            foreach (var file in files)
            {
                if (file.Length == 0)
                {
                    continue;
                }

                string fileName = $"{file.FileName}_{DateTime.Now.Ticks}_{Path.GetExtension(file.FileName)}";
                string documentId = Path.GetFileNameWithoutExtension(fileName);
                string filePath = Path.Combine(_wwwrootPath, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                var document = new DocumentDto
                {
                    DocumentId = fileName,
                    Name = file.FileName,
                    Icon = "icon.png",
                    ContentPreviewImage = null,
                    UploadDateTime = DateTime.Now.ToString(),
                    DownloadCount = 0
                };

                await _databaseHelper.InsertDocumentAsync(document);
                uploadedDocuments.Add(document);
            }

            return uploadedDocuments;
        }

        public async Task<string> GenerateDocumentToken(string documentId)
        {
            string filePath = Path.Combine(_documentsDirectory, documentId);

            if (!System.IO.File.Exists(filePath))
            {
                return null;
            }

            string uniqueToken = Guid.NewGuid().ToString();
            await _databaseHelper.StoreTokenInDatabaseAsync(uniqueToken, documentId);

            return uniqueToken;
        }

        public (bool, string) CheckDocumentToken(string token)
        {
            return _databaseHelper.CheckTokenValidity(token);
        }

        private int GetDownloadCount(string documentId)
        {
            int downloadCount = _databaseHelper.GetDownloadCountFromDatabase(documentId);
            return downloadCount;
        }

        private string GetDocumentFilePath(string documentId)
        {
            string filePath = Path.Combine(_wwwrootPath, documentId);
            return filePath;
        }

        private void IncrementDownloadCount(string documentId)
        {
            _databaseHelper.UpdateDownloadCountInDatabase(documentId);
        }

        private List<string> GetDocumentFiles(string documentsPath)
        {
            if (!Directory.Exists(documentsPath))
            {
                return new List<string>();
            }

            string[] files = Directory.GetFiles(documentsPath);
            List<string> documentFiles = new List<string>(files);
            return documentFiles;
        }

        private List<DocumentDto> MapDocumentFilesToDtos(List<string> documentFiles)
        {
            List<DocumentDto> documentDtos = new List<DocumentDto>();

            foreach (var file in documentFiles)
            {
                string filePath = Path.Combine(_documentsDirectory, file);
                string fileName = Path.GetFileName(file);

                DocumentDto documentDto = new DocumentDto
                {
                    DocumentId = fileName,
                    Name = fileName,
                    Icon = GetIconByDocumentType(Path.GetExtension(file)),
                    type = Path.GetExtension(file),
                    ContentPreviewImage = GetDocumentPreview.ProcessDocument(file),
                    UploadDateTime = System.IO.File.GetCreationTimeUtc(filePath).ToString(),
                    DownloadCount = GetDownloadCount(fileName)
                };

                documentDtos.Add(documentDto);
            }

            return documentDtos;
        }

        private string GetIconByDocumentType(string fileExtension)
        {
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
    }

}
