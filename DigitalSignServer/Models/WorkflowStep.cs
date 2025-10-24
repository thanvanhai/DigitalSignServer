using System.ComponentModel.DataAnnotations;

namespace DigitalSignServer.Models
{
    /// <summary>
    /// Một bước trong workflow template
    /// </summary>
    public class WorkflowStep
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid WorkflowTemplateId { get; set; }
        public WorkflowTemplate WorkflowTemplate { get; set; }

        [Required]
        public int Level { get; set; } // thứ tự ký

        [Required]
        [MaxLength(100)]
        public string Role { get; set; } // chức vụ hoặc người ký

        [Required]
        [MaxLength(50)]
        public string SignatureType { get; set; } // "Nháy", "Chính", "Lưu trữ"

        public bool IsActive { get; set; } = true;
    }
}
