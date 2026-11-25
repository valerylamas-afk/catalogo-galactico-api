
using PersonajesApi.Services;
using PersonajesApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PersonajesApi.Middleware;

// Configuracion de la API.
var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();

builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});

// Conexion a MySQL.
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString!)
    )
);

// Servicios del proyecto.
builder.Services.AddSingleton<CatalogoService>();
builder.Services.AddSingleton<CifradoService>();

// Controladores y Swagger.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Obtenemos la clave JWT desde User Secrets.
var claveJwt = Convert.FromBase64String(
    builder.Configuration["Jwt:Clave"]!
);

// Configuracion de autenticacion JWT.
builder.Services.AddAuthentication(
    JwtBearerDefaults.AuthenticationScheme
).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;

    options.TokenValidationParameters =
        new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey =
                new SymmetricSecurityKey(claveJwt),

            ValidateIssuer = true,
            ValidIssuer = "CatalogoGalactico",

            ValidateAudience = true,
            ValidAudience = "CatalogoClientes",

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,

            NameClaimType = "sub"
        };
});

builder.Services.AddAuthorization();

// Construimos la aplicacion.
var app = builder.Build();
app.UseMiddleware<ContextoPeticionMiddleware>();
// Swagger en el entorno Development.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Autenticacion y autorizacion.
app.UseAuthentication();
app.UseAuthorization();

// Rutas de los controladores.
app.MapControllers();

// Iniciamos la API.
app.Run();
