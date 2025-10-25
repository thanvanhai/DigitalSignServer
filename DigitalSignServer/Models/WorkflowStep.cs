using System.ComponentModel.DataAnnotations;

namespace DigitalSignServer.Models
{
    public class WorkflowStep
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid WorkflowTemplateId { get; set; }
        public WorkflowTemplate WorkflowTemplate { get; set; }

        [Required]
        public int Level { get; set; } // Giữ lại để tham khảo thứ tự

        [Required]
        [MaxLength(100)]
        public string Role { get; set; }

        [Required]
        [MaxLength(50)]
        public string SignatureType { get; set; }

        public bool IsActive { get; set; } = true;

        // Thông tin cho Nodify
        public double PositionX { get; set; }
        public double PositionY { get; set; }

        [MaxLength(50)]
        public string? NodeType { get; set; } = "sign"; // "start", "approval", "sign", "end", "parallel"

        [MaxLength(500)]
        public string? Description { get; set; }

        // **QUAN TRỌNG: Navigation properties cho connections**
        public ICollection<WorkflowConnection> OutgoingConnections { get; set; } = new List<WorkflowConnection>();
        public ICollection<WorkflowConnection> IncomingConnections { get; set; } = new List<WorkflowConnection>();
    }
}