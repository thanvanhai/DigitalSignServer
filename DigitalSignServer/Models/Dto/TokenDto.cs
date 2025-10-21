using System.ComponentModel.DataAnnotations;

namespace DigitalSignServer.Models.Dto
{
    public class TokenInfo
    {
        public string Name { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
    }

    public class VerifyPinRequest
    {
        [Required]
        public string Pin { get; set; } = string.Empty;
    }
}
