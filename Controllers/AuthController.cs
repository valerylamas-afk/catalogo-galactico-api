
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using PersonajesApi.Data;
using PersonajesApi.Models;

namespace PersonajesApi.Controllers;

public record LoginDto(string Correo, string Password);

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuracion;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AppDbContext db,
        IConfiguration configuracion,
        ILogger<AuthController> logger)
    {
        _db = db;
        _configuracion = configuracion;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto datos)
    {
        string correo = datos.Correo.Trim().ToLowerInvariant();

        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Correo == correo);

        if (usuario is null)
        {
            _logger.LogWarning("Login fallido: usuario no existe");

            return Unauthorized(new
            {
                mensaje = "Correo o contraseña incorrectos"
            });
        }

        var hasher = new PasswordHasher<Usuario>();

        var resultado = hasher.VerifyHashedPassword(
            usuario,
            usuario.PasswordHash,
            datos.Password
        );

        if (resultado == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Login fallido: contraseña incorrecta");

            return Unauthorized(new
            {
                mensaje = "Correo o contraseña incorrectos"
            });
        }

        var clave = Convert.FromBase64String(
            _configuracion["Jwt:Clave"]!
        );

        var claims = new[]
        {
            new Claim("sub", usuario.Id.ToString()),
            new Claim("email", usuario.Correo),
            new Claim("name", usuario.Nombre)
        };

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(clave),
            SecurityAlgorithms.HmacSha256
        );

        var expira = DateTime.UtcNow.AddMinutes(30);

        var jwt = new JwtSecurityToken(
            issuer: "CatalogoGalactico",
            audience: "CatalogoClientes",
            claims: claims,
            expires: expira,
            signingCredentials: credenciales
        );

        var token = new JwtSecurityTokenHandler()
            .WriteToken(jwt);

        _logger.LogInformation(
            "Login correcto para el usuario {UsuarioId}",
            usuario.Id
        );

        return Ok(new
        {
            token,
            expira,
            usuario.Nombre
        });
    }
}
