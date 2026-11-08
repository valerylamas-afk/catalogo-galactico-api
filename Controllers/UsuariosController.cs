
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonajesApi.Data;
using PersonajesApi.Models;

namespace PersonajesApi.Controllers;

public record RegistroUsuarioDto(
    string Nombre,
    string Correo,
    string Password
);

[ApiController]
[Route("api/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsuariosController(AppDbContext db)
    {
        _db = db;
    }

    // POST /api/usuarios/registro
    [HttpPost("registro")]
    public async Task<IActionResult> Registrar(
        RegistroUsuarioDto datos)
    {
        if (string.IsNullOrWhiteSpace(datos.Nombre))
            return BadRequest("El nombre es obligatorio");

        if (string.IsNullOrWhiteSpace(datos.Correo))
            return BadRequest("El correo es obligatorio");

        if (string.IsNullOrWhiteSpace(datos.Password) ||
            datos.Password.Length < 8)
            return BadRequest(
                "La contraseña debe tener al menos 8 caracteres");

        string correo = datos.Correo.Trim().ToLower();

        bool existe = await _db.Usuarios
            .AnyAsync(u => u.Correo == correo);

        if (existe)
            return BadRequest("El correo ya está registrado");

        var usuario = new Usuario
        {
            Nombre = datos.Nombre,
            Correo = correo
        };

        // Generar hash de la contraseña
        var hasher = new PasswordHasher<Usuario>();

        usuario.PasswordHash = hasher.HashPassword(
            usuario,
            datos.Password
        );

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        // Nunca devolver la contraseña ni su hash
        return Created($"/api/usuarios/{usuario.Id}", new
        {
            usuario.Id,
            usuario.Nombre,
            usuario.Correo
        });
    }
}
