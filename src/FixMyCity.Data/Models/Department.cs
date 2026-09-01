using System.ComponentModel.DataAnnotations;

namespace FixMyCity.Data.Models
{
    public class Department
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public List<Category> Categories { get; set; } = new();
    }
}