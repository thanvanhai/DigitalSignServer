using Microsoft.EntityFrameworkCore;
using DigitalSignServer.Data;
using DigitalSignServer.Models;

namespace DigitalSignServer.Services;

public class WorkflowService
{
    private readonly AppDbContext _context;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(AppDbContext context, ILogger<WorkflowService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Tạo workflow instance cho tài liệu
    /// </summary>
    public async Task<DocumentWorkflow> CreateWorkflowForDocumentAsync(Guid documentId, Guid workflowTemplateId)
    {
        var template = await _context.WorkflowTemplates
            .Include(t => t.Steps.OrderBy(s => s.Level))
            .FirstOrDefaultAsync(t => t.Id == workflowTemplateId);

        if (template == null) throw new Exception("Workflow template not found");
        if (!template.IsActive) throw new Exception("Workflow template is inactive");
        if (!template.Steps.Any()) throw new Exception("Workflow template has no steps");

        // Tạo workflow instance
        var workflow = new DocumentWorkflow
        {
            DocumentId = documentId,
            WorkflowTemplateId = workflowTemplateId,
            CurrentLevel = 1,
            Status = DocumentStatus.PendingSignature.ToString()
        };

        _context.DocumentWorkflows.Add(workflow);
        await _context.SaveChangesAsync();

        // Tạo approval history cho tất cả các bước
        foreach (var step in template.Steps)
        {
            var approvalHistory = new ApprovalHistory
            {
                DocumentWorkflowId = workflow.Id,
                WorkflowStepId = step.Id,
                SignedByUserId = Guid.Empty, // chưa ai ký
                Status = "Pending"
            };
            _context.ApprovalHistories.Add(approvalHistory);
        }

        await _context.SaveChangesAsync();
        return workflow;
    }

    /// <summary>
    /// Ký/Approve bước hiện tại
    /// </summary>
    public async Task<bool> ApproveStepAsync(Guid workflowId, Guid userId, Guid signatureId, string? note = null)
    {
        var workflow = await _context.DocumentWorkflows
            .Include(w => w.WorkflowTemplate)
                .ThenInclude(t => t.Steps)
            .Include(w => w.ApprovalHistories)
            .Include(w => w.Document)
            .FirstOrDefaultAsync(w => w.Id == workflowId);

        if (workflow == null) throw new Exception("Workflow not found");
        if (workflow.Status != DocumentStatus.PendingSignature.ToString() && workflow.Status != DocumentStatus.Uploaded.ToString())
            throw new Exception("Workflow is not pending signature");

        var currentStep = workflow.WorkflowTemplate.Steps
            .FirstOrDefault(s => s.Level == workflow.CurrentLevel);
        if (currentStep == null) throw new Exception("Current step not found");

        var approvalHistory = workflow.ApprovalHistories
            .FirstOrDefault(h => h.WorkflowStepId == currentStep.Id);
        if (approvalHistory == null) throw new Exception("Approval history not found");
        if (approvalHistory.Status != "Pending") throw new Exception("Step already processed");

        // Cập nhật approval history
        approvalHistory.Status = "Ký OK";
        approvalHistory.SignedByUserId = userId;
        approvalHistory.SignedAt = DateTime.UtcNow;
        approvalHistory.Note = note;
        //approvalHistory.SignatureId = signatureId;

        // Chuyển sang bước tiếp theo
        var nextStep = workflow.WorkflowTemplate.Steps
            .FirstOrDefault(s => s.Level == workflow.CurrentLevel + 1);

        if (nextStep != null)
        {
            workflow.CurrentLevel++;
            workflow.Status = DocumentStatus.PendingSignature.ToString();
        }
        else
        {
            workflow.Status = DocumentStatus.FullySigned.ToString();
            workflow.Document.IsSigned = true;
            workflow.Document.SignedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation($"Workflow {workflowId}: step {currentStep.Level} approved by user {userId}");
        return true;
    }

    /// <summary>
    /// Từ chối bước ký hiện tại
    /// </summary>
    public async Task<bool> RejectStepAsync(Guid workflowId, Guid userId, string reason)
    {
        var workflow = await _context.DocumentWorkflows
            .Include(w => w.WorkflowTemplate)
                .ThenInclude(t => t.Steps)
            .Include(w => w.ApprovalHistories)
            .Include(w => w.Document)
            .FirstOrDefaultAsync(w => w.Id == workflowId);

        if (workflow == null) throw new Exception("Workflow not found");
        if (workflow.Status != DocumentStatus.PendingSignature.ToString())
            throw new Exception("Workflow is not pending");

        var currentStep = workflow.WorkflowTemplate.Steps
            .FirstOrDefault(s => s.Level == workflow.CurrentLevel);
        if (currentStep == null) throw new Exception("Current step not found");

        var approvalHistory = workflow.ApprovalHistories
            .FirstOrDefault(h => h.WorkflowStepId == currentStep.Id);
        if (approvalHistory == null) throw new Exception("Approval history not found");

        approvalHistory.Status = "Từ chối";
        approvalHistory.SignedByUserId = userId;
        approvalHistory.SignedAt = DateTime.UtcNow;
        approvalHistory.Note = reason;

        workflow.Status = DocumentStatus.Uploaded.ToString(); // hoặc tạo enum Rejected nếu muốn
        workflow.Document.IsSigned = false;

        await _context.SaveChangesAsync();
        _logger.LogInformation($"Workflow {workflowId}: step {currentStep.Level} rejected by user {userId}");
        return true;
    }

    /// <summary>
    /// Lấy danh sách workflow đang chờ user ký
    /// </summary>
    public async Task<List<DocumentWorkflow>> GetPendingApprovalsForUserAsync(Guid userId)
    {
        return await _context.DocumentWorkflows
            .Include(w => w.Document)
            .Include(w => w.WorkflowTemplate)
                .ThenInclude(t => t.Steps)
            .Include(w => w.ApprovalHistories)
            .Where(w =>
                w.Status == DocumentStatus.PendingSignature.ToString() &&
                w.WorkflowTemplate.Steps.Any(s =>
                    s.Level == w.CurrentLevel &&
                    w.ApprovalHistories.Any(h => h.WorkflowStepId == s.Id && h.SignedByUserId == Guid.Empty)
                )
            )
            .OrderBy(w => w.Document.UploadedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Lấy trạng thái workflow chi tiết
    /// </summary>
    public async Task<DocumentWorkflow?> GetWorkflowStatusAsync(Guid documentId)
    {
        return await _context.DocumentWorkflows
            .Include(w => w.Document)
            .Include(w => w.WorkflowTemplate)
                .ThenInclude(t => t.Steps)
            .Include(w => w.ApprovalHistories)
            .FirstOrDefaultAsync(w => w.DocumentId == documentId);
    }
}
