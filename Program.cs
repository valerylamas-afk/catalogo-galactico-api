using PersonajesApi.Services;
using PersonajesApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<CatalogoService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPersonajesEndpoints();
app.MapCartasEndpoints();
app.MapEventosEndpoints();

app.Run();