using System.ComponentModel.DataAnnotations;

namespace DigitalSignServer.Models
{
    public class WorkflowStep
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid WorkflowTemplateId { get; set; }
        public WorkflowTemplate? WorkflowTemplate { get; set; }

        [Required]
        public int Level { get; set; } // Thứ tự ký: 1, 2, 3, ...

        [Required, MaxLength(100)]
        public string Role { get; set; } = string.Empty; // Vai trò người ký

        [Required, MaxLength(50)]
        public string SignatureType { get; set; } = "Chính"; // Nháy, Chính, Lưu trữ

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Thông tin hiển thị trong giao diện Nodify
        public double PositionX { get; set; }
        public double PositionY { get; set; }

        [MaxLength(50)]
        public string? NodeType { get; set; } = "sign"; // "start", "sign", "approval", "end"
    }
}
