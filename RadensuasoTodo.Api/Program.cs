using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.Text;
using RadensuasoTodo.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

// Load environment variables from .env file
string envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
if (File.Exists(envPath))
{
    DotNetEnv.Env.Load(envPath);
}
else
{
    Console.WriteLine("Warning: The .env file was not found at the expected location.");
}

// Read MongoDB connection string from environment variables
var mongoConnectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING") ?? throw new ArgumentNullException("MONGO_CONNECTION_STRING must be set");
var databaseName = Environment.GetEnvironmentVariable("MONGO_DATABASE") ?? throw new ArgumentNullException("MONGO_DATABASE must be set");

// Configure MongoDB
var mongoClient = new MongoClient(mongoConnectionString);
var mongoDatabase = mongoClient.GetDatabase(databaseName);
builder.Services.AddSingleton(mongoDatabase);

// Ensure collections and indexes
EnsureIndexes(mongoDatabase);

// Configure JWT authentication
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? throw new ArgumentNullException("JWT_KEY must be set");
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? throw new ArgumentNullException("JWT_ISSUER must be set");
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? throw new ArgumentNullException("JWT_AUDIENCE must be set");

builder.Services.AddSingleton(new JwtSettings
{
    Key = jwtKey,
    Issuer = jwtIssuer,
    Audience = jwtAudience
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// Configure CORS
var frontendUrls = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? throw new ArgumentNullException("FRONTEND_URL");
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins(frontendUrls.Split(","))
                   .AllowAnyHeader()
                   .AllowAnyMethod()
                   .AllowCredentials();  // If using cookies or authorization headers
        });
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Ensure this is enabled in development
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "RadensuasoTodo API V1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    });
}

// Use CORS
app.UseCors("AllowFrontend");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

void EnsureIndexes(IMongoDatabase database)
{
    var userCollection = database.GetCollection<User>("Users");
    var todoItemsCollection = database.GetCollection<TodoItem>("TodoItems");

    var userIndexes = Builders<User>.IndexKeys.Ascending(u => u.Username);
    userCollection.Indexes.CreateOne(new CreateIndexModel<User>(userIndexes));

    var todoItemIndexes = Builders<TodoItem>.IndexKeys.Ascending(t => t.UserId);
    todoItemsCollection.Indexes.CreateOne(new CreateIndexModel<TodoItem>(todoItemIndexes));
}
