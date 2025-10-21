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
        }
    }
}