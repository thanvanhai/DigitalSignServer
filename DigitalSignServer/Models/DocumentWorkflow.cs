using System.ComponentModel.DataAnnotations;

namespace DigitalSignServer.Models
{
    public class DocumentWorkflow
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // ---------------------------------
        // Liên kết tới tài liệu gốc
        // ---------------------------------
        [Required]
        public Guid DocumentId { get; set; }
        public Document? Document { get; set; }

        // ---------------------------------
        // Liên kết tới mẫu quy trình
        // ---------------------------------
        [Required]
        public Guid WorkflowTemplateId { get; set; }
        public WorkflowTemplate? WorkflowTemplate { get; set; }

        // ---------------------------------
        // Bước hiện tại (theo Level hoặc Step cụ thể)
        // ---------------------------------
        public Guid? CurrentStepId { get; set; }
        public WorkflowStep? CurrentStep { get; set; }

        // ---------------------------------
        // Nhật ký phê duyệt (lưu các lần ký)
        // ---------------------------------
        public ICollection<ApprovalHistory> ApprovalHistories { get; set; } = new List<ApprovalHistory>();

        // ---------------------------------
        // Trạng thái & thời gian
        // ---------------------------------
        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed, Rejected

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        // ---------------------------------
        // Người khởi tạo quy trình
        // ---------------------------------
        public Guid? InitiatedByUserId { get; set; }
        public User? InitiatedBy { get; set; }
    }
}
