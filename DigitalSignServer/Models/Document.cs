using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigitalSignServer.Models
{
    public enum DocumentStatus { Uploaded = 1, PendingSignature = 2, FullySigned = 3 }

    public class Document
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required, MaxLength(255)]
        public string FileName { get; set; } = string.Empty;
        [Required, MaxLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;
        [MaxLength(100)]
        public string ContentType { get; set; } = "application/pdf";
        [Required]
        public long FileSize { get; set; }
        [Required]
        public string StoragePath { get; set; } = string.Empty;
        public string? SignedFilePath { get; set; }
        [MaxLength(64)]
        public string? Hash { get; set; }
        public bool IsSigned { get; set; } = false;
        public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SignedAt { get; set; }
        [MaxLength(500)]
        public string? Description { get; set; }

        [ForeignKey(nameof(UploadedBy))]
        public Guid UploadedByUserId { get; set; }
        public virtual User UploadedBy { get; set; } = null!;

        // Liên kết workflow
        public Guid DocumentTypeId { get; set; }
        public virtual DocumentType? DocumentType { get; set; }
        public virtual DocumentWorkflow? Workflow { get; set; }

        public virtual ICollection<Signature> Signatures { get; set; } = new List<Signature>();
    }
}