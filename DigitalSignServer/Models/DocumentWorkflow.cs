using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigitalSignServer.Models
{
    public class DocumentWorkflow
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // ---------------------------------
        // Liên kết tới tài liệu (Document)
        // ---------------------------------
        [Required]
        public Guid DocumentId { get; set; }
        public Document Document { get; set; }

        // ---------------------------------
        // Liên kết tới template quy trình (WorkflowTemplate)
        // ---------------------------------
        [Required]
        public Guid WorkflowTemplateId { get; set; }
        public WorkflowTemplate WorkflowTemplate { get; set; }

        // ---------------------------------
        // Bước hiện tại trong quy trình
        // ---------------------------------
        public Guid? CurrentStepId { get; set; }
        public WorkflowStep? CurrentStep { get; set; }

        // ---------------------------------
        // Nhật ký phê duyệt (ApprovalHistory)
        // ---------------------------------
        public ICollection<ApprovalHistory> ApprovalHistories { get; set; } = new List<ApprovalHistory>();

        // ---------------------------------
        // Trạng thái & thời gian
        // ---------------------------------
        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed, Rejected, etc.

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        // ---------------------------------
        // Người khởi tạo
        // ---------------------------------
        public Guid? InitiatedByUserId { get; set; }
        public User? InitiatedBy { get; set; }
    }
}
