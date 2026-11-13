using InventoryHub.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Services -------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// The database provider is chosen by CONFIGURATION ONLY.
//   Local run:      "DatabaseProvider": "Sqlite"   (appsettings.json default)
//   Docker / cloud: "DatabaseProvider": "Npgsql"   (environment variable)
// No code change is needed to move from SQLite to PostgreSQL.
var provider = builder.Configuration["DatabaseProvider"] ?? "Sqlite";
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (string.Equals(provider, "Npgsql", StringComparison.OrdinalIgnoreCase))
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection"));
    }
    else
    {
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
});

var app = builder.Build();

// --- Database creation + seed data on startup (local SQLite profile) ------
// EnsureCreated builds the schema from the EF Core model when the database
// does not exist yet. For production, replace this with EF Core migrations.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    if (!db.Products.Any())
    {
        db.Products.AddRange(AppDbContext.SeedProducts);
        db.SaveChanges();
    }
}

// --- HTTP pipeline ----------------------------------------------------------
app.UseSwagger();
app.UseSwaggerUI();

app.UseDefaultFiles();   // serves wwwroot/index.html at "/"
app.UseStaticFiles();    // serves the single-page frontend from wwwroot

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
