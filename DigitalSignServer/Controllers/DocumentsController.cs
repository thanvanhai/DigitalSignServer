using DigitalSignServer.Data;
using DigitalSignServer.Models;
using DigitalSignServer.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DigitalSignServer.Services;
using System.Security.Claims;

namespace DigitalSignServer.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPdfSignerService _signingService;
    private readonly ILogger<DocumentsController> _logger;
    private readonly IConfiguration _configuration;

    public DocumentsController(
        AppDbContext context,
        IPdfSignerService signingService,
        ILogger<DocumentsController> logger,
        IConfiguration configuration)
    {
        _context = context;
        _signingService = signingService;
        _logger = logger;
        _configuration = configuration;
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    // === Upload PDF ===
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] UploadRequest request)
    {
        var file = request.File;
        var description = request.Description;

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded" });

        if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Only PDF files are allowed" });

        var maxFileSizeMB = int.Parse(_configuration["FileSettings:MaxFileSizeMB"] ?? "10");
        if (file.Length > maxFileSizeMB * 1024 * 1024)
            return BadRequest(new { message = $"File size must not exceed {maxFileSizeMB}MB" });

        var userId = GetCurrentUserId();
        var uploadPath = _configuration["FileSettings:UploadPath"] ?? "Uploads";
        Directory.CreateDirectory(uploadPath);

        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var storagePath = Path.Combine(uploadPath, fileName);

        using (var stream = new FileStream(storagePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var document = new Document
        {
            FileName = Path.GetFileName(file.FileName),
            OriginalFileName = file.FileName,
            StoragePath = storagePath,
            FileSize = file.Length,
            ContentType = file.ContentType,
            UploadedByUserId = userId,
            UploadedAt = DateTime.UtcNow,
            Description = description,
            Status = DocumentStatus.Uploaded,
            IsSigned = false
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        _logger.LogInformation("📄 Uploaded file: {File} by user {UserId}", file.FileName, userId);

        return Ok(new
        {
            id = document.Id,
            fileName = document.FileName,
            fileSize = document.FileSize,
            uploadedAt = document.UploadedAt
        });
    }

    // === Upload signed PDF (from WPF client) ===
    [HttpPost("{id:guid}/upload-signed")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadSignedPdf(Guid id, IFormFile file)
    {
        var userId = GetCurrentUserId();

        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.UploadedByUserId == userId);

        if (document == null)
            return NotFound(new { message = "Document not found" });

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No signed file uploaded" });

        if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Only PDF files are allowed" });

        try
        {
            var signedPath = _configuration["FileSettings:SignedPath"] ?? "Signed";
            Directory.CreateDirectory(signedPath);

            var outputFileName = $"hand_signed_{Path.GetFileNameWithoutExtension(document.FileName)}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            var outputPath = Path.Combine(signedPath, outputFileName);

            using (var stream = new FileStream(outputPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // ✅ Cập nhật document
            document.SignedFilePath = outputPath;
            document.IsSigned = true;
            document.Status = DocumentStatus.FullySigned;
            document.SignedAt = DateTime.UtcNow;

            // ✅ Ghi log chữ ký (dạng ký tay)
            var signature = new Signature
            {
                DocumentId = id,
                SignedByUserId = userId,
                SignerName = User.FindFirstValue(ClaimTypes.Name) ?? "Unknown User",
                Reason = "Handwritten Signature from WPF Client",
                Location = "WPF Client",
                SignatureData = "HandwrittenSignature",
                SignedAt = DateTime.UtcNow,
                IsValid = true
            };

            _context.Signatures.Add(signature);
            await _context.SaveChangesAsync();

            _logger.LogInformation("🖋️ Hand-signed PDF uploaded for document {Id} by user {UserId}", id, userId);

            return Ok(new
            {
                message = "Signed PDF uploaded successfully",
                signedFilePath = outputPath,
                signedAt = document.SignedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error uploading signed PDF for document {Id}", id);
            return StatusCode(500, new { message = $"Error uploading signed file: {ex.Message}" });
        }
    }


    // === Get all documents ===
    [HttpGet]
    public async Task<IActionResult> GetDocuments()
    {
        var userId = GetCurrentUserId();
        var documents = await _context.Documents
            .Include(d => d.Signatures)
            .Where(d => d.UploadedByUserId == userId)
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new
            {
                d.Id,
                d.FileName,
                d.OriginalFileName,
                d.FileSize,
                d.ContentType,
                d.IsSigned,
                d.Status,
                d.UploadedAt,
                d.SignedAt,
                d.Description,
                SignatureCount = d.Signatures.Count,
                UploadedByUsername = d.UploadedBy.Username
            })
            .ToListAsync();

        return Ok(documents);
    }

    // === Get document by ID ===
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDocument(Guid id)
    {
        var userId = GetCurrentUserId();
        var document = await _context.Documents
            .Include(d => d.Signatures)
            .FirstOrDefaultAsync(d => d.Id == id && d.UploadedByUserId == userId);

        if (document == null)
            return NotFound(new { message = "Document not found" });

        return Ok(new
        {
            document.Id,
            document.FileName,
            document.OriginalFileName,
            document.FileSize,
            document.ContentType,
            document.IsSigned,
            document.Status,
            document.UploadedAt,
            document.SignedAt,
            document.Description,

            Signatures = document.Signatures.Select(s => new
            {
                s.Id,
                s.SignerName,
                s.SignedAt,
                s.Reason,
                s.Location,
                s.IsValid
            })
        });
    }

    // === Sign a document (accepts PFX file) ===
    [HttpPost("{id:guid}/sign")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> SignDocument(Guid id, [FromForm] SignDocumentForm form)
    {
        var userId = GetCurrentUserId();
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.UploadedByUserId == userId);

        if (document == null)
            return NotFound(new { message = "Document not found" });

        if (document.IsSigned)
            return BadRequest(new { message = "Document is already signed" });

        try
        {
            using var ms = new MemoryStream();
            await form.CertificateFile.CopyToAsync(ms);
            var certBytes = ms.ToArray();

            var signedPath = _configuration["FileSettings:SignedPath"] ?? "Signed";
            Directory.CreateDirectory(signedPath);

            var outputFileName = $"signed_{Path.GetFileNameWithoutExtension(document.FileName)}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            var outputPath = Path.Combine(signedPath, outputFileName);

            await _signingService.SignPdfAsync(
                document.StoragePath,
                outputPath,
                certBytes,
                form.CertificatePassword,
                new SignDocumentRequest
                {
                    SignerName = form.SignerName,
                    Reason = form.Reason,
                    Location = form.Location
                });

            document.SignedFilePath = outputPath;
            document.Status = DocumentStatus.FullySigned;
            document.IsSigned = true;
            document.SignedAt = DateTime.UtcNow;

            var signature = new Signature
            {
                DocumentId = id,
                SignedByUserId = userId,
                SignerName = form.SignerName ?? User.FindFirstValue(ClaimTypes.Name) ?? "Unknown",
                Reason = form.Reason,
                Location = form.Location,
                SignatureData = "Digital Signature",
                SignedAt = DateTime.UtcNow,
                IsValid = true
            };

            _context.Signatures.Add(signature);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Document signed successfully",
                signatureId = signature.Id,
                signedFilePath = outputPath,
                signedAt = signature.SignedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error signing document {Id}", id);
            return StatusCode(500, new { message = $"Error signing document: {ex.Message}" });
        }
    }

    // === Verify signature ===
    [HttpPost("{id:guid}/verify")]
    public async Task<IActionResult> VerifySignature(Guid id)
    {
        var userId = GetCurrentUserId();
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.UploadedByUserId == userId);

        if (document == null)
            return NotFound(new { message = "Document not found" });

        if (!document.IsSigned || string.IsNullOrEmpty(document.SignedFilePath))
            return BadRequest(new { message = "Document is not signed" });

        if (!System.IO.File.Exists(document.SignedFilePath))
            return NotFound(new { message = "Signed file not found" });

        try
        {
            var result = await _signingService.VerifyPdfSignaturesAsync(document.SignedFilePath);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error verifying signature for document {Id}", id);
            return StatusCode(500, new { message = $"Error verifying signature: {ex.Message}" });
        }
    }

    // === Download document ===
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> DownloadDocument(Guid id, [FromQuery] bool signed = false)
    {
        var userId = GetCurrentUserId();
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.UploadedByUserId == userId);

        if (document == null)
            return NotFound(new { message = "Document not found" });

        string filePath = signed && !string.IsNullOrEmpty(document.SignedFilePath)
            ? document.SignedFilePath!
            : document.StoragePath;

        if (!System.IO.File.Exists(filePath))
            return NotFound(new { message = "File not found" });

        var fileName = signed ? $"signed_{document.FileName}" : document.FileName;
        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

        return File(fileBytes, document.ContentType, fileName);
    }

    // === Delete document ===
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        var userId = GetCurrentUserId();
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.UploadedByUserId == userId);

        if (document == null)
            return NotFound(new { message = "Document not found" });

        if (System.IO.File.Exists(document.StoragePath))
            System.IO.File.Delete(document.StoragePath);

        if (!string.IsNullOrEmpty(document.SignedFilePath) && System.IO.File.Exists(document.SignedFilePath))
            System.IO.File.Delete(document.SignedFilePath);

        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();

        _logger.LogInformation("🗑️ Document {Id} deleted by user {UserId}", id, userId);

        return Ok(new { message = "Document deleted successfully" });
    }
}
