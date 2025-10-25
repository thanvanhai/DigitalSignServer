using System.ComponentModel.DataAnnotations;

namespace DigitalSignServer.Models
{
    public class WorkflowTemplate
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [Required]
        public Guid DocumentTypeId { get; set; }
        public DocumentType DocumentType { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();
        public ICollection<WorkflowConnection> Connections { get; set; } = new List<WorkflowConnection>(); // THÊM
    }
}