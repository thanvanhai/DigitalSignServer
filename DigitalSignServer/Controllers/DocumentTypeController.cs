using DigitalSignServer.Data;
using DigitalSignServer.Models;
using DigitalSignServer.Models.Dto;
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
        var types = await _context.DocumentTypes
            .Where(dt => dt.IsActive)
            .Select(dt => new DocumentTypeDto
            {
                Id = dt.Id,
                Name = dt.Name,
                Description = dt.Description,
                IsActive = dt.IsActive
            })
            .ToListAsync();

        return Ok(types);
    }

    [HttpPost]
    public async Task<IActionResult> Create(DocumentTypeDto dto)
    {
        var type = new DocumentType
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            IsActive = dto.IsActive
        };

        _context.DocumentTypes.Add(type);
        await _context.SaveChangesAsync();
        return Ok(type);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, DocumentTypeDto dto)
    {
        var type = await _context.DocumentTypes.FindAsync(id);
        if (type == null) return NotFound();

        type.Name = dto.Name;
        type.Description = dto.Description;
        type.IsActive = dto.IsActive;
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
