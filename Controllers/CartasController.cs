
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonajesApi.Data;
using PersonajesApi.Models;
using PersonajesApi.Models.DTOs;

namespace PersonajesApi.Controllers;

[ApiController]
[Route("cartas")]
public class CartasController : ControllerBase
{
    private readonly AppDbContext _db;

    public CartasController(AppDbContext db)
    {
        _db = db;
    }

    // GET /cartas
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var cartas = await _db.Cartas.ToListAsync();
        return Ok(cartas);
    }

    // GET /cartas/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var carta = await _db.Cartas.FindAsync(id);

        if (carta is null)
            return NotFound("Carta no encontrada");

        return Ok(carta);
    }

    // POST /cartas
    [HttpPost]
    public async Task<IActionResult> Crear(CardPersonajeDto datos)
    {
        bool existePersonaje = await _db.Personajes
            .AnyAsync(p => p.Id == datos.PersonajeId);

        if (!existePersonaje)
            return BadRequest("El personaje no existe");

        bool tieneCarta = await _db.Cartas
            .AnyAsync(c => c.PersonajeId == datos.PersonajeId);

        if (tieneCarta)
            return BadRequest("El personaje ya tiene una carta");

        var nueva = new CardPersonaje
        {
            PersonajeId = datos.PersonajeId,
            Poder = datos.Poder,
            HabilidadEspecial = datos.HabilidadEspecial,
            Arma = datos.Arma,
            NivelPeligrosidad = datos.NivelPeligrosidad,
            ImagenUrl = datos.ImagenUrl
        };

        _db.Cartas.Add(nueva);
        await _db.SaveChangesAsync();

        return Created($"/cartas/{nueva.Id}", nueva);
    }

    // PUT /cartas/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(
        int id, CardPersonajeDto datos)
    {
        var carta = await _db.Cartas.FindAsync(id);

        if (carta is null)
            return NotFound("Carta no encontrada");

        bool existePersonaje = await _db.Personajes
            .AnyAsync(p => p.Id == datos.PersonajeId);

        if (!existePersonaje)
            return BadRequest("El personaje no existe");

        bool otraCarta = await _db.Cartas
            .AnyAsync(c =>
                c.PersonajeId == datos.PersonajeId &&
                c.Id != id);

        if (otraCarta)
            return BadRequest(
                "El personaje ya tiene otra carta");

        carta.PersonajeId = datos.PersonajeId;
        carta.Poder = datos.Poder;
        carta.HabilidadEspecial = datos.HabilidadEspecial;
        carta.Arma = datos.Arma;
        carta.NivelPeligrosidad = datos.NivelPeligrosidad;
        carta.ImagenUrl = datos.ImagenUrl;

        await _db.SaveChangesAsync();

        return Ok(carta);
    }
}
