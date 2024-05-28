using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RadensuasoTodo.Api.Models;

namespace RadensuasoTodo.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TodoItemsController : ControllerBase
    {
        private readonly TodoContext _context;
        private readonly ILogger<TodoItemsController> _logger;

        public TodoItemsController(TodoContext context, ILogger<TodoItemsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/TodoItems
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetTodoItems()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;

            if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Invalid User ID format in claims.");
                return Unauthorized("Invalid User ID format in claims.");
            }

            _logger.LogInformation("Fetching Todo items for user: {UserId}", userId);
            var todoItems = await _context.TodoItems.Where(todo => todo.UserId == userId).ToListAsync();
            return todoItems;
        }

        // GET: api/TodoItems/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TodoItem>> GetTodoItem(int id)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Invalid User ID format in claims.");
                return Unauthorized("Invalid User ID format in claims.");
            }

            _logger.LogInformation("Fetching Todo item {TodoId} for user: {UserId}", id, userId);
            var todoItem = await _context.TodoItems.SingleOrDefaultAsync(todo => todo.Id == id && todo.UserId == userId);

            if (todoItem == null)
            {
                _logger.LogWarning("Todo item not found: {TodoId}", id);
                return NotFound();
            }

            return todoItem;
        }

        // PUT: api/TodoItems/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTodoItem(int id, TodoItem todoItem)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Invalid User ID format in claims.");
                return Unauthorized("Invalid User ID format in claims.");
            }

            if (id != todoItem.Id || todoItem.UserId != userId)
            {
                _logger.LogWarning("Bad request for updating Todo item: {TodoId} for user: {UserId}", id, userId);
                return BadRequest();
            }

            _context.Entry(todoItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Todo item {TodoId} updated for user: {UserId}", id, userId);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TodoItemExists(id))
                {
                    _logger.LogWarning("Todo item not found during update: {TodoId}", id);
                    return NotFound();
                }
                else
                {
                    _logger.LogError("Concurrency error when updating Todo item: {TodoId}", id);
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/TodoItems
        [HttpPost]
        public async Task<ActionResult<TodoItem>> PostTodoItem(TodoItem todoItem)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Invalid User ID format in claims.");
                return Unauthorized("Invalid User ID format in claims.");
            }

            todoItem.UserId = userId;

            _context.TodoItems.Add(todoItem);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Todo item {TodoId} created for user: {UserId}", todoItem.Id, userId);
            return CreatedAtAction("GetTodoItem", new { id = todoItem.Id }, todoItem);
        }

        // DELETE: api/TodoItems/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTodoItem(int id)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Invalid User ID format in claims.");
                return Unauthorized("Invalid User ID format in claims.");
            }

            var todoItem = await _context.TodoItems.SingleOrDefaultAsync(todo => todo.Id == id && todo.UserId == userId);
            if (todoItem == null)
            {
                _logger.LogWarning("Todo item not found: {TodoId}", id);
                return NotFound();
            }

            _context.TodoItems.Remove(todoItem);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Todo item {TodoId} deleted for user: {UserId}", id, userId);
            return NoContent();
        }

        private bool TodoItemExists(int id)
        {
            return _context.TodoItems.Any(e => e.Id == id);
        }
    }
}
