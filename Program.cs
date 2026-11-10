
using PersonajesApi.Services;
using PersonajesApi.Data;
using Microsoft.EntityFrameworkCore;

// Aqui comienza la configuracion de nuestra API.
var builder = WebApplication.CreateBuilder(args);

// Obtenemos la conexion a MySQL desde la configuracion.
// En nuestro proyecto la guardamos en User Secrets
// para no escribir la contraseña directamente en el codigo.
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection");

// Registramos AppDbContext para trabajar con MySQL.
// UseMySql indica que utilizamos MySQL como base de datos.
// AutoDetect identifica la version del servidor MySQL.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString!)
    )
);

// Registramos los servicios que utilizara la API.
// Singleton significa que se comparte una instancia
// del servicio durante la ejecucion de la aplicacion.
builder.Services.AddSingleton<CatalogoService>();
builder.Services.AddSingleton<CifradoService>();

// AddControllers habilita el uso de controladores.
// Swagger permite visualizar y probar nuestros endpoints.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Construimos la aplicacion con los servicios configurados.
var app = builder.Build();

// Swagger se habilita solamente en Development.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Reconoce las rutas definidas en nuestros Controllers.
// Por ejemplo: /personajes, /eventos y /api/escuadrones.
app.MapControllers();

// Inicia la API y comienza a recibir peticiones HTTP.
app.Run();
