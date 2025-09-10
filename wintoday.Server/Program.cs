using Microsoft.EntityFrameworkCore;
using wintoday.Server.Infrastructure;
using wintoday.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



builder.Services.Configure<GameOptions>(builder.Configuration.GetSection(GameOptions.SectionName));
var connStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Host=localhost;Port=5432;Database=wintoday;Username=postgres;Password=postgres";
builder.Services.AddDbContext<GameDbContext>(opt => opt.UseNpgsql(connStr));
builder.Services.AddScoped<IGameService, GameService>();


var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
