using DigitalSignServer.Data;
using DigitalSignServer.Models;
using DigitalSignServer.Models.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DigitalSignServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WorkflowController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<WorkflowController> _logger;

        public WorkflowController(AppDbContext context, ILogger<WorkflowController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ======================
        // GET: api/Workflow
        // ======================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var templates = await _context.WorkflowTemplates
                .Include(t => t.DocumentType)
                .Include(t => t.Steps)
                .Include(t => t.Connections)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var result = templates.Select(t => MapTemplateToDto(t));
            return Ok(result);
        }

        // ======================
        // GET: api/Workflow/{id}
        // ======================
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var template = await _context.WorkflowTemplates
                .Include(t => t.DocumentType)
                .Include(t => t.Steps)
                .Include(t => t.Connections)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null)
                return NotFound(new { message = "Không tìm thấy workflow template" });

            return Ok(MapTemplateToDto(template));
        }

        // ======================
        // POST: api/Workflow
        // ======================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] WorkflowTemplateCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var docTypeExists = await _context.DocumentTypes.AnyAsync(d => d.Id == dto.DocumentTypeId);
            if (!docTypeExists)
                return BadRequest(new { message = "DocumentTypeId không hợp lệ" });

            // Validate levels unique
            if (dto.Steps.GroupBy(s => s.Level).Any(g => g.Count() > 1))
                return BadRequest(new { message = "Các Level không được trùng nhau trong cùng một template" });

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var template = new WorkflowTemplate
                {
                    Name = dto.Name,
                    DocumentTypeId = dto.DocumentTypeId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.WorkflowTemplates.Add(template);
                await _context.SaveChangesAsync();

                // Add steps
                var steps = dto.Steps.Select(s => new WorkflowStep
                {
                    WorkflowTemplateId = template.Id,
                    Level = s.Level,
                    Role = s.Role,
                    SignatureType = s.SignatureType,
                    Description = s.Description,
                    PositionX = s.PositionX,
                    PositionY = s.PositionY,
                    NodeType = s.NodeType
                }).ToList();

                _context.WorkflowSteps.AddRange(steps);
                await _context.SaveChangesAsync();

                // Add connections (nếu có)
                if (dto.Connections?.Any() == true)
                {
                    var connections = dto.Connections.Select(c => new WorkflowConnection
                    {
                        WorkflowTemplateId = template.Id,
                        SourceStepId = c.SourceStepId,
                        TargetStepId = c.TargetStepId,
                        Condition = c.Condition,
                        Order = c.Order
                    }).ToList();

                    _context.WorkflowConnections.AddRange(connections);
                    await _context.SaveChangesAsync();
                }

                await tx.CommitAsync();
                _logger.LogInformation("Created WorkflowTemplate {TemplateId} with {StepCount} steps", template.Id, steps.Count);

                return CreatedAtAction(nameof(Get), new { id = template.Id }, new { id = template.Id });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error creating workflow template");
                return StatusCode(500, new { message = "Lỗi khi tạo workflow", error = ex.Message });
            }
        }

        // ======================
        // PUT: api/Workflow/{id}
        // ======================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] WorkflowTemplateCreateDto dto)
        {
            var template = await _context.WorkflowTemplates
                .Include(t => t.Steps)
                .Include(t => t.Connections)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null)
                return NotFound(new { message = "Không tìm thấy workflow template" });

            if (dto.Steps.GroupBy(s => s.Level).Any(g => g.Count() > 1))
                return BadRequest(new { message = "Các Level không được trùng nhau trong cùng một template" });

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                template.Name = dto.Name;
                template.DocumentTypeId = dto.DocumentTypeId;
                template.UpdatedAt = DateTime.UtcNow;

                // Xóa step & connection cũ
                _context.WorkflowConnections.RemoveRange(template.Connections);
                _context.WorkflowSteps.RemoveRange(template.Steps);
                await _context.SaveChangesAsync();

                // Thêm step mới
                var newSteps = dto.Steps.Select(s => new WorkflowStep
                {
                    WorkflowTemplateId = template.Id,
                    Level = s.Level,
                    Role = s.Role,
                    SignatureType = s.SignatureType,
                    Description = s.Description,
                    PositionX = s.PositionX,
                    PositionY = s.PositionY,
                    NodeType = s.NodeType
                }).ToList();

                _context.WorkflowSteps.AddRange(newSteps);
                await _context.SaveChangesAsync();

                // Thêm connection mới (nếu có)
                if (dto.Connections?.Any() == true)
                {
                    var newConnections = dto.Connections.Select(c => new WorkflowConnection
                    {
                        WorkflowTemplateId = template.Id,
                        SourceStepId = c.SourceStepId,
                        TargetStepId = c.TargetStepId,
                        Condition = c.Condition,
                        Order = c.Order
                    }).ToList();

                    _context.WorkflowConnections.AddRange(newConnections);
                    await _context.SaveChangesAsync();
                }

                await tx.CommitAsync();
                _logger.LogInformation("Updated WorkflowTemplate {TemplateId}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error updating workflow template");
                return StatusCode(500, new { message = "Lỗi khi cập nhật workflow", error = ex.Message });
            }
        }

        // ======================
        // DELETE: api/Workflow/{id}
        // ======================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var template = await _context.WorkflowTemplates
                .Include(t => t.Steps)
                .Include(t => t.Connections)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null)
                return NotFound(new { message = "Không tìm thấy workflow template" });

            _context.WorkflowConnections.RemoveRange(template.Connections);
            _context.WorkflowSteps.RemoveRange(template.Steps);
            _context.WorkflowTemplates.Remove(template);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted WorkflowTemplate {TemplateId}", id);
            return NoContent();
        }

        // ======================
        // GET: api/Workflow/{id}/validate
        // ======================
        [HttpGet("{id}/validate")]
        public async Task<IActionResult> ValidateWorkflow(Guid id)
        {
            var template = await _context.WorkflowTemplates
                .Include(t => t.Steps)
                .Include(t => t.Connections)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null)
                return NotFound(new { message = "Không tìm thấy workflow template" });

            var errors = new List<string>();
            var steps = template.Steps.OrderBy(s => s.Level).ToList();

            if (!steps.Any())
                errors.Add("Workflow phải có ít nhất 1 bước.");

            if (!steps.Any(s => string.Equals(s.NodeType, "start", StringComparison.OrdinalIgnoreCase)))
                errors.Add("Workflow nên có node 'start'.");

            if (!steps.Any(s => string.Equals(s.NodeType, "end", StringComparison.OrdinalIgnoreCase)))
                errors.Add("Workflow nên có node 'end'.");

            if (steps.Select(s => s.Level).Distinct().Count() != steps.Count)
                errors.Add("Các Level không được trùng nhau.");

            if (!template.Connections.Any())
                errors.Add("Workflow chưa có liên kết giữa các bước.");

            if (errors.Any())
                return BadRequest(new { isValid = false, errors });

            return Ok(new { isValid = true, message = "Workflow hợp lệ" });
        }

        // ======================
        // Helper: map entity -> DTO
        // ======================
        private static WorkflowTemplateDto MapTemplateToDto(WorkflowTemplate t)
        {
            return new WorkflowTemplateDto
            {
                Id = t.Id,
                Name = t.Name,
                DocumentTypeId = t.DocumentTypeId,
                DocumentTypeName = t.DocumentType?.Name,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                Steps = t.Steps.OrderBy(s => s.Level).Select(s => new WorkflowStepDto
                {
                    Id = s.Id,
                    Level = s.Level,
                    Role = s.Role,
                    SignatureType = s.SignatureType,
                    Description = s.Description,
                    PositionX = s.PositionX,
                    PositionY = s.PositionY,
                    NodeType = s.NodeType
                }).ToList(),
                Connections = t.Connections.Select(c => new WorkflowConnectionDto
                {
                    SourceStepId = c.SourceStepId,
                    TargetStepId = c.TargetStepId,
                    Condition = c.Condition,
                    Order = c.Order
                }).ToList()
            };
        }
    }
}
