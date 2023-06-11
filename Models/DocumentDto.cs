namespace DocumentManagementApi.Models
{
    // Define the document DTO (Data Transfer Object) for response
    public class DocumentDto
    {
        public string DocumentId { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string type { get; set; }
        public byte[] ContentPreviewImage { get; set; }
        public string UploadDateTime { get; set; }
        public int DownloadCount { get; set; }
    }
}
