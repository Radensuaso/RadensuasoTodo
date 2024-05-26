using System.ComponentModel.DataAnnotations;

namespace RadensuasoTodo.Api.Models
{
    public class TodoItem
    {
        public int Id { get; set; }

        [Required]
        public required string Name { get; set; }

        public bool IsComplete { get; set; }

        public int UserId { get; set; }

        public User? User { get; set; }
    }
}
