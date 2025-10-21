using System.ComponentModel.DataAnnotations;

namespace DigitalSignServer.Models.Dto
{
    /// <summary>
    /// Request thông tin ký số — không chứa file chứng thư nữa.
    /// File .pfx sẽ được truyền qua [FromForm] IFormFile.
    /// </summary>
    public class SignDocumentRequest
    {
        /// <summary> File chứng thư số (.pfx) </summary>
        public IFormFile CertificateFile { get; set; }
        /// <summary> Mật khẩu chứng thư số (.pfx) </summary>
        public string CertificatePassword { get; set; } = string.Empty;
        /// <summary> Tên người ký (hiển thị trong chữ ký PDF). </summary>
        public string? SignerName { get; set; }

        /// <summary> Lý do ký (tùy chọn). </summary>
        public string? Reason { get; set; }

        /// <summary> Địa điểm ký (tùy chọn). </summary>
        public string? Location { get; set; }

        // Tùy chọn hiển thị chữ ký trên PDF
        public bool VisibleSignature { get; set; } = false;
        public int? PageNumber { get; set; }
        public float? X { get; set; }
        public float? Y { get; set; }
    }
    public class SignDocumentForm
    {
        [Required]
        public IFormFile CertificateFile { get; set; } = null!;

        [Required]
        public string CertificatePassword { get; set; } = string.Empty;

        public string? SignerName { get; set; }
        public string? Reason { get; set; }
        public string? Location { get; set; }
    }
    public class VerifySignatureResponse
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<SignatureInfo> Signatures { get; set; } = new();
    }

    public class SignatureInfo
    {
        public string SignerName { get; set; } = string.Empty;
        public DateTime SignedAt { get; set; }
        public string? Reason { get; set; }
        public string? Location { get; set; }
        public bool IsValid { get; set; }
        public string? CertificateInfo { get; set; }
    }

}
