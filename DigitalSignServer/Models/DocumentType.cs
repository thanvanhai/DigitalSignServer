using System.ComponentModel.DataAnnotations;

namespace DigitalSignServer.Models
{
    /// <summary>
    /// Loại tài liệu (Đơn nghỉ phép, Đơn xin tăng ca, Đơn xin thiết bị...)
    /// </summary>
    public class DocumentType
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } // "Đơn nghỉ phép", "Đơn xin tăng ca"

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<WorkflowTemplate> WorkflowTemplates { get; set; }
        public virtual ICollection<Document> Documents { get; set; }
    }
}