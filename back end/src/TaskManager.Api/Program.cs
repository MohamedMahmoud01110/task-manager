using TaskManager.Api.Extensions;
using TaskManager.Infrastructure;
using TaskManager.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuth(builder.Configuration);

var app = builder.Build();

await DbSeeder.SeedAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
// ... swagger ...
app.UseAuthentication();                               // BEFORE UseAuthorization
app.UseAuthorization();
app.MapControllers();
app.Run();

app.UseAuthorization();

app.MapControllers();

app.Run();
