using PersonajesApi.Services;
using PersonajesApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<CatalogoService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
         policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseCors("Frontend");

app.MapPersonajesEndpoints();
app.MapCartasEndpoints();
app.MapEventosEndpoints();

app.Run();