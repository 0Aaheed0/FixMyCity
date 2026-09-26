using System.ComponentModel.DataAnnotations;

namespace FixMyCity.Data.Models
{
    public class MunicipalProvider
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // e.g. DPDC, DESCO, Titas Gas, WASA, DNCC Tax

        [Required]
        [MaxLength(100)]
        public string NameBn { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty; // DPDC, DESCO, TITAS, WASA, DNCC_TAX, DSCC_TAX

        public int ServiceId { get; set; }
        public MunicipalService? Service { get; set; }

        public string? Logo { get; set; }

        [Required]
        [MaxLength(100)]
        public string AccountNumberLabel { get; set; } = "Account Number / Meter No";

        [Required]
        [MaxLength(100)]
        public string AccountNumberLabelBn { get; set; } = "Account Number / Meter No";

        public bool IsActive { get; set; } = true;

        public List<CitizenBill> Bills { get; set; } = new();
    }
}
