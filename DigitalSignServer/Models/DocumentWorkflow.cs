using System.ComponentModel.DataAnnotations;

namespace DigitalSignServer.Models
{
    /// <summary>
    /// Một tài liệu thực tế đang được ký
    /// </summary>
    public class DocumentWorkflow
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid DocumentId { get; set; }
        public Document Document { get; set; }

        [Required]
        public Guid WorkflowTemplateId { get; set; }
        public WorkflowTemplate WorkflowTemplate { get; set; }

        public int CurrentLevel { get; set; } = 1; // Level đang chờ ký

        [MaxLength(50)]
        public string Status { get; set; } = "Chờ ký"; // "Chờ ký", "Đã ký đầy đủ", "Từ chối"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public virtual ICollection<ApprovalHistory> ApprovalHistories { get; set; }
    }
}
