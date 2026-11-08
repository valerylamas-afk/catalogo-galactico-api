
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonajesApi.Data;
using PersonajesApi.Models;
using PersonajesApi.Models.DTOs;

namespace PersonajesApi.Controllers;

[ApiController]
[Route("personajes")]
public class PersonajesController : ControllerBase
{
    private readonly AppDbContext _db;

    public PersonajesController(AppDbContext db)
    {
        _db = db;
    }

    // GET /personajes
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? faccion,
        [FromQuery] bool? fuerzaSensitivo)
    {
        var consulta = _db.Personajes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(faccion))
        {
            consulta = consulta.Where(p =>
                p.Faccion.ToLower() == faccion.ToLower());
        }

        if (fuerzaSensitivo.HasValue)
        {
            consulta = consulta.Where(p =>
                p.FuerzaSensitivo == fuerzaSensitivo.Value);
        }

        return Ok(await consulta.ToListAsync());
    }

    // GET /personajes/ranking?por=poder
    [HttpGet("ranking")]
    public async Task<IActionResult> Ranking(
        [FromQuery] string? por)
    {
        if (por != "poder")
            return BadRequest(
                "El parámetro 'por' debe ser 'poder'");

        var ranking = await _db.Personajes
            .Join(
                _db.Cartas,
                personaje => personaje.Id,
                carta => carta.PersonajeId,
                (personaje, carta) => new
                {
                    personaje.Id,
                    personaje.Nombre,
                    personaje.Faccion,
                    Poder = carta.Poder
                })
            .OrderByDescending(x => x.Poder)
            .ToListAsync();

        return Ok(ranking);
    }

    // GET /personajes/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var personaje = await _db.Personajes.FindAsync(id);

        if (personaje is null)
            return NotFound("Personaje no encontrado");

        return Ok(personaje);
    }

    // POST /personajes
    [HttpPost]
    public async Task<IActionResult> Crear(PersonajeDto datos)
    {
        var error = Validar(datos);

        if (error != null)
            return BadRequest(error);

        var nuevo = new Personaje
        {
            Nombre = datos.Nombre,
            Especie = datos.Especie,
            Faccion = datos.Faccion,
            Afiliacion = datos.Afiliacion,
            Estado = datos.Estado,
            FuerzaSensitivo = datos.FuerzaSensitivo
        };

        _db.Personajes.Add(nuevo);
        await _db.SaveChangesAsync();

        return Created($"/personajes/{nuevo.Id}", nuevo);
    }

    // PUT /personajes/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(
        int id, PersonajeDto datos)
    {
        var personaje = await _db.Personajes.FindAsync(id);

        if (personaje is null)
            return NotFound("Personaje no encontrado");

        var error = Validar(datos);

        if (error != null)
            return BadRequest(error);

        personaje.Nombre = datos.Nombre;
        personaje.Especie = datos.Especie;
        personaje.Faccion = datos.Faccion;
        personaje.Afiliacion = datos.Afiliacion;
        personaje.Estado = datos.Estado;
        personaje.FuerzaSensitivo = datos.FuerzaSensitivo;

        await _db.SaveChangesAsync();

        return Ok(personaje);
    }

    // DELETE /personajes/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var personaje = await _db.Personajes.FindAsync(id);

        if (personaje is null)
            return NotFound("Personaje no encontrado");

        bool tieneCarta = await _db.Cartas
            .AnyAsync(c => c.PersonajeId == id);

        if (tieneCarta)
            return BadRequest(
                "No se puede eliminar el personaje porque tiene una carta asociada");

        bool esGanador = await _db.Eventos
            .AnyAsync(e => e.GanadorId == id);

        var eventos = await _db.Eventos.ToListAsync();

        bool participaEnEvento = esGanador ||
            eventos.Any(e =>
                e.Participantes.Contains(id) ||
                e.Fallecidos.Contains(id));

        if (participaEnEvento)
            return BadRequest(
                "No se puede eliminar el personaje porque está relacionado con un evento");

        _db.Personajes.Remove(personaje);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // GET /personajes/{id}/eventos
    [HttpGet("{id:int}/eventos")]
    public async Task<IActionResult> ObtenerEventos(int id)
    {
        var personaje = await _db.Personajes.FindAsync(id);

        if (personaje is null)
            return NotFound("Personaje no encontrado");

        var eventos = await _db.Eventos.ToListAsync();

        var relacionados = eventos
            .Where(e => e.Participantes.Contains(id))
            .ToList();

        return Ok(relacionados);
    }

    // Validaciones para POST y PUT
    private string? Validar(PersonajeDto datos)
    {
        if (string.IsNullOrWhiteSpace(datos.Nombre))
            return "El nombre es obligatorio";

        string[] facciones = { "Rebelde", "Imperio", "Neutral" };

        if (!facciones.Contains(datos.Faccion))
            return "La facción debe ser Rebelde, Imperio o Neutral";

        string[] estados = { "vivo", "muerto", "desconocido" };

        if (!estados.Contains(datos.Estado))
            return "El estado debe ser vivo, muerto o desconocido";

        return null;
    }
}
