using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using ClipOrganizer.Api.Data;
using ClipOrganizer.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Entity Framework Core with SQLite
builder.Services.AddDbContext<ClipDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add custom services
builder.Services.AddScoped<IYouTubeService, YouTubeService>();
builder.Services.AddScoped<IClipValidationService, ClipValidationService>();
builder.Services.AddScoped<IAIClipGenerationService, AIClipGenerationService>();
builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddHttpClient<AIClipGenerationService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextJs", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowNextJs");
app.UseAuthorization();
app.MapControllers();

// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ClipDbContext>();
    context.Database.EnsureCreated();
    
    // Add Description column if it doesn't exist (for existing databases)
    try
    {
        var connection = context.Database.GetDbConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        
        // Check if Description column exists
        command.CommandText = "PRAGMA table_info(Clips)";
        using var reader = command.ExecuteReader();
        var columns = new List<string>();
        while (reader.Read())
        {
            columns.Add(reader.GetString(1)); // Column name is at index 1
        }
        reader.Close();
        
        if (!columns.Contains("Description"))
        {
            // Add Description column
            command.CommandText = "ALTER TABLE Clips ADD COLUMN Description TEXT DEFAULT ''";
            command.ExecuteNonQuery();
        }
        
        // Check if Settings table exists
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Settings'";
        using var tableReader = command.ExecuteReader();
        bool settingsTableExists = tableReader.HasRows;
        tableReader.Close();
        
        if (!settingsTableExists)
        {
            // Create Settings table
            command.CommandText = @"
                CREATE TABLE Settings (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Key TEXT NOT NULL UNIQUE,
                    Value TEXT
                )";
            command.ExecuteNonQuery();
        }
        
        connection.Close();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Could not migrate database automatically. If the database is new, this is normal.");
    }
}

app.Run();
