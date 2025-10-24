using DigitalSignServer.Data;
using DigitalSignServer.Services;
using Microsoft.AspNetCore.Mvc;


[Route("api/[controller]")]
[ApiController]
public class DocumentWorkflowController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<DocumentWorkflowController> _logger;
    private readonly WorkflowService _workflowService;

    public DocumentWorkflowController(AppDbContext context, ILogger<DocumentWorkflowController> logger, WorkflowService workflowService)
    {
        _context = context;
        _logger = logger;
        _workflowService = workflowService;
    }

    [HttpPost("Create")]
    public async Task<IActionResult> CreateWorkflow(Guid documentId, Guid workflowTemplateId)
    {
        var workflow = await _workflowService.CreateWorkflowForDocumentAsync(documentId, workflowTemplateId);
        return Ok(workflow);
    }

    [HttpPost("{workflowId}/Approve")]
    public async Task<IActionResult> ApproveStep(Guid workflowId, Guid userId, Guid signatureId, string? note = null)
    {
        var result = await _workflowService.ApproveStepAsync(workflowId, userId, signatureId, note);
        return Ok(result);
    }

    [HttpPost("{workflowId}/Reject")]
    public async Task<IActionResult> RejectStep(Guid workflowId, Guid userId, string reason)
    {
        var result = await _workflowService.RejectStepAsync(workflowId, userId, reason);
        return Ok(result);
    }

    [HttpGet("{documentId}/Status")]
    public async Task<IActionResult> GetStatus(Guid documentId)
    {
        var workflow = await _workflowService.GetWorkflowStatusAsync(documentId);
        if (workflow == null) return NotFound();
        return Ok(workflow);
    }

    [HttpGet("Pending/{userId}")]
    public async Task<IActionResult> GetPending(Guid userId)
    {
        var list = await _workflowService.GetPendingApprovalsForUserAsync(userId);
        return Ok(list);
    }
}
