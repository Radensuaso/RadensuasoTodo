using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RadensuasoTodo.Api.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public required string Username { get; set; }

        [Required]
        public required string PasswordHash { get; set; }

        public ICollection<TodoItem> TodoItems { get; set; } = [];
    }
}
