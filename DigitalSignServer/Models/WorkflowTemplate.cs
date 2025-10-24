using System.ComponentModel.DataAnnotations;

namespace DigitalSignServer.Models
{
    /// <summary>
    /// Mẫu luồng ký (template) cho từng loại tài liệu
    /// </summary>
    public class WorkflowTemplate
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid DocumentTypeId { get; set; }
        public DocumentType DocumentType { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } // "Luồng nghỉ phép chuẩn"

        public int MaxLevel { get; set; } = 1; // số bước tối đa

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public virtual ICollection<WorkflowStep> Steps { get; set; }
    }
}
