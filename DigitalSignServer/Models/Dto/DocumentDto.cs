using System.ComponentModel.DataAnnotations;

namespace DigitalSignServer.Models.Dto
{
    public class UploadRequest
    {
        [Required]
        public IFormFile File { get; set; } = null!;

        public string? Description { get; set; }
    }

    public class DocumentResponse
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public bool IsSigned { get; set; }
        public DateTime UploadedAt { get; set; }
        public DateTime? SignedAt { get; set; }
        public string? Description { get; set; }
        public int SignatureCount { get; set; }
        public string UploadedByUsername { get; set; } = string.Empty;
    }
}
