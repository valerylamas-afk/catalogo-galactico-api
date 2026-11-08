
using Microsoft.AspNetCore.Mvc;
using PersonajesApi.Data;
using PersonajesApi.Models;
using PersonajesApi.Models.DTOs;
using PersonajesApi.Services;

namespace PersonajesApi.Controllers;

[ApiController]
[Route("eventos")]
public class EventosController : ControllerBase
{
    private readonly CatalogoService _service;

    public EventosController(CatalogoService service)
    {
        _service = service;
    }

    // GET /eventos
    [HttpGet]
    public IActionResult Listar()
    {
        return Ok(CatalogoStore.Eventos);
    }

    // GET /eventos/{id}
    [HttpGet("{id:int}")]
    public IActionResult ObtenerPorId(int id)
    {
        var evento = _service.BuscarEvento(id);

        if (evento is null)
            return NotFound("Evento no encontrado");

        return Ok(evento);
    }

    // POST /eventos
    [HttpPost]
    public IActionResult Crear(EventoDto datos)
    {
        var error = ValidarEvento(datos);

        if (error != null)
            return BadRequest(error);

        int nuevoId = CatalogoStore.Eventos.Count == 0
            ? 1
            : CatalogoStore.Eventos.Max(e => e.Id) + 1;

        var nuevoEvento = new Evento(
            nuevoId,
            datos.Nombre,
            datos.Fecha,
            datos.Ubicacion,
            datos.Descripcion,
            datos.Participantes,
            datos.Fallecidos,
            datos.Resultado,
            datos.GanadorId
        );

        CatalogoStore.Eventos.Add(nuevoEvento);

        foreach (int fallecidoId in datos.Fallecidos)
        {
            int posicion = CatalogoStore.Personajes
                .FindIndex(p => p.Id == fallecidoId);

            var personaje = CatalogoStore.Personajes[posicion];

            personaje.Estado = "muerto";
        }

        return Created($"/eventos/{nuevoId}", nuevoEvento);
    }

    // PUT /eventos/{id}
    [HttpPut("{id:int}")]
    public IActionResult Actualizar(int id, EventoDto datos)
    {
        int posicion = CatalogoStore.Eventos
            .FindIndex(e => e.Id == id);

        if (posicion == -1)
            return NotFound("Evento no encontrado");

        var error = ValidarEvento(datos, id);

        if (error != null)
            return BadRequest(error);

        var eventoAnterior = CatalogoStore.Eventos[posicion];

        var afectados = eventoAnterior.Fallecidos
            .Union(datos.Fallecidos)
            .ToList();

        var actualizado = new Evento(
            id,
            datos.Nombre,
            datos.Fecha,
            datos.Ubicacion,
            datos.Descripcion,
            datos.Participantes,
            datos.Fallecidos,
            datos.Resultado,
            datos.GanadorId
        );

        CatalogoStore.Eventos[posicion] = actualizado;

        foreach (int personajeId in afectados)
        {
            int posicionPersonaje = CatalogoStore.Personajes
                .FindIndex(p => p.Id == personajeId);

            if (posicionPersonaje == -1)
                continue;

            bool apareceComoFallecido = CatalogoStore.Eventos
                .Any(e => e.Fallecidos.Contains(personajeId));

            var personaje = CatalogoStore.Personajes[posicionPersonaje];

            personaje.Estado = apareceComoFallecido ? "muerto" : "vivo";
        }

        return Ok(actualizado);
    }

    // GET /eventos/{id}/mvp
    [HttpGet("{id:int}/mvp")]
    public IActionResult ObtenerMvp(int id)
    {
        var evento = _service.BuscarEvento(id);

        if (evento is null)
            return NotFound("Evento no encontrado");

        var mvp = CatalogoStore.Cartas
            .Where(c => evento.Participantes.Contains(c.PersonajeId))
            .OrderByDescending(c => c.Poder)
            .FirstOrDefault();

        if (mvp is null)
            return BadRequest(
                "No hay cartas disponibles para los participantes del evento");

        var personaje = _service.BuscarPersonaje(mvp.PersonajeId);

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
    public IActionResult Simular(int id)
    {
        var evento = _service.BuscarEvento(id);

        if (evento is null)
            return NotFound("Evento no encontrado");

        if (evento.Participantes.Count < 2)
            return BadRequest(
                "Se necesitan al menos dos participantes para simular el evento");

        var participantesConPoder = evento.Participantes
            .Join(
                CatalogoStore.Personajes,
                participanteId => participanteId,
                personaje => personaje.Id,
                (participanteId, personaje) => personaje
            )
            .Join(
                CatalogoStore.Cartas,
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

    // Validaciones compartidas por POST y PUT
    private string? ValidarEvento(EventoDto datos, int? idActual = null)
    {
        if (string.IsNullOrWhiteSpace(datos.Nombre))
            return "El nombre del evento es obligatorio";

        if (datos.Participantes is null ||
            datos.Participantes.Count == 0)
            return "El evento debe tener al menos un participante";

        if (datos.Fallecidos is null)
            return "La lista de fallecidos es obligatoria";

        foreach (int participanteId in datos.Participantes)
        {
            if (!_service.ExistePersonaje(participanteId))
                return $"El personaje con ID {participanteId} no existe";

            bool murioAntes = CatalogoStore.Eventos.Any(e =>
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

            bool participaDespues = CatalogoStore.Eventos.Any(e =>
                (!idActual.HasValue || e.Id != idActual.Value) &&
                e.Fecha > datos.Fecha &&
                e.Participantes.Contains(fallecidoId));

            if (participaDespues)
                return $"El personaje con ID {fallecidoId} participa en un evento posterior y no puede morir en esta fecha";
        }

        if (datos.GanadorId.HasValue)
        {
            if (!_service.ExistePersonaje(datos.GanadorId.Value))
                return "El personaje ganador no existe";

            if (!datos.Participantes.Contains(datos.GanadorId.Value))
                return "El personaje ganador debe ser participante del evento";
        }

        return null;
    }
}
