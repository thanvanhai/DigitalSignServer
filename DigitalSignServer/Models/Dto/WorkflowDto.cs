namespace DigitalSignServer.Models.Dto
{
    // ============================================
    // WORKFLOW STEP DTOs
    // ============================================
    public class WorkflowStepDto
    {
        public Guid Id { get; set; }
        public int Level { get; set; }
        public string Role { get; set; } = string.Empty;
        public string SignatureType { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Thông tin hiển thị vị trí (cho giao diện Nodify)
        public double PositionX { get; set; }
        public double PositionY { get; set; }
        public string? NodeType { get; set; }
    }

    public class WorkflowStepCreateDto
    {
        public int Level { get; set; }
        public string Role { get; set; } = string.Empty;
        public string SignatureType { get; set; } = string.Empty;
        public string? Description { get; set; }

        public double PositionX { get; set; }
        public double PositionY { get; set; }
        public string? NodeType { get; set; } = "sign"; // mặc định là nút ký
    }

    // ============================================
    // WORKFLOW TEMPLATE DTOs
    // ============================================
    public class WorkflowTemplateDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public Guid DocumentTypeId { get; set; }
        public string? DocumentTypeName { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<WorkflowStepDto> Steps { get; set; } = new();
    }

    public class WorkflowTemplateCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public Guid DocumentTypeId { get; set; }

        public List<WorkflowStepCreateDto> Steps { get; set; } = new();
    }
}
