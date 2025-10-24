using System.ComponentModel.DataAnnotations;

namespace DigitalSignServer.Models
{
    /// <summary>
    /// Lịch sử ký từng bước
    /// </summary>
    public class ApprovalHistory
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid DocumentWorkflowId { get; set; }
        public DocumentWorkflow DocumentWorkflow { get; set; }

        [Required]
        public Guid WorkflowStepId { get; set; }
        public WorkflowStep WorkflowStep { get; set; }

        [Required]
        public Guid SignedByUserId { get; set; }
        public User SignedBy { get; set; }

        public DateTime SignedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string Status { get; set; } // "Ký OK", "Từ chối"

        [MaxLength(500)]
        public string? Note { get; set; } // ghi chú khi ký
    }
}
