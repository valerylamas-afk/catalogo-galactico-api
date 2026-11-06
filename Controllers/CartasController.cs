
using Microsoft.AspNetCore.Mvc;
using PersonajesApi.Data;
using PersonajesApi.Models;
using PersonajesApi.Models.DTOs;
using PersonajesApi.Services;

namespace PersonajesApi.Controllers;

[ApiController]
[Route("cartas")]
public class CartasController : ControllerBase
{
    private readonly CatalogoService _service;

    public CartasController(CatalogoService service)
    {
        _service = service;
    }

    // GET /cartas
    [HttpGet]
    public IActionResult Listar()
    {
        return Ok(CatalogoStore.Cartas);
    }

    // GET /cartas/{id}
    [HttpGet("{id:int}")]
    public IActionResult ObtenerPorId(int id)
    {
        var carta = _service.BuscarCarta(id);

        if (carta is null)
        {
            return NotFound("Carta no encontrada");
        }

        return Ok(carta);
    }

    // POST /cartas
    [HttpPost]
    public IActionResult Crear(CardPersonajeDto datos)
    {
        if (!_service.ExistePersonaje(datos.PersonajeId))
        {
            return BadRequest("El personaje indicado no existe");
        }

        if (_service.PersonajeTieneCarta(datos.PersonajeId))
        {
            return BadRequest("El personaje ya tiene una carta");
        }

        int nuevoId = CatalogoStore.Cartas.Count == 0
            ? 1
            : CatalogoStore.Cartas.Max(c => c.Id) + 1;

        var nuevaCarta = new CardPersonaje(
            nuevoId,
            datos.PersonajeId,
            datos.Poder,
            datos.HabilidadEspecial,
            datos.Arma,
            datos.NivelPeligrosidad,
            datos.ImagenUrl
        );

        CatalogoStore.Cartas.Add(nuevaCarta);

        return Created($"/cartas/{nuevoId}", nuevaCarta);
    }

    // PUT /cartas/{id}
    [HttpPut("{id:int}")]
    public IActionResult Actualizar(int id, CardPersonajeDto datos)
    {
        int posicion = CatalogoStore.Cartas
            .FindIndex(c => c.Id == id);

        if (posicion == -1)
        {
            return NotFound("Carta no encontrada");
        }

        if (!_service.ExistePersonaje(datos.PersonajeId))
        {
            return BadRequest("El personaje indicado no existe");
        }

        bool tieneOtraCarta = CatalogoStore.Cartas
            .Any(c =>
                c.PersonajeId == datos.PersonajeId &&
                c.Id != id
            );

        if (tieneOtraCarta)
        {
            return BadRequest("El personaje ya tiene otra carta");
        }

        var cartaActualizada = new CardPersonaje(
            id,
            datos.PersonajeId,
            datos.Poder,
            datos.HabilidadEspecial,
            datos.Arma,
            datos.NivelPeligrosidad,
            datos.ImagenUrl
        );

        CatalogoStore.Cartas[posicion] = cartaActualizada;

        return Ok(cartaActualizada);
    }
}
