
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonajesApi.Data;
using PersonajesApi.Models;

namespace PersonajesApi.Controllers;

// Datos para crear y actualizar escuadrones
public record EscuadronDto(
    string Nombre,
    string Faccion,
    int Nivel,
    int LiderId,
    List<int> Integrantes
);

// Datos para agregar un integrante
public record IntegranteDto(int PersonajeId);

[ApiController]
[Route("api/escuadrones")]
public class EscuadronesController : ControllerBase
{
    private readonly AppDbContext _db;

    public EscuadronesController(AppDbContext db)
    {
        _db = db;
    }

    // GET /api/escuadrones
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var escuadrones = await _db.Escuadrones
            .Include(e => e.Lider)
            .Include(e => e.Integrantes)
                .ThenInclude(i => i.Personaje)
            .AsNoTracking()
            .ToListAsync();

        return Ok(escuadrones.Select(MostrarEscuadron));
    }

    // GET /api/escuadrones/ranking
    [HttpGet("ranking")]
    public async Task<IActionResult> Ranking()
    {
        var escuadrones = await _db.Escuadrones
            .Include(e => e.Integrantes)
            .AsNoTracking()
            .ToListAsync();

        var cartas = await _db.Cartas
            .AsNoTracking()
            .ToListAsync();

        var ranking = escuadrones
            .Select(e => new
            {
                e.Id,
                e.Nombre,
                e.Faccion,
                e.Nivel,
                PoderTotal = e.Integrantes.Sum(i =>
                    cartas
                        .Where(c => c.PersonajeId == i.PersonajeId)
                        .Sum(c => c.Poder))
            })
            .OrderByDescending(e => e.PoderTotal)
            .ToList();

        return Ok(ranking);
    }

    // GET /api/escuadrones/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var escuadron = await _db.Escuadrones
            .Include(e => e.Lider)
            .Include(e => e.Integrantes)
                .ThenInclude(i => i.Personaje)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (escuadron is null)
            return NotFound("Escuadrón no encontrado");

        return Ok(MostrarEscuadron(escuadron));
    }

    // POST /api/escuadrones
    [HttpPost]
    public async Task<IActionResult> Crear(EscuadronDto datos)
    {
        var error = await Validar(datos);

        if (error != null)
            return BadRequest(error);

        var nuevo = new Escuadron
        {
            Nombre = datos.Nombre,
            Faccion = datos.Faccion,
            Nivel = datos.Nivel,
            LiderId = datos.LiderId
        };

        foreach (int personajeId in datos.Integrantes)
        {
            nuevo.Integrantes.Add(new EscuadronIntegrante
            {
                PersonajeId = personajeId
            });
        }

        _db.Escuadrones.Add(nuevo);
        await _db.SaveChangesAsync();

        return Created(
            $"/api/escuadrones/{nuevo.Id}",
            new
            {
                nuevo.Id,
                nuevo.Nombre,
                nuevo.Faccion,
                nuevo.Nivel,
                nuevo.LiderId,
                Integrantes = datos.Integrantes
            });
    }

    // PUT /api/escuadrones/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(
        int id, EscuadronDto datos)
    {
        var escuadron = await _db.Escuadrones
            .Include(e => e.Integrantes)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (escuadron is null)
            return NotFound("Escuadrón no encontrado");

        var error = await Validar(datos);

        if (error != null)
            return BadRequest(error);

        escuadron.Nombre = datos.Nombre;
        escuadron.Faccion = datos.Faccion;
        escuadron.Nivel = datos.Nivel;
        escuadron.LiderId = datos.LiderId;

        // Reemplazar la lista de integrantes
        _db.EscuadronIntegrantes.RemoveRange(
            escuadron.Integrantes);

        escuadron.Integrantes.Clear();

        foreach (int personajeId in datos.Integrantes)
        {
            escuadron.Integrantes.Add(new EscuadronIntegrante
            {
                EscuadronId = id,
                PersonajeId = personajeId
            });
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            escuadron.Id,
            escuadron.Nombre,
            escuadron.Faccion,
            escuadron.Nivel,
            escuadron.LiderId,
            Integrantes = datos.Integrantes
        });
    }

    // DELETE /api/escuadrones/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var escuadron = await _db.Escuadrones.FindAsync(id);

        if (escuadron is null)
            return NotFound("Escuadrón no encontrado");

        _db.Escuadrones.Remove(escuadron);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // POST /api/escuadrones/{id}/integrantes
    [HttpPost("{id:int}/integrantes")]
    public async Task<IActionResult> AgregarIntegrante(
        int id, IntegranteDto datos)
    {
        var escuadron = await _db.Escuadrones.FindAsync(id);

        if (escuadron is null)
            return NotFound("Escuadrón no encontrado");

        bool existePersonaje = await _db.Personajes
            .AnyAsync(p => p.Id == datos.PersonajeId);

        if (!existePersonaje)
            return BadRequest("El personaje no existe");

        bool yaPertenece = await _db.EscuadronIntegrantes
            .AnyAsync(i =>
                i.EscuadronId == id &&
                i.PersonajeId == datos.PersonajeId);

        if (yaPertenece)
            return BadRequest(
                "El personaje ya pertenece a este escuadrón");

        var integrante = new EscuadronIntegrante
        {
            EscuadronId = id,
            PersonajeId = datos.PersonajeId
        };

        _db.EscuadronIntegrantes.Add(integrante);
        await _db.SaveChangesAsync();

        return Ok("Integrante agregado correctamente");
    }

    // DELETE /api/escuadrones/{id}/integrantes/{personajeId}
    [HttpDelete("{id:int}/integrantes/{personajeId:int}")]
    public async Task<IActionResult> QuitarIntegrante(
        int id, int personajeId)
    {
        var escuadron = await _db.Escuadrones.FindAsync(id);

        if (escuadron is null)
            return NotFound("Escuadrón no encontrado");

        if (escuadron.LiderId == personajeId)
            return BadRequest(
                "No se puede quitar al líder del escuadrón");

        var integrante = await _db.EscuadronIntegrantes
            .FindAsync(id, personajeId);

        if (integrante is null)
            return NotFound(
                "El personaje no pertenece a este escuadrón");

        _db.EscuadronIntegrantes.Remove(integrante);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // GET /api/escuadrones/{id}/poder
    [HttpGet("{id:int}/poder")]
    public async Task<IActionResult> CalcularPoder(int id)
    {
        var escuadron = await _db.Escuadrones
            .Include(e => e.Integrantes)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (escuadron is null)
            return NotFound("Escuadrón no encontrado");

        var ids = escuadron.Integrantes
            .Select(i => i.PersonajeId)
            .ToList();

        var cartas = await _db.Cartas
            .Where(c => ids.Contains(c.PersonajeId))
            .ToListAsync();

        int poderTotal = cartas.Sum(c => c.Poder);

        return Ok(new
        {
            escuadron.Id,
            escuadron.Nombre,
            CantidadIntegrantes = ids.Count,
            PoderTotal = poderTotal
        });
    }

    // Validaciones para POST y PUT
    private async Task<string?> Validar(EscuadronDto datos)
    {
        if (string.IsNullOrWhiteSpace(datos.Nombre))
            return "El nombre es obligatorio";

        if (string.IsNullOrWhiteSpace(datos.Faccion))
            return "La facción es obligatoria";

        if (datos.Nivel < 1)
            return "El nivel debe ser mayor o igual a 1";

        if (datos.Integrantes is null ||
            datos.Integrantes.Count == 0)
            return "El escuadrón debe tener integrantes";

        if (datos.Integrantes.Distinct().Count() !=
            datos.Integrantes.Count)
            return "No se permiten integrantes repetidos";

        bool existeLider = await _db.Personajes
            .AnyAsync(p => p.Id == datos.LiderId);

        if (!existeLider)
            return "El líder no existe";

        if (!datos.Integrantes.Contains(datos.LiderId))
            return "El líder debe pertenecer al escuadrón";

        foreach (int personajeId in datos.Integrantes)
        {
            bool existe = await _db.Personajes
                .AnyAsync(p => p.Id == personajeId);

            if (!existe)
                return $"El personaje con ID {personajeId} no existe";
        }

        return null;
    }

    // Formato de respuesta sin referencias circulares
    private object MostrarEscuadron(Escuadron e)
    {
        return new
        {
            e.Id,
            e.Nombre,
            e.Faccion,
            e.Nivel,
            e.LiderId,
            Lider = e.Lider.Nombre,
            Integrantes = e.Integrantes.Select(i => new
            {
                i.PersonajeId,
                i.Personaje.Nombre,
                i.Personaje.Faccion
            }).ToList()
        };
    }
}
