
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonajesApi.Data;
using PersonajesApi.Models;
using PersonajesApi.Services;

namespace PersonajesApi.Controllers;

// Datos para registrar un usuario
public record RegistroUsuarioDto(
    string Nombre,
    string Correo,
    string Password
);

// Datos para guardar una nota privada
public record NotaPrivadaDto(string Nota);

[ApiController]
[Route("api/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly CifradoService _cifrado;

    public UsuariosController(
        AppDbContext db,
        CifradoService cifrado)
    {
        _db = db;
        _cifrado = cifrado;
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

        // Proteger la contraseña con hash
        var hasher = new PasswordHasher<Usuario>();

        usuario.PasswordHash = hasher.HashPassword(
            usuario,
            datos.Password
        );

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        return Created($"/api/usuarios/{usuario.Id}", new
        {
            usuario.Id,
            usuario.Nombre,
            usuario.Correo
        });
    }

    // PUT /api/usuarios/{id}/nota-privada
    // Demostración local: no publicar sin autenticación
    [HttpPut("{id:int}/nota-privada")]
    public async Task<IActionResult> GuardarNota(
        int id,
        NotaPrivadaDto datos)
    {
        var usuario = await _db.Usuarios.FindAsync(id);

        if (usuario is null)
            return NotFound("Usuario no encontrado");

        if (string.IsNullOrWhiteSpace(datos.Nota))
            return BadRequest("La nota no puede estar vacía");

        // Cifrar la nota antes de guardarla
        usuario.NotaPrivadaCifrada =
            _cifrado.Cifrar(datos.Nota);

        await _db.SaveChangesAsync();

        return Ok("Nota guardada de forma cifrada");
    }
}
