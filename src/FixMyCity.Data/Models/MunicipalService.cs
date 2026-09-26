using System.ComponentModel.DataAnnotations;

namespace FixMyCity.Data.Models
{
    public class MunicipalService
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string NameBn { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = "Electricity"; // Electricity, Gas, Water, CityTax, MunicipalFee, Other

        public string? Description { get; set; }
        public string? DescriptionBn { get; set; }

        [MaxLength(50)]
        public string IconClass { get; set; } = "bi-building";

        public bool IsActive { get; set; } = true;

        public List<MunicipalProvider> Providers { get; set; } = new();
    }
}
