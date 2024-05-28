using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using RadensuasoTodo.Api.Models;

namespace RadensuasoTodo.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TodoItemsController : ControllerBase
    {
        private readonly IMongoCollection<TodoItem> _todoItemsCollection;
        private readonly ILogger<TodoItemsController> _logger;

        public TodoItemsController(IMongoDatabase database, ILogger<TodoItemsController> logger)
        {
            _todoItemsCollection = database.GetCollection<TodoItem>("TodoItems");
            _logger = logger;
        }

        // GET: api/TodoItems
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetTodoItems()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                _logger.LogWarning("Invalid User ID format in claims.");
                return Unauthorized("Invalid User ID format in claims.");
            }

            _logger.LogInformation("Fetching Todo items for user: {UserId}", userIdClaim);
            var todoItems = await _todoItemsCollection.Find(todo => todo.UserId == userIdClaim).ToListAsync();
            return todoItems;
        }

        // GET: api/TodoItems/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TodoItem>> GetTodoItem(string id)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                _logger.LogWarning("Invalid User ID format in claims.");
                return Unauthorized("Invalid User ID format in claims.");
            }

            _logger.LogInformation("Fetching Todo item {TodoId} for user: {UserId}", id, userIdClaim);
            var todoItem = await _todoItemsCollection.Find(todo => todo.Id == id && todo.UserId == userIdClaim).FirstOrDefaultAsync();

            if (todoItem == null)
            {
                _logger.LogWarning("Todo item not found: {TodoId}", id);
                return NotFound();
            }

            return todoItem;
        }

        // PUT: api/TodoItems/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTodoItem(string id, TodoItem todoItem)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                _logger.LogWarning("Invalid User ID format in claims.");
                return Unauthorized("Invalid User ID format in claims.");
            }

            if (id != todoItem.Id || todoItem.UserId != userIdClaim)
            {
                _logger.LogWarning("Bad request for updating Todo item: {TodoId} for user: {UserId}", id, userIdClaim);
                return BadRequest();
            }

            var filter = Builders<TodoItem>.Filter.Eq("Id", id);
            var updateResult = await _todoItemsCollection.ReplaceOneAsync(filter, todoItem);

            if (updateResult.MatchedCount == 0)
            {
                _logger.LogWarning("Todo item not found during update: {TodoId}", id);
                return NotFound();
            }

            _logger.LogInformation("Todo item {TodoId} updated for user: {UserId}", id, userIdClaim);
            return NoContent();
        }

        // POST: api/TodoItems
        [HttpPost]
        public async Task<ActionResult<TodoItem>> PostTodoItem(TodoItem todoItem)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                _logger.LogWarning("Invalid User ID format in claims.");
                return Unauthorized("Invalid User ID format in claims.");
            }

            todoItem.UserId = userIdClaim;

            await _todoItemsCollection.InsertOneAsync(todoItem);

            _logger.LogInformation("Todo item {TodoId} created for user: {UserId}", todoItem.Id, userIdClaim);
            return CreatedAtAction("GetTodoItem", new { id = todoItem.Id }, todoItem);
        }

        // DELETE: api/TodoItems/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTodoItem(string id)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                _logger.LogWarning("Invalid User ID format in claims.");
                return Unauthorized("Invalid User ID format in claims.");
            }

            var filter = Builders<TodoItem>.Filter.Eq("Id", id) & Builders<TodoItem>.Filter.Eq("UserId", userIdClaim);
            var deleteResult = await _todoItemsCollection.DeleteOneAsync(filter);

            if (deleteResult.DeletedCount == 0)
            {
                _logger.LogWarning("Todo item not found: {TodoId}", id);
                return NotFound();
            }

            _logger.LogInformation("Todo item {TodoId} deleted for user: {UserId}", id, userIdClaim);
            return NoContent();
        }
    }
}
