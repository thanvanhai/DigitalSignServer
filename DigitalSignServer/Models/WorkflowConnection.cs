using System.ComponentModel.DataAnnotations;

namespace DigitalSignServer.Models
{
    /// <summary>
    /// Kết nối giữa các bước trong workflow (hỗ trợ song song, phân nhánh)
    /// </summary>
    public class WorkflowConnection
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid WorkflowTemplateId { get; set; }
        public WorkflowTemplate WorkflowTemplate { get; set; }

        [Required]
        public Guid SourceStepId { get; set; }
        public WorkflowStep SourceStep { get; set; }

        [Required]
        public Guid TargetStepId { get; set; }
        public WorkflowStep TargetStep { get; set; }

        /// <summary>
        /// Điều kiện để đi theo connection này
        /// "auto" = tự động chuyển
        /// "approved" = khi bước source được duyệt
        /// "rejected" = khi bước source bị từ chối
        /// </summary>
        [MaxLength(50)]
        public string? Condition { get; set; } = "auto";

        /// <summary>
        /// Độ ưu tiên (dùng khi có nhiều connection từ 1 source)
        /// Số nhỏ = ưu tiên cao
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Ghi chú cho connection
        /// </summary>
        [MaxLength(200)]
        public string? Label { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}