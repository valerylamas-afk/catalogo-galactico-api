using PersonajesApi.Services;
using PersonajesApi.Data;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString!)
    )
);

builder.Services.AddSingleton<CatalogoService>();
builder.Services.AddSingleton<CifradoService>();

builder.Services.AddControllers(); // permite usar controaldores en nuestra API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.MapPersonajesEndpoints();
//app.MapCartasEndpoints();

//app.MapEventosEndpoints();

app.MapControllers(); // permite que ASP.NET Core reconozca sus rutas.

app.Run();