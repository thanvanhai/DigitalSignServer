using DigitalSignServer.Data;
using DigitalSignServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class WorkflowTemplateController : ControllerBase
{
    private readonly AppDbContext _context;
    public WorkflowTemplateController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _context.WorkflowTemplates.Include(w => w.Steps).ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id) =>
        Ok(await _context.WorkflowTemplates.Include(w => w.Steps)
                                            .FirstOrDefaultAsync(w => w.Id == id));

    [HttpPost]
    public async Task<IActionResult> Create(WorkflowTemplate model)
    {
        model.Id = Guid.NewGuid();
        _context.WorkflowTemplates.Add(model);
        await _context.SaveChangesAsync();
        return Ok(model);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, WorkflowTemplate model)
    {
        var template = await _context.WorkflowTemplates.Include(w => w.Steps)
                                                       .FirstOrDefaultAsync(w => w.Id == id);
        if (template == null) return NotFound();

        template.Name = model.Name;
        template.IsActive = model.IsActive;

        // Xử lý steps (update, add, remove) - có thể custom theo nhu cầu
        // ...

        await _context.SaveChangesAsync();
        return Ok(template);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var template = await _context.WorkflowTemplates.FindAsync(id);
        if (template == null) return NotFound();

        template.IsActive = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
