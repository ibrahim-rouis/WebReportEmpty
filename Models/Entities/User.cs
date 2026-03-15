using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WebReport.Models.Entities
{
    // Set a unique index on the Name property
    [Index(nameof(Name), IsUnique = true)]
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string? Name { get; set; }

        public List<Role>? Roles { get; set; }
    }
}
