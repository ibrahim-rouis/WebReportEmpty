using System.ComponentModel.DataAnnotations;

namespace WebReport.Models.Entities
{
    public class Role
    {
        public int Id { get; set; }

        [Required]
        public string? Name { get; set; }

        public List<User>? Users { get; set; }
    }
}
