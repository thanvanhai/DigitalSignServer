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
                .Include(t => t.Steps.OrderBy(s => s.Level))
                .ToListAsync();

            var result = templates.Select(t => new WorkflowTemplateDto
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
                }).ToList()
            });

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
                .Include(t => t.Steps.OrderBy(s => s.Level))
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null)
                return NotFound(new { message = "Không tìm thấy workflow template" });

            var dto = new WorkflowTemplateDto
            {
                Id = template.Id,
                Name = template.Name,
                DocumentTypeId = template.DocumentTypeId,
                DocumentTypeName = template.DocumentType?.Name,
                IsActive = template.IsActive,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt,
                Steps = template.Steps.OrderBy(s => s.Level).Select(s => new WorkflowStepDto
                {
                    Id = s.Id,
                    Level = s.Level,
                    Role = s.Role,
                    SignatureType = s.SignatureType,
                    Description = s.Description,
                    PositionX = s.PositionX,
                    PositionY = s.PositionY,
                    NodeType = s.NodeType
                }).ToList()
            };

            return Ok(dto);
        }

        // ======================
        // POST: api/Workflow
        // ======================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] WorkflowTemplateCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validate DocumentType
            var docTypeExists = await _context.DocumentTypes.AnyAsync(d => d.Id == dto.DocumentTypeId);
            if (!docTypeExists)
                return BadRequest(new { message = "DocumentTypeId không hợp lệ" });

            // Validate steps basic
            var levelSet = new HashSet<int>();
            foreach (var s in dto.Steps)
            {
                if (levelSet.Contains(s.Level))
                    return BadRequest(new { message = "Các Level không được trùng nhau trong cùng một template" });
                levelSet.Add(s.Level);
            }

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
                await _context.SaveChangesAsync(); // để có template.Id

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

                await tx.CommitAsync();

                _logger.LogInformation("Created WorkflowTemplate {TemplateId} ({Name}) with {Count} steps", template.Id, template.Name, steps.Count);
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
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null)
                return NotFound(new { message = "Không tìm thấy workflow template" });

            // Validate steps basic
            var levelSet = new HashSet<int>();
            foreach (var s in dto.Steps)
            {
                if (levelSet.Contains(s.Level))
                    return BadRequest(new { message = "Các Level không được trùng nhau trong cùng một template" });
                levelSet.Add(s.Level);
            }

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                template.Name = dto.Name;
                template.DocumentTypeId = dto.DocumentTypeId;
                template.UpdatedAt = DateTime.UtcNow;

                // Remove old steps
                _context.WorkflowSteps.RemoveRange(template.Steps);
                await _context.SaveChangesAsync();

                // Add new steps
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
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null)
                return NotFound(new { message = "Không tìm thấy workflow template" });

            _context.WorkflowSteps.RemoveRange(template.Steps);
            _context.WorkflowTemplates.Remove(template);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted WorkflowTemplate {TemplateId}", id);
            return NoContent();
        }

        // ======================
        // GET: api/Workflow/{id}/validate
        // Kiểm tra workflow hợp lệ cho mô hình tuần tự
        // ======================
        [HttpGet("{id}/validate")]
        public async Task<IActionResult> ValidateWorkflow(Guid id)
        {
            var template = await _context.WorkflowTemplates
                .Include(t => t.Steps)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null)
                return NotFound(new { message = "Không tìm thấy workflow template" });

            var errors = new List<string>();
            var steps = template.Steps.OrderBy(s => s.Level).ToList();

            if (!steps.Any())
                errors.Add("Workflow phải có ít nhất 1 bước.");

            // Kiểm tra tồn tại 1 node start và 1 node end (nếu UI vẫn dùng start/end)
            if (!steps.Any(s => string.Equals(s.NodeType, "start", StringComparison.OrdinalIgnoreCase)))
                errors.Add("Workflow nên có 1 node 'start' (nếu dùng loại node này).");

            if (!steps.Any(s => string.Equals(s.NodeType, "end", StringComparison.OrdinalIgnoreCase)))
                errors.Add("Workflow nên có 1 node 'end' (nếu dùng loại node này).");

            // Kiểm tra Level không trùng và liên tục (1..n) - nếu cậu muốn bắt buộc
            var levels = steps.Select(s => s.Level).ToList();
            if (levels.Distinct().Count() != levels.Count)
                errors.Add("Các Level không được trùng nhau.");

            var min = levels.MinOrDefault();
            var max = levels.MaxOrDefault();
            // nếu muốn bắt buộc Level bắt đầu từ 1 và liên tục đến max:
            if (levels.Any() && !(min == 1 && levels.Count == (max - min + 1)))
                errors.Add("Level nên bắt đầu từ 1 và liên tục (1..n) để quy trình tuần tự rõ ràng.");

            if (errors.Any())
                return BadRequest(new { isValid = false, errors });

            return Ok(new { isValid = true, message = "Workflow hợp lệ cho mô hình tuần tự" });
        }
    }

    // helper extension (nếu .NET version không có MinOrDefault/MaxOrDefault)
    internal static class EnumerableExtensions
    {
        public static int MinOrDefault(this IEnumerable<int> source) =>
            source?.Any() == true ? source.Min() : 0;
        public static int MaxOrDefault(this IEnumerable<int> source) =>
            source?.Any() == true ? source.Max() : 0;
    }
}
