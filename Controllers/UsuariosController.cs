
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonajesApi.Data;
using PersonajesApi.Models;
using PersonajesApi.Services;

namespace PersonajesApi.Controllers;

// DTO: recibe los datos necesarios para registrar un usuario.
// La contraseña recibida no se guarda directamente en MySQL.
public record RegistroUsuarioDto(
    string Nombre,
    string Correo,
    string Password
);

// DTO utilizado para recibir una nota privada.
public record NotaPrivadaDto(string Nota);

// Define las rutas de usuarios.
// Todos los endpoints comienzan con /api/usuarios.
[ApiController]
[Route("api/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly CifradoService _cifrado;
    private readonly IWebHostEnvironment _entorno;

    // Inyeccion de dependencias:
    // AppDbContext permite consultar MySQL.
    // CifradoService permite cifrar las notas.
    // IWebHostEnvironment identifica el entorno de ejecucion.
    public UsuariosController(
        AppDbContext db,
        CifradoService cifrado,
        IWebHostEnvironment entorno)
    {
        _db = db;
        _cifrado = cifrado;
        _entorno = entorno;
    }

    // POST /api/usuarios/registro
    // Registra usuarios protegiendo sus contraseñas con hash.
    [HttpPost("registro")]
    public async Task<IActionResult> Registrar(
        RegistroUsuarioDto datos)
    {
        // Validamos que el nombre no este vacio.
        if (string.IsNullOrWhiteSpace(datos.Nombre))
            return BadRequest("El nombre es obligatorio");

        // El correo tambien es obligatorio.
        if (string.IsNullOrWhiteSpace(datos.Correo))
            return BadRequest("El correo es obligatorio");

        // Exigimos una contraseña de al menos 8 caracteres.
        if (string.IsNullOrWhiteSpace(datos.Password) ||
            datos.Password.Length < 8)
            return BadRequest(
                "La contraseña debe tener al menos 8 caracteres");

        // Normalizamos el correo para evitar diferencias
        // por espacios o letras mayusculas.
        string correo = datos.Correo.Trim().ToLower();

        // AnyAsync comprueba si el correo ya esta registrado.
        // Si existe, devolvemos HTTP 400.
        bool existe = await _db.Usuarios
            .AnyAsync(u => u.Correo == correo);

        if (existe)
            return BadRequest("El correo ya está registrado");

        // Creamos el usuario sin guardar la contraseña original.
        var usuario = new Usuario
        {
            Nombre = datos.Nombre,
            Correo = correo
        };

        // PasswordHasher genera un hash de la contraseña.
        // El hash sirve para verificarla posteriormente,
        // pero no permite recuperar su texto original.
        var hasher = new PasswordHasher<Usuario>();

        usuario.PasswordHash = hasher.HashPassword(
            usuario,
            datos.Password
        );

        // Guardamos el usuario y su hash en MySQL.
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        // HTTP 201 indica que el usuario fue creado.
        // Por seguridad no devolvemos Password ni PasswordHash.
        return Created($"/api/usuarios/{usuario.Id}", new
        {
            usuario.Id,
            usuario.Nombre,
            usuario.Correo
        });
    }

    // PUT /api/usuarios/{id}/nota-privada
    // Demostracion local del cifrado AES-GCM.
    [HttpPut("{id:int}/nota-privada")]
    public async Task<IActionResult> GuardarNota(
        int id,
        NotaPrivadaDto datos)
    {
        // Solo permitimos utilizar este endpoint en Development.
        // Esto NO es autenticacion; solo limita el entorno.
        if (!_entorno.IsDevelopment())
            return NotFound();

        // Buscamos el usuario por su ID.
        var usuario = await _db.Usuarios.FindAsync(id);

        if (usuario is null)
            return NotFound("Usuario no encontrado");

        // La nota no puede estar vacia.
        if (string.IsNullOrWhiteSpace(datos.Nota))
            return BadRequest("La nota no puede estar vacía");

        // Ciframos la nota antes de guardarla.
        // En MySQL se almacena el texto cifrado,
        // no la informacion original.
        usuario.NotaPrivadaCifrada =
            _cifrado.Cifrar(datos.Nota);

        await _db.SaveChangesAsync();

        return Ok("Nota guardada de forma cifrada");
    }
}
