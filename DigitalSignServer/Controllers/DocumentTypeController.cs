using DigitalSignServer.Data;
using DigitalSignServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class DocumentTypeController : ControllerBase
{
    private readonly AppDbContext _context;
    public DocumentTypeController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var types = await _context.DocumentTypes.Where(dt => dt.IsActive).ToListAsync();
        return Ok(types);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var type = await _context.DocumentTypes.FindAsync(id);
        if (type == null) return NotFound();
        return Ok(type);
    }

    [HttpPost]
    public async Task<IActionResult> Create(DocumentType model)
    {
        model.Id = Guid.NewGuid();
        _context.DocumentTypes.Add(model);
        await _context.SaveChangesAsync();
        return Ok(model);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, DocumentType model)
    {
        var type = await _context.DocumentTypes.FindAsync(id);
        if (type == null) return NotFound();

        type.Name = model.Name;
        type.Description = model.Description;
        type.IsActive = model.IsActive;
        type.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(type);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var type = await _context.DocumentTypes.FindAsync(id);
        if (type == null) return NotFound();

        type.IsActive = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
