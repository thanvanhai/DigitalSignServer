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

        // --- Bổ sung workflow ---
        public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
        public DbSet<WorkflowTemplate> WorkflowTemplates => Set<WorkflowTemplate>();
        public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();
        public DbSet<DocumentWorkflow> DocumentWorkflows => Set<DocumentWorkflow>();
        public DbSet<ApprovalHistory> ApprovalHistories => Set<ApprovalHistory>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Document configuration
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

            // Signature configuration
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

            // DocumentType configuration
            modelBuilder.Entity<DocumentType>(entity =>
            {
                entity.HasMany(dt => dt.WorkflowTemplates)
                    .WithOne(wt => wt.DocumentType)
                    .HasForeignKey(wt => wt.DocumentTypeId);

                entity.HasMany(dt => dt.Documents)
                    .WithOne(d => d.DocumentType)
                    .HasForeignKey(d => d.DocumentTypeId);
            });

            // WorkflowTemplate configuration
            modelBuilder.Entity<WorkflowTemplate>(entity =>
            {
                entity.HasMany(wt => wt.Steps)
                    .WithOne(s => s.WorkflowTemplate)
                    .HasForeignKey(s => s.WorkflowTemplateId);
            });

            // WorkflowStep configuration
            modelBuilder.Entity<WorkflowStep>(entity =>
            {
                entity.Property(s => s.Level).IsRequired();
            });

            // DocumentWorkflow configuration
            modelBuilder.Entity<DocumentWorkflow>(entity =>
            {
                entity.HasOne(dw => dw.WorkflowTemplate)
                    .WithMany()
                    .HasForeignKey(dw => dw.WorkflowTemplateId);

                entity.HasMany(dw => dw.ApprovalHistories)
                    .WithOne(ah => ah.DocumentWorkflow)
                    .HasForeignKey(ah => ah.DocumentWorkflowId);
            });

            // ApprovalHistory configuration
            modelBuilder.Entity<ApprovalHistory>(entity =>
            {
                entity.HasOne(ah => ah.WorkflowStep)
                    .WithMany()
                    .HasForeignKey(ah => ah.WorkflowStepId)
                    .OnDelete(DeleteBehavior.Restrict); // bỏ cascade

                entity.HasOne(ah => ah.SignedBy)
                    .WithMany()
                    .HasForeignKey(ah => ah.SignedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ah => ah.DocumentWorkflow)
                    .WithMany(dw => dw.ApprovalHistories)
                    .HasForeignKey(ah => ah.DocumentWorkflowId)
                    .OnDelete(DeleteBehavior.Cascade); // giữ cascade cho DocumentWorkflow
            });
        }
    }
}
