using DigitalSignServer.Models;
using Microsoft.EntityFrameworkCore;

namespace DigitalSignServer.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Document> Documents => Set<Document>();
        public DbSet<Signature> Signatures => Set<Signature>();

        // --- Workflow System ---
        public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
        public DbSet<WorkflowTemplate> WorkflowTemplates => Set<WorkflowTemplate>();
        public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();
        public DbSet<WorkflowConnection> WorkflowConnections => Set<WorkflowConnection>(); // ✅ MỚI THÊM
        public DbSet<DocumentWorkflow> DocumentWorkflows => Set<DocumentWorkflow>();
        public DbSet<ApprovalHistory> ApprovalHistories => Set<ApprovalHistory>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============================================
            // USER CONFIGURATION
            // ============================================
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // ============================================
            // DOCUMENT CONFIGURATION
            // ============================================
            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasOne(d => d.UploadedBy)
                    .WithMany(u => u.Documents)
                    .HasForeignKey(d => d.UploadedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.DocumentType)
                    .WithMany(dt => dt.Documents)
                    .HasForeignKey(d => d.DocumentTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Workflow)
                    .WithOne(w => w.Document)
                    .HasForeignKey<DocumentWorkflow>(w => w.DocumentId);

                entity.HasIndex(e => e.Hash);
            });

            // ============================================
            // SIGNATURE CONFIGURATION
            // ============================================
            modelBuilder.Entity<Signature>(entity =>
            {
                entity.HasOne(s => s.Document)
                    .WithMany(d => d.Signatures)
                    .HasForeignKey(s => s.DocumentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(s => s.SignedByUser)
                    .WithMany(u => u.Signatures)
                    .HasForeignKey(s => s.SignedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================
            // DOCUMENT TYPE CONFIGURATION
            // ============================================
            modelBuilder.Entity<DocumentType>(entity =>
            {
                entity.HasMany(dt => dt.WorkflowTemplates)
                    .WithOne(wt => wt.DocumentType)
                    .HasForeignKey(wt => wt.DocumentTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================
            // WORKFLOW TEMPLATE CONFIGURATION
            // ============================================
            modelBuilder.Entity<WorkflowTemplate>(entity =>
            {
                entity.HasMany(wt => wt.Steps)
                    .WithOne(s => s.WorkflowTemplate)
                    .HasForeignKey(s => s.WorkflowTemplateId)
                    .OnDelete(DeleteBehavior.Cascade);

                // ✅ MỚI: Relationship với WorkflowConnections
                entity.HasMany(wt => wt.Connections)
                    .WithOne(c => c.WorkflowTemplate)
                    .HasForeignKey(c => c.WorkflowTemplateId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================
            // WORKFLOW STEP CONFIGURATION
            // ============================================
            modelBuilder.Entity<WorkflowStep>(entity =>
            {
                entity.Property(s => s.Level).IsRequired();
                entity.Property(s => s.Role).IsRequired().HasMaxLength(100);
                entity.Property(s => s.SignatureType).IsRequired().HasMaxLength(50);
                entity.Property(s => s.NodeType).HasMaxLength(50);
                entity.Property(s => s.Description).HasMaxLength(500);

                // ✅ MỚI: Indexes cho performance
                entity.HasIndex(s => s.WorkflowTemplateId);
                entity.HasIndex(s => s.Level);
            });

            // ============================================
            // ✅ WORKFLOW CONNECTION CONFIGURATION (MỚI)
            // ============================================
            modelBuilder.Entity<WorkflowConnection>(entity =>
            {
                // Primary Key
                entity.HasKey(c => c.Id);

                // Relationship: WorkflowConnection -> WorkflowTemplate
                entity.HasOne(c => c.WorkflowTemplate)
                    .WithMany(t => t.Connections)
                    .HasForeignKey(c => c.WorkflowTemplateId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relationship: WorkflowConnection -> SourceStep
                entity.HasOne(c => c.SourceStep)
                    .WithMany(s => s.OutgoingConnections)
                    .HasForeignKey(c => c.SourceStepId)
                    .OnDelete(DeleteBehavior.Restrict); // ⚠️ QUAN TRỌNG: Restrict để tránh cascade conflict

                // Relationship: WorkflowConnection -> TargetStep
                entity.HasOne(c => c.TargetStep)
                    .WithMany(s => s.IncomingConnections)
                    .HasForeignKey(c => c.TargetStepId)
                    .OnDelete(DeleteBehavior.Restrict); // ⚠️ QUAN TRỌNG: Restrict

                // Properties
                entity.Property(c => c.Condition).HasMaxLength(50);
                entity.Property(c => c.Label).HasMaxLength(200);
                entity.Property(c => c.Priority).HasDefaultValue(0);

                // Indexes cho performance
                entity.HasIndex(c => new { c.SourceStepId, c.TargetStepId })
                    .HasDatabaseName("IX_WorkflowConnection_SourceTarget");

                entity.HasIndex(c => c.WorkflowTemplateId)
                    .HasDatabaseName("IX_WorkflowConnection_Template");

                entity.HasIndex(c => c.SourceStepId)
                    .HasDatabaseName("IX_WorkflowConnection_Source");

                entity.HasIndex(c => c.TargetStepId)
                    .HasDatabaseName("IX_WorkflowConnection_Target");

                // ✅ Constraint: Không cho phép kết nối node với chính nó
                entity.HasCheckConstraint("CK_WorkflowConnection_NoSelfLoop",
                    "[SourceStepId] <> [TargetStepId]");
            });

            // ============================================
            // DOCUMENT WORKFLOW CONFIGURATION
            // ============================================
            modelBuilder.Entity<DocumentWorkflow>(entity =>
            {
                entity.HasOne(dw => dw.WorkflowTemplate)
                    .WithMany()
                    .HasForeignKey(dw => dw.WorkflowTemplateId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(dw => dw.ApprovalHistories)
                    .WithOne(ah => ah.DocumentWorkflow)
                    .HasForeignKey(ah => ah.DocumentWorkflowId)
                    .OnDelete(DeleteBehavior.Cascade);

                // ✅ Index cho CurrentStepId (để query nhanh)
                entity.HasIndex(dw => dw.CurrentStepId);
            });

            // ============================================
            // APPROVAL HISTORY CONFIGURATION
            // ============================================
            modelBuilder.Entity<ApprovalHistory>(entity =>
            {
                entity.HasOne(ah => ah.WorkflowStep)
                    .WithMany()
                    .HasForeignKey(ah => ah.WorkflowStepId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ah => ah.SignedBy)
                    .WithMany()
                    .HasForeignKey(ah => ah.SignedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ah => ah.DocumentWorkflow)
                    .WithMany(dw => dw.ApprovalHistories)
                    .HasForeignKey(ah => ah.DocumentWorkflowId)
                    .OnDelete(DeleteBehavior.Cascade);

                // ✅ Indexes
                entity.HasIndex(ah => ah.DocumentWorkflowId);
                entity.HasIndex(ah => ah.WorkflowStepId);
                entity.HasIndex(ah => ah.SignedByUserId);
                entity.HasIndex(ah => ah.SignedAt);
            });
        }
    }
}