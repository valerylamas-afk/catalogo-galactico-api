
using Microsoft.AspNetCore.Mvc;
using PersonajesApi.Data;
using PersonajesApi.Models;
using PersonajesApi.Models.DTOs;
using PersonajesApi.Services;

namespace PersonajesApi.Controllers;

[ApiController]
[Route("personajes")]
public class PersonajesController : ControllerBase
{
    private readonly CatalogoService _service;

    public PersonajesController(CatalogoService service)
    {
        _service = service;
    }

    // GET /personajes
    [HttpGet]
    public IActionResult Listar(
        [FromQuery] string? faccion,
        [FromQuery] bool? fuerzaSensitivo)
    {
        var personajes = CatalogoStore.Personajes.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(faccion))
        {
            personajes = personajes.Where(p =>
                p.Faccion.Equals(
                    faccion,
                    StringComparison.OrdinalIgnoreCase));
        }

        if (fuerzaSensitivo.HasValue)
        {
            personajes = personajes.Where(p =>
                p.FuerzaSensitivo == fuerzaSensitivo.Value);
        }

        return Ok(personajes.ToList());
    }

    // GET /personajes/ranking?por=poder
    [HttpGet("ranking")]
    public IActionResult Ranking([FromQuery] string? por)
    {
        if (por != "poder")
            return BadRequest(
                "El parámetro 'por' debe ser 'poder'");

        var ranking = CatalogoStore.Personajes
            .Join(
                CatalogoStore.Cartas,
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
            .ToList();

        return Ok(ranking);
    }

    // GET /personajes/{id}
    [HttpGet("{id:int}")]
    public IActionResult ObtenerPorId(int id)
    {
        var personaje = _service.BuscarPersonaje(id);

        if (personaje is null)
            return NotFound("Personaje no encontrado");

        return Ok(personaje);
    }

    // POST /personajes
    [HttpPost]
    public IActionResult Crear(PersonajeDto datos)
    {
        var error = Validar(datos);

        if (error != null)
            return BadRequest(error);

        int nuevoId = CatalogoStore.Personajes.Count == 0
            ? 1
            : CatalogoStore.Personajes.Max(p => p.Id) + 1;

        var nuevo = new Personaje
        {
            Id = nuevoId,
            Nombre = datos.Nombre,
            Especie = datos.Especie,
            Faccion = datos.Faccion,
            Afiliacion = datos.Afiliacion,
            Estado = datos.Estado,
            FuerzaSensitivo = datos.FuerzaSensitivo
        };

        CatalogoStore.Personajes.Add(nuevo);

        return Created($"/personajes/{nuevoId}", nuevo);
    }

    // PUT /personajes/{id}
    [HttpPut("{id:int}")]
    public IActionResult Actualizar(int id, PersonajeDto datos)
    {
        int posicion = CatalogoStore.Personajes
            .FindIndex(p => p.Id == id);

        if (posicion == -1)
            return NotFound("Personaje no encontrado");

        var error = Validar(datos);

        if (error != null)
            return BadRequest(error);

        var actualizado = new Personaje
        {
            Id = id,
            Nombre = datos.Nombre,
            Especie = datos.Especie,
            Faccion = datos.Faccion,
            Afiliacion = datos.Afiliacion,
            Estado = datos.Estado,
            FuerzaSensitivo = datos.FuerzaSensitivo
        };

        CatalogoStore.Personajes[posicion] = actualizado;

        return Ok(actualizado);
    }

    // DELETE /personajes/{id}
    [HttpDelete("{id:int}")]
    public IActionResult Eliminar(int id)
    {
        var personaje = _service.BuscarPersonaje(id);

        if (personaje is null)
            return NotFound("Personaje no encontrado");

        if (_service.PersonajeTieneCarta(id))
            return BadRequest(
                "No se puede eliminar el personaje porque tiene una carta asociada");

        bool participaEnEvento = CatalogoStore.Eventos.Any(e =>
            e.Participantes.Contains(id) ||
            e.Fallecidos.Contains(id) ||
            e.GanadorId == id);

        if (participaEnEvento)
            return BadRequest(
                "No se puede eliminar el personaje porque está relacionado con un evento");

        CatalogoStore.Personajes.Remove(personaje);

        return NoContent();
    }

    // GET /personajes/{id}/eventos
    [HttpGet("{id:int}/eventos")]
    public IActionResult ObtenerEventos(int id)
    {
        if (_service.BuscarPersonaje(id) is null)
            return NotFound("Personaje no encontrado");

        var eventos = CatalogoStore.Eventos
            .Where(e => e.Participantes.Contains(id))
            .ToList();

        return Ok(eventos);
    }

    // Validaciones para crear y actualizar personajes
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
