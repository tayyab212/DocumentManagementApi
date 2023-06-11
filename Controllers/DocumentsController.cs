using DocumentManagementApi.Models;
using DocumentManagementApi.Repository;
using DocumentManagementApi.Utilities;
using Microsoft.AspNetCore.Mvc;


namespace DocumentManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentRepository _documentRepository;

        public DocumentsController(IDocumentRepository documentRepository)
        {
            _documentRepository = documentRepository;
        }

        // GET api/documents/GetDocuments
        [HttpGet("GetDocuments")]
        public ActionResult<IEnumerable<DocumentDto>> GetDocuments()
        {
            List<DocumentDto> documentDtos = _documentRepository.GetDocuments();
            return Ok(documentDtos);
        }

        [HttpGet("DownloadDocument/{documentId}")]
        public IActionResult DownloadDocument(string documentId)
        {
            DocumentDto document = _documentRepository.GetDocument(documentId);

            if (document != null)
            {
                byte[] fileBytes = _documentRepository.GetDocumentFile(documentId);
                string contentType = Helper.GetContentType(Path.GetExtension(document.Name));
                return File(fileBytes, contentType, document.Name);
            }

            return NotFound();
        }

        [HttpPost]
        [Route("DownloadSelectedDocuments")]
        public IActionResult DownloadSelectedDocuments([FromBody] List<string> documentIds)
        {
            if (documentIds == null || documentIds.Count == 0)
            {
                return BadRequest("No documentIds specified.");
            }

            byte[] zipFileBytes = _documentRepository.DownloadSelectedDocuments(documentIds);

            if (zipFileBytes != null)
            {
                return File(zipFileBytes, "application/octet-stream", "documents.zip");
            }

            return NotFound();
        }

        [HttpPost]
        [Route("UploadDocumentAsync")]
        public async Task<IActionResult> UploadDocumentAsync(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest("No files selected.");
            }

            List<DocumentDto> uploadedDocuments = await _documentRepository.UploadDocumentsAsync(files);

            if (uploadedDocuments.Count > 0)
            {
                return Ok("Files uploaded successfully.");
            }

            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while uploading the files.");
        }

        [HttpPost]
        [Route("GenerateLink")]
        public async Task<ActionResult<string>> GenerateLinkAsync([FromBody] string documentId)
        {
            DocumentDto document = _documentRepository.GetDocument(documentId);

            if (document != null)
            {
                string uniqueToken = await _documentRepository.GenerateDocumentToken(documentId);
                string publicLink = $"{Request.Scheme}://{Request.Host}/api/documents/public/{uniqueToken}";

                return Ok(publicLink);
            }

            return NotFound("Document not found.");
        }

        [HttpGet("Public/{token}")]
        public IActionResult GetPublicDocument(string token)
        {
            (bool isValid, string documentId) = _documentRepository.CheckDocumentToken(token);

            if (isValid)
            {
                byte[] fileBytes = _documentRepository.GetDocumentFile(documentId);
                string contentType = Helper.GetContentType(Path.GetExtension(documentId));
                return File(fileBytes, contentType);
            }

            return NotFound();
        }

    }

}
