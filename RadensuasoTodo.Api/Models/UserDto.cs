using System.ComponentModel.DataAnnotations;

namespace RadensuasoTodo.Api.Models
{
    public class UserDto
    {
        public int Id { get; set; }

        [Required]
        public required string Username { get; set; }

        [Required]
        public required string Password { get; set; }
    }
}
