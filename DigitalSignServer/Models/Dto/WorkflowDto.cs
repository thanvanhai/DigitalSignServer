namespace DigitalSignServer.Models.Dto
{
    // Models/Dto/WorkflowStepDto.cs
    public class WorkflowStepDto
    {
        public Guid Id { get; set; }
        public int Level { get; set; }
        public string Role { get; set; }
        public string SignatureType { get; set; }
        public string? Description { get; set; }
        public double PositionX { get; set; }
        public double PositionY { get; set; }
        public string? NodeType { get; set; }
    }

    public class WorkflowStepCreateDto
    {
        public int Level { get; set; }
        public string Role { get; set; }
        public string SignatureType { get; set; }
        public string? Description { get; set; }
        public double PositionX { get; set; }
        public double PositionY { get; set; }
        public string? NodeType { get; set; }
    }

    // Models/Dto/WorkflowConnectionDto.cs
    public class WorkflowConnectionDto
    {
        public Guid Id { get; set; }
        public Guid SourceStepId { get; set; }
        public Guid TargetStepId { get; set; }
        public string? Condition { get; set; }
        public int Priority { get; set; }
        public string? Label { get; set; }
    }

    public class WorkflowConnectionCreateDto
    {
        public Guid SourceStepId { get; set; }
        public Guid TargetStepId { get; set; }
        public string? Condition { get; set; } = "auto";
        public int Priority { get; set; } = 0;
        public string? Label { get; set; }
    }

    // Models/Dto/WorkflowTemplateDto.cs - CẬP NHẬT
    public class WorkflowTemplateDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid DocumentTypeId { get; set; }
        public string? DocumentTypeName { get; set; }
        public List<WorkflowStepDto> Steps { get; set; } = new();
        public List<WorkflowConnectionDto> Connections { get; set; } = new(); // THÊM
    }

    public class WorkflowTemplateCreateDto
    {
        public string Name { get; set; }
        public Guid DocumentTypeId { get; set; }
        public List<WorkflowStepCreateDto> Steps { get; set; } = new();
        public List<WorkflowConnectionCreateDto> Connections { get; set; } = new(); // THÊM
    }
}
