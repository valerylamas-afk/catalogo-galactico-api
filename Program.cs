using PersonajesApi.Services;
using PersonajesApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<CatalogoService>();

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

app.MapEventosEndpoints();

app.MapControllers(); // permite que ASP.NET Core reconozca sus rutas.

app.Run();