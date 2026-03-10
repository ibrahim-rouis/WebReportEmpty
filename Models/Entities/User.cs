using System.ComponentModel.DataAnnotations;

namespace WebReport.Models.Entities
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string? Name { get; set; }

        public List<Role>? Roles { get; set; }
    }
}
