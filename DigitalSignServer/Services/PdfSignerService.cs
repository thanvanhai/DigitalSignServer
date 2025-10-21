using DigitalSignServer.Models.Dto;
using iText.Bouncycastle.Crypto;
using iText.Bouncycastle.X509;
using iText.Forms.Form.Element;
using iText.Kernel.Crypto;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Signatures;
using System.Security.Cryptography.X509Certificates;

namespace DigitalSignServer.Services;

/// <summary>
/// Định nghĩa các dịch vụ ký và xác minh chữ ký số PDF.
/// </summary>
public interface IPdfSignerService
{
    /// <summary>
    /// Ký số file PDF bằng chứng thư (.pfx).
    /// </summary>
    /// <param name="inputPath">Đường dẫn file PDF gốc.</param>
    /// <param name="outputPath">Đường dẫn file PDF sau khi ký.</param>
    /// <param name="certData">Dữ liệu chứng thư (.pfx) dạng byte.</param>
    /// <param name="certPassword">Mật khẩu của file chứng thư.</param>
    /// <param name="request">Thông tin ký: tên, lý do, vị trí,…</param>
    Task<string> SignPdfAsync(string inputPath, string outputPath, byte[] certData, string certPassword, SignDocumentRequest request);

    /// <summary>
    /// Xác minh tất cả chữ ký trong file PDF.
    /// </summary>
    Task<VerifySignatureResponse> VerifyPdfSignaturesAsync(string pdfPath);

    /// <summary>
    /// Đọc thông tin chứng thư số (.pfx) từ đường dẫn.
    /// </summary>
    Task<X509Certificate2?> GetCertificateInfoAsync(string certificatePath, string password);
}

/// <summary>
/// Triển khai dịch vụ ký PDF bằng iText 9.3 và BouncyCastle.
/// </summary>
public class PdfSignerService : IPdfSignerService
{
    /// <summary>
    /// Ký số file PDF sử dụng chứng thư (.pfx).
    /// </summary>
    public async Task<string> SignPdfAsync(string inputPath, string outputPath, byte[]? certData, string certPassword, SignDocumentRequest request)
    {
        return await Task.Run(() =>
        {
            // ===============================
            // 1️⃣ NẠP CHỨNG THƯ (.pfx)
            // ===============================
            var pkcs12Store = new Org.BouncyCastle.Pkcs.Pkcs12StoreBuilder().Build();
            using (var certStream = new MemoryStream(certData))
            {
                pkcs12Store.Load(certStream, certPassword.ToCharArray());
            }

            // Tìm alias chứa private key
            string? alias = null;
            foreach (string a in pkcs12Store.Aliases)
            {
                if (pkcs12Store.IsKeyEntry(a))
                {
                    alias = a;
                    break;
                }
            }

            if (alias == null)
                throw new InvalidOperationException("No private key found in certificate");

            // Lấy private key + chuỗi chứng thư
            var privateKeyEntry = pkcs12Store.GetKey(alias);
            var certChain = pkcs12Store.GetCertificateChain(alias);

            // Bọc lại bằng adapter của iText
            var pk = new PrivateKeyBC(privateKeyEntry.Key);
            var chain = new iText.Commons.Bouncycastle.Cert.IX509Certificate[certChain.Length];
            for (int i = 0; i < certChain.Length; i++)
                chain[i] = new X509CertificateBC(certChain[i].Certificate);

            // ===============================
            // 2️⃣ TẠO PDF SIGNER
            // ===============================
            using var reader = new PdfReader(inputPath);
            using var writer = new FileStream(outputPath, FileMode.Create);
            var signer = new PdfSigner(reader, writer, new StampingProperties());

            // Tạo tên trường chữ ký duy nhất
            string fieldName = $"Signature_{DateTime.Now:yyyyMMddHHmmss}";

            // ===============================
            // 3️⃣ CẤU HÌNH GIAO DIỆN CHỮ KÝ
            // ===============================
            var appearance = new SignatureFieldAppearance(fieldName);
            appearance.SetContent(request.SignerName ?? "Digital Signature");

            // Cấu hình vùng hiển thị (tọa độ trên trang)
            var signerProperties = new SignerProperties()
                .SetFieldName(fieldName)
                .SetPageNumber(1) // Trang đầu tiên
                .SetPageRect(new Rectangle(36, 648, 200, 100)) // Toạ độ vùng ký (x, y, w, h)
                .SetSignatureAppearance(appearance);

            // Gán thêm thông tin meta: lý do & vị trí
            if (!string.IsNullOrEmpty(request.Reason))
                signerProperties.SetReason(request.Reason);

            if (!string.IsNullOrEmpty(request.Location))
                signerProperties.SetLocation(request.Location);

            signer.SetSignerProperties(signerProperties);

            // ===============================
            // 4️⃣ THỰC HIỆN KÝ
            // ===============================
            IExternalSignature externalSignature = new PrivateKeySignature(pk, DigestAlgorithms.SHA256);

            // Ký detached (phù hợp chuẩn PDF/A)
            signer.SignDetached(externalSignature, chain, null, null, null, 0, PdfSigner.CryptoStandard.CMS);

            // Trả về đường dẫn file kết quả
            return outputPath;
        });
    }

    /// <summary>
    /// Xác minh tất cả các chữ ký trong file PDF.
    /// </summary>
    public async Task<VerifySignatureResponse> VerifyPdfSignaturesAsync(string pdfPath)
    {
        return await Task.Run(() =>
        {
            var response = new VerifySignatureResponse
            {
                IsValid = false,
                Message = "No signatures found",
                Signatures = new List<SignatureInfo>()
            };

            try
            {
                // ===============================
                // 1️⃣ ĐỌC FILE PDF
                // ===============================
                using var reader = new PdfReader(pdfPath);
                using var document = new PdfDocument(reader);

                var signUtil = new SignatureUtil(document);
                var signatureNames = signUtil.GetSignatureNames();

                if (!signatureNames.Any())
                    return response;

                bool allValid = true;

                // ===============================
                // 2️⃣ DUYỆT TỪNG CHỮ KÝ
                // ===============================
                foreach (var name in signatureNames)
                {
                    PdfSignature pdfSignature = signUtil.GetSignature(name);
                    PdfPKCS7 pkcs7 = signUtil.ReadSignatureData(name);

                    var signerCert = pkcs7.GetSigningCertificate();
                    var signerName = signerCert?.GetSubjectDN()?.ToString() ?? "Unknown";
                    var signDate = pkcs7.GetSignDate();

                    // Kiểm tra tính hợp lệ của chữ ký
                    bool isValid = pkcs7.VerifySignatureIntegrityAndAuthenticity();

                    if (!isValid)
                        allValid = false;

                    // Lưu thông tin chữ ký
                    var signatureInfo = new SignatureInfo
                    {
                        SignerName = signerName,
                        SignedAt = signDate,
                        IsValid = isValid,
                        CertificateInfo = signerCert?.GetSubjectDN()?.ToString(),
                        Reason = pdfSignature.GetReason()?.ToString(),
                        Location = pdfSignature.GetLocation()?.ToString()
                    };

                    response.Signatures.Add(signatureInfo);
                }

                // ===============================
                // 3️⃣ TỔNG KẾT KẾT QUẢ
                // ===============================
                response.IsValid = allValid;
                response.Message = allValid ? "All signatures are valid" : "Some signatures are invalid";
            }
            catch (Exception ex)
            {
                response.Message = $"Error verifying signatures: {ex.Message}";
            }

            return response;
        });
    }

    /// <summary>
    /// Đọc thông tin chứng thư (.pfx), phục vụ hiển thị trước khi ký.
    /// </summary>
    public async Task<X509Certificate2?> GetCertificateInfoAsync(string certificatePath, string password)
    {
        return await Task.Run(() =>
        {
            try
            {
                return new X509Certificate2(certificatePath, password);
            }
            catch
            {
                return null;
            }
        });
    }
}
