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
                .Include(t => t.Connections) // THÊM
                .ToListAsync();

            var result = templates.Select(t => new WorkflowTemplateDto
            {
                Id = t.Id,
                Name = t.Name,
                DocumentTypeId = t.DocumentTypeId,
                DocumentTypeName = t.DocumentType?.Name,
                Steps = t.Steps.Select(s => new WorkflowStepDto
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
                    Id = c.Id,
                    SourceStepId = c.SourceStepId,
                    TargetStepId = c.TargetStepId,
                    Condition = c.Condition,
                    Priority = c.Priority,
                    Label = c.Label
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
                .Include(t => t.Steps)
                .Include(t => t.Connections)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null)
                return NotFound(new { message = "Không tìm thấy workflow template" });

            var dto = new WorkflowTemplateDto
            {
                Id = template.Id,
                Name = template.Name,
                DocumentTypeId = template.DocumentTypeId,
                DocumentTypeName = template.DocumentType?.Name,
                Steps = template.Steps.Select(s => new WorkflowStepDto
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
                Connections = template.Connections.Select(c => new WorkflowConnectionDto
                {
                    Id = c.Id,
                    SourceStepId = c.SourceStepId,
                    TargetStepId = c.TargetStepId,
                    Condition = c.Condition,
                    Priority = c.Priority,
                    Label = c.Label
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

            // Validate connections
            var stepIds = dto.Steps.Select((s, idx) => idx).ToList(); // Tạm thời dùng index
            foreach (var conn in dto.Connections)
            {
                // Sẽ validate sau khi tạo steps
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Tạo template
                var template = new WorkflowTemplate
                {
                    Name = dto.Name,
                    DocumentTypeId = dto.DocumentTypeId
                };

                _context.WorkflowTemplates.Add(template);
                await _context.SaveChangesAsync(); // Lưu để có Id

                // Tạo steps và map temporary Id
                var stepIdMap = new Dictionary<int, Guid>(); // index -> real Guid
                var steps = new List<WorkflowStep>();

                for (int i = 0; i < dto.Steps.Count; i++)
                {
                    var stepDto = dto.Steps[i];
                    var step = new WorkflowStep
                    {
                        WorkflowTemplateId = template.Id,
                        Level = stepDto.Level,
                        Role = stepDto.Role,
                        SignatureType = stepDto.SignatureType,
                        Description = stepDto.Description,
                        PositionX = stepDto.PositionX,
                        PositionY = stepDto.PositionY,
                        NodeType = stepDto.NodeType
                    };
                    steps.Add(step);
                    stepIdMap[i] = step.Id;
                }

                _context.WorkflowSteps.AddRange(steps);
                await _context.SaveChangesAsync();

                // Tạo connections
                var connections = new List<WorkflowConnection>();
                foreach (var connDto in dto.Connections)
                {
                    var connection = new WorkflowConnection
                    {
                        WorkflowTemplateId = template.Id,
                        SourceStepId = connDto.SourceStepId,
                        TargetStepId = connDto.TargetStepId,
                        Condition = connDto.Condition,
                        Priority = connDto.Priority,
                        Label = connDto.Label
                    };
                    connections.Add(connection);
                }

                _context.WorkflowConnections.AddRange(connections);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("✅ Created WorkflowTemplate {Name} with {StepCount} steps and {ConnCount} connections",
                    template.Name, steps.Count, connections.Count);

                return CreatedAtAction(nameof(Get), new { id = template.Id }, new { id = template.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "❌ Error creating workflow template");
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

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Update template info
                template.Name = dto.Name;
                template.DocumentTypeId = dto.DocumentTypeId;
                template.UpdatedAt = DateTime.UtcNow;

                // Xóa connections và steps cũ
                _context.WorkflowConnections.RemoveRange(template.Connections);
                _context.WorkflowSteps.RemoveRange(template.Steps);
                await _context.SaveChangesAsync();

                // Tạo lại steps
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

                // Tạo lại connections
                var connections = dto.Connections.Select(c => new WorkflowConnection
                {
                    WorkflowTemplateId = template.Id,
                    SourceStepId = c.SourceStepId,
                    TargetStepId = c.TargetStepId,
                    Condition = c.Condition,
                    Priority = c.Priority,
                    Label = c.Label
                }).ToList();

                _context.WorkflowConnections.AddRange(connections);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("🔄 Updated WorkflowTemplate {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "❌ Error updating workflow template");
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

            _logger.LogInformation("🗑️ Deleted WorkflowTemplate {Id}", id);
            return NoContent();
        }

        // ======================
        // GET: api/Workflow/{id}/validate
        // Kiểm tra workflow có hợp lệ không
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

            // 1. Kiểm tra có ít nhất 1 step
            if (!template.Steps.Any())
                errors.Add("Workflow phải có ít nhất 1 bước");

            // 2. Kiểm tra có node start
            if (!template.Steps.Any(s => s.NodeType == "start"))
                errors.Add("Workflow phải có 1 node Start");

            // 3. Kiểm tra có node end
            if (!template.Steps.Any(s => s.NodeType == "end"))
                errors.Add("Workflow phải có 1 node End");

            // 4. Kiểm tra tất cả nodes (trừ end) phải có outgoing connection
            var nodesWithoutOutgoing = template.Steps
                .Where(s => s.NodeType != "end" && !template.Connections.Any(c => c.SourceStepId == s.Id))
                .Select(s => s.Role)
                .ToList();

            if (nodesWithoutOutgoing.Any())
                errors.Add($"Các node sau chưa có connection đi ra: {string.Join(", ", nodesWithoutOutgoing)}");

            // 5. Kiểm tra tất cả nodes (trừ start) phải có incoming connection
            var nodesWithoutIncoming = template.Steps
                .Where(s => s.NodeType != "start" && !template.Connections.Any(c => c.TargetStepId == s.Id))
                .Select(s => s.Role)
                .ToList();

            if (nodesWithoutIncoming.Any())
                errors.Add($"Các node sau chưa có connection đi vào: {string.Join(", ", nodesWithoutIncoming)}");

            // 6. Kiểm tra chu trình (cycle detection) - dùng DFS
            if (HasCycle(template.Steps.ToList(), template.Connections.ToList()))
                errors.Add("Workflow có chu trình (cycle), cần loại bỏ");

            if (errors.Any())
            {
                return BadRequest(new
                {
                    isValid = false,
                    errors = errors
                });
            }

            return Ok(new
            {
                isValid = true,
                message = "Workflow hợp lệ"
            });
        }

        // Helper: Kiểm tra chu trình
        private bool HasCycle(List<WorkflowStep> steps, List<WorkflowConnection> connections)
        {
            var visited = new HashSet<Guid>();
            var recursionStack = new HashSet<Guid>();

            foreach (var step in steps)
            {
                if (HasCycleDFS(step.Id, visited, recursionStack, connections))
                    return true;
            }

            return false;
        }

        private bool HasCycleDFS(Guid nodeId, HashSet<Guid> visited, HashSet<Guid> recursionStack, List<WorkflowConnection> connections)
        {
            visited.Add(nodeId);
            recursionStack.Add(nodeId);

            var outgoing = connections.Where(c => c.SourceStepId == nodeId);
            foreach (var conn in outgoing)
            {
                if (!visited.Contains(conn.TargetStepId))
                {
                    if (HasCycleDFS(conn.TargetStepId, visited, recursionStack, connections))
                        return true;
                }
                else if (recursionStack.Contains(conn.TargetStepId))
                {
                    return true; // Phát hiện cycle
                }
            }

            recursionStack.Remove(nodeId);
            return false;
        }
    }
}