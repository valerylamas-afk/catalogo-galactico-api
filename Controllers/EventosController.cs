
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonajesApi.Data;
using PersonajesApi.Models;
using PersonajesApi.Models.DTOs;

namespace PersonajesApi.Controllers;

[ApiController]
[Route("eventos")]
public class EventosController : ControllerBase
{
    private readonly AppDbContext _db;

    public EventosController(AppDbContext db)
    {
        _db = db;
    }

    // GET /eventos
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        return Ok(await _db.Eventos.ToListAsync());
    }

    // GET /eventos/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var evento = await _db.Eventos.FindAsync(id);

        if (evento is null)
            return NotFound("Evento no encontrado");

        return Ok(evento);
    }

    // POST /eventos
    [HttpPost]
    public async Task<IActionResult> Crear(EventoDto datos)
    {
        var error = await ValidarEvento(datos);

        if (error != null)
            return BadRequest(error);

        var nuevo = new Evento
        {
            Nombre = datos.Nombre,
            Fecha = datos.Fecha,
            Ubicacion = datos.Ubicacion,
            Descripcion = datos.Descripcion,
            Participantes = datos.Participantes.ToList(),
            Fallecidos = datos.Fallecidos.ToList(),
            Resultado = datos.Resultado,
            GanadorId = datos.GanadorId
        };

        _db.Eventos.Add(nuevo);

        foreach (int fallecidoId in datos.Fallecidos)
        {
            var personaje = await _db.Personajes.FindAsync(fallecidoId);

            if (personaje != null)
                personaje.Estado = "muerto";
        }

        await _db.SaveChangesAsync();

        return Created($"/eventos/{nuevo.Id}", nuevo);
    }

    // PUT /eventos/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(
        int id, EventoDto datos)
    {
        var evento = await _db.Eventos.FindAsync(id);

        if (evento is null)
            return NotFound("Evento no encontrado");

        var error = await ValidarEvento(datos, id);

        if (error != null)
            return BadRequest(error);

        var afectados = evento.Fallecidos
            .Union(datos.Fallecidos)
            .ToList();

        evento.Nombre = datos.Nombre;
        evento.Fecha = datos.Fecha;
        evento.Ubicacion = datos.Ubicacion;
        evento.Descripcion = datos.Descripcion;
        evento.Participantes = datos.Participantes.ToList();
        evento.Fallecidos = datos.Fallecidos.ToList();
        evento.Resultado = datos.Resultado;
        evento.GanadorId = datos.GanadorId;

        // Consultamos los eventos desde MySQL.
        // El evento actual ya tiene sus nuevos valores en memoria.
        var eventos = await _db.Eventos.ToListAsync();

        foreach (int personajeId in afectados)
        {
            var personaje = await _db.Personajes.FindAsync(personajeId);

            if (personaje is null)
                continue;

            bool apareceComoFallecido = eventos.Any(e =>
                e.Fallecidos.Contains(personajeId));

            personaje.Estado = apareceComoFallecido
                ? "muerto"
                : "vivo";
        }

        await _db.SaveChangesAsync();

        return Ok(evento);
    }

    // GET /eventos/{id}/mvp
    [HttpGet("{id:int}/mvp")]
    public async Task<IActionResult> ObtenerMvp(int id)
    {
        var evento = await _db.Eventos.FindAsync(id);

        if (evento is null)
            return NotFound("Evento no encontrado");

        var cartas = await _db.Cartas.ToListAsync();

        var mvp = cartas
            .Where(c => evento.Participantes.Contains(c.PersonajeId))
            .OrderByDescending(c => c.Poder)
            .FirstOrDefault();

        if (mvp is null)
            return BadRequest(
                "No hay cartas disponibles para los participantes del evento");

        var personaje = await _db.Personajes.FindAsync(mvp.PersonajeId);

        return Ok(new
        {
            Evento = evento.Nombre,
            PersonajeId = mvp.PersonajeId,
            Personaje = personaje?.Nombre,
            Poder = mvp.Poder,
            HabilidadEspecial = mvp.HabilidadEspecial
        });
    }

    // POST /eventos/{id}/simular
    [HttpPost("{id:int}/simular")]
    public async Task<IActionResult> Simular(int id)
    {
        var evento = await _db.Eventos.FindAsync(id);

        if (evento is null)
            return NotFound("Evento no encontrado");

        if (evento.Participantes.Count < 2)
            return BadRequest(
                "Se necesitan al menos dos participantes para simular el evento");

        var personajes = await _db.Personajes.ToListAsync();
        var cartas = await _db.Cartas.ToListAsync();

        var participantesConPoder = evento.Participantes
            .Join(
                personajes,
                participanteId => participanteId,
                personaje => personaje.Id,
                (participanteId, personaje) => personaje
            )
            .Join(
                cartas,
                personaje => personaje.Id,
                carta => carta.PersonajeId,
                (personaje, carta) => new
                {
                    personaje.Id,
                    personaje.Nombre,
                    personaje.Faccion,
                    carta.Poder
                }
            )
            .ToList();

        if (participantesConPoder.Count < 2)
            return BadRequest(
                "No existen suficientes participantes con cartas para realizar la simulación");

        var bandos = participantesConPoder
            .GroupBy(p => p.Faccion)
            .Select(grupo => new
            {
                Faccion = grupo.Key,
                PoderBase = grupo.Sum(p => p.Poder)
            })
            .ToList();

        if (bandos.Count < 2)
            return BadRequest(
                "Se necesitan al menos dos facciones diferentes para simular el evento");

        var resultados = bandos
            .Select(bando =>
            {
                double factorAleatorio =
                    Random.Shared.Next(90, 111) / 100.0;

                double poderFinal =
                    bando.PoderBase * factorAleatorio;

                return new
                {
                    bando.Faccion,
                    bando.PoderBase,
                    FactorAleatorio = factorAleatorio,
                    PoderFinal = Math.Round(poderFinal, 2)
                };
            })
            .OrderByDescending(b => b.PoderFinal)
            .ToList();

        var ganador = resultados.First();

        return Ok(new
        {
            EventoId = evento.Id,
            Evento = evento.Nombre,
            Resultado = $"Victoria de {ganador.Faccion}",
            Ganador = ganador.Faccion,
            Totales = resultados,
            Criterio =
                "Suma del poder de las cartas por facción con un factor aleatorio entre 0.90 y 1.10"
        });
    }

    // Validaciones para POST y PUT
    private async Task<string?> ValidarEvento(
        EventoDto datos,
        int? idActual = null)
    {
        if (string.IsNullOrWhiteSpace(datos.Nombre))
            return "El nombre del evento es obligatorio";

        if (datos.Participantes is null ||
            datos.Participantes.Count == 0)
            return "El evento debe tener al menos un participante";

        if (datos.Fallecidos is null)
            return "La lista de fallecidos es obligatoria";

        var personajes = await _db.Personajes.ToListAsync();
        var eventos = await _db.Eventos.ToListAsync();

        foreach (int participanteId in datos.Participantes)
        {
            if (!personajes.Any(p => p.Id == participanteId))
                return $"El personaje con ID {participanteId} no existe";

            bool murioAntes = eventos.Any(e =>
                (!idActual.HasValue || e.Id != idActual.Value) &&
                e.Fecha < datos.Fecha &&
                e.Fallecidos.Contains(participanteId));

            if (murioAntes)
                return $"El personaje con ID {participanteId} murió antes de este evento";
        }

        foreach (int fallecidoId in datos.Fallecidos)
        {
            if (!datos.Participantes.Contains(fallecidoId))
                return $"El personaje fallecido con ID {fallecidoId} debe ser participante del evento";

            bool participaDespues = eventos.Any(e =>
                (!idActual.HasValue || e.Id != idActual.Value) &&
                e.Fecha > datos.Fecha &&
                e.Participantes.Contains(fallecidoId));

            if (participaDespues)
                return $"El personaje con ID {fallecidoId} participa en un evento posterior y no puede morir en esta fecha";
        }

        if (datos.GanadorId.HasValue)
        {
            if (!personajes.Any(p => p.Id == datos.GanadorId.Value))
                return "El personaje ganador no existe";

            if (!datos.Participantes.Contains(datos.GanadorId.Value))
                return "El personaje ganador debe ser participante del evento";
        }

        return null;
    }
}
