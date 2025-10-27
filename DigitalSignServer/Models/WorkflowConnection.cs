using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigitalSignServer.Models
{
    public class WorkflowConnection
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey(nameof(WorkflowTemplate))]
        public Guid WorkflowTemplateId { get; set; }
        public WorkflowTemplate WorkflowTemplate { get; set; } = null!;

        [ForeignKey(nameof(SourceStep))]
        public Guid SourceStepId { get; set; }
        public WorkflowStep SourceStep { get; set; } = null!;

        [ForeignKey(nameof(TargetStep))]
        public Guid TargetStepId { get; set; }
        public WorkflowStep TargetStep { get; set; } = null!;

        // Nếu muốn có điều kiện hoặc loại đường kết nối
        [MaxLength(100)]
        public string? Condition { get; set; }

        // Lưu thứ tự hoặc label (nếu có nhiều đường nối)
        public int? Order { get; set; }
    }
}
