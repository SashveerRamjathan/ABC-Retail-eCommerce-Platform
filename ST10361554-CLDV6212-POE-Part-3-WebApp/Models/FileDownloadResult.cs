namespace ST10361554_CLDV6212_POE_Part_3_WebApp.Models
{
    // Class representing the result of a file download
    public class FileDownloadResult
    {
        public required MemoryStream Content { get; set; } // The content of the file as a memory stream
        public required string ContentType { get; set; } // The MIME type of the file (e.g. "application/pdf")
        public required string FileName { get; set; } // The name of the file to be downloaded
    }
}
