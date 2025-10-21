using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigitalSignServer.Models;

public class Signature
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string SignatureData { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? CertificateInfo { get; set; }

    [MaxLength(100)]
    public string? SignerName { get; set; }

    [MaxLength(255)]
    public string? Reason { get; set; }

    [MaxLength(255)]
    public string? Location { get; set; }

    public DateTime SignedAt { get; set; } = DateTime.UtcNow;

    public bool IsValid { get; set; } = true;

    [MaxLength(50)]
    public string SignatureType { get; set; } = "Digital";

    // Foreign Keys
    [ForeignKey(nameof(Document))]
    public Guid DocumentId { get; set; }

    [ForeignKey(nameof(SignedByUser))]
    public Guid SignedByUserId { get; set; }

    // Navigation properties
    public virtual Document Document { get; set; } = null!;
    public virtual User SignedByUser { get; set; } = null!;
}