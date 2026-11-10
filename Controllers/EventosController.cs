
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonajesApi.Data;
using PersonajesApi.Models;
using PersonajesApi.Models.DTOs;

namespace PersonajesApi.Controllers;

// ApiController permite manejar peticiones HTTP.
// Todas las rutas de este controlador empiezan con /eventos.
[ApiController]
[Route("eventos")]
public class EventosController : ControllerBase
{
    private readonly AppDbContext _db;

    // Inyeccion de dependencias:
    // recibimos el contexto para consultar MySQL.
    public EventosController(AppDbContext db)
    {
        _db = db;
    }

    // GET /eventos
    // Consulta todos los eventos registrados en MySQL.
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        return Ok(await _db.Eventos.ToListAsync());
    }

    // GET /eventos/{id}
    // Busca un evento por su clave primaria.
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        // FindAsync busca el registro por su ID.
        var evento = await _db.Eventos.FindAsync(id);

        // Si no existe, devolvemos HTTP 404.
        if (evento is null)
            return NotFound("Evento no encontrado");

        return Ok(evento);
    }

    // POST /eventos
    // Crea un evento y actualiza el estado de sus fallecidos.
    [HttpPost]
    public async Task<IActionResult> Crear(EventoDto datos)
    {
        // Validamos antes de guardar cualquier informacion.
        var error = await ValidarEvento(datos);

        if (error != null)
            return BadRequest(error);

        // Convertimos los datos recibidos en una entidad Evento.
        // ToList crea una lista independiente de los datos recibidos.
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

        // Add prepara el evento para insertarlo en MySQL.
        _db.Eventos.Add(nuevo);

        // Si el evento tiene fallecidos, cambiamos su estado.
        foreach (int fallecidoId in datos.Fallecidos)
        {
            var personaje = await _db.Personajes.FindAsync(fallecidoId);

            if (personaje != null)
                personaje.Estado = "muerto";
        }

        // Guarda el evento y los cambios de los personajes.
        await _db.SaveChangesAsync();

        // HTTP 201 indica que se creo correctamente.
        return Created($"/eventos/{nuevo.Id}", nuevo);
    }

    // PUT /eventos/{id}
    // Actualiza un evento y recalcula los fallecidos afectados.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(
        int id, EventoDto datos)
    {
        var evento = await _db.Eventos.FindAsync(id);

        if (evento is null)
            return NotFound("Evento no encontrado");

        // En PUT ignoramos el evento actual al validar
        // posibles conflictos con otros eventos.
        var error = await ValidarEvento(datos, id);

        if (error != null)
            return BadRequest(error);

        // Union combina los fallecidos anteriores y nuevos,
        // sin repetir IDs. Son los personajes a revisar.
        var afectados = evento.Fallecidos
            .Union(datos.Fallecidos)
            .ToList();

        // Actualizamos las propiedades del evento.
        evento.Nombre = datos.Nombre;
        evento.Fecha = datos.Fecha;
        evento.Ubicacion = datos.Ubicacion;
        evento.Descripcion = datos.Descripcion;
        evento.Participantes = datos.Participantes.ToList();
        evento.Fallecidos = datos.Fallecidos.ToList();
        evento.Resultado = datos.Resultado;
        evento.GanadorId = datos.GanadorId;

        // Consultamos los eventos desde MySQL.
        // EF Core tambien conserva los cambios del evento
        // actual en la instancia que esta rastreando.
        var eventos = await _db.Eventos.ToListAsync();

        foreach (int personajeId in afectados)
        {
            var personaje = await _db.Personajes.FindAsync(personajeId);

            if (personaje is null)
                continue;

            // Revisamos si el personaje figura como fallecido
            // en algun evento despues de la actualizacion.
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
    // MVP: participante con la carta de mayor poder.
    [HttpGet("{id:int}/mvp")]
    public async Task<IActionResult> ObtenerMvp(int id)
    {
        var evento = await _db.Eventos.FindAsync(id);

        if (evento is null)
            return NotFound("Evento no encontrado");

        // Consultamos las cartas guardadas en MySQL.
        var cartas = await _db.Cartas.ToListAsync();

        // Filtramos las cartas de los participantes.
        // OrderByDescending ordena de mayor a menor poder.
        // FirstOrDefault selecciona la carta mas poderosa.
        var mvp = cartas
            .Where(c => evento.Participantes.Contains(c.PersonajeId))
            .OrderByDescending(c => c.Poder)
            .FirstOrDefault();

        if (mvp is null)
            return BadRequest(
                "No hay cartas disponibles para los participantes del evento");

        var personaje = await _db.Personajes.FindAsync(mvp.PersonajeId);

        // Devolvemos el nombre del evento y los datos del MVP.
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
    // Simula un enfrentamiento entre facciones.
    [HttpPost("{id:int}/simular")]
    public async Task<IActionResult> Simular(int id)
    {
        var evento = await _db.Eventos.FindAsync(id);

        if (evento is null)
            return NotFound("Evento no encontrado");

        // Necesitamos al menos dos participantes.
        if (evento.Participantes.Count < 2)
            return BadRequest(
                "Se necesitan al menos dos participantes para simular el evento");

        var personajes = await _db.Personajes.ToListAsync();
        var cartas = await _db.Cartas.ToListAsync();

        // Join relaciona los IDs del evento con personajes
        // y despues con sus cartas para obtener el poder.
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

        // Solo participan en el calculo quienes tienen carta.
        if (participantesConPoder.Count < 2)
            return BadRequest(
                "No existen suficientes participantes con cartas para realizar la simulación");

        // GroupBy separa a los participantes por faccion.
        // Sum obtiene el poder base de cada bando.
        var bandos = participantesConPoder
            .GroupBy(p => p.Faccion)
            .Select(grupo => new
            {
                Faccion = grupo.Key,
                PoderBase = grupo.Sum(p => p.Poder)
            })
            .ToList();

        // Debe haber al menos dos facciones distintas.
        if (bandos.Count < 2)
            return BadRequest(
                "Se necesitan al menos dos facciones diferentes para simular el evento");

        var resultados = bandos
            .Select(bando =>
            {
                // Next(90,111) genera enteros de 90 a 110.
                // Al dividir entre 100 obtenemos 0.90 a 1.10.
                double factorAleatorio =
                    Random.Shared.Next(90, 111) / 100.0;

                // Multiplicamos el poder base por ese factor.
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

            // La faccion con mayor poder final gana.
            .OrderByDescending(b => b.PoderFinal)
            .ToList();

        var ganador = resultados.First();

        // Solo mostramos el resultado de la simulacion.
        // No modificamos el GanadorId guardado en MySQL.
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

    // Validaciones compartidas por POST y PUT.
    // Devuelve un mensaje cuando existe un error.
    // Devuelve null cuando los datos son validos.
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
            // Un personaje solo puede participar si existe.
            if (!personajes.Any(p => p.Id == participanteId))
                return $"El personaje con ID {participanteId} no existe";

            // Comparamos fechas para impedir participar
            // despues de haber fallecido en un evento anterior.
            // En PUT excluimos el evento que se esta editando.
            bool murioAntes = eventos.Any(e =>
                (!idActual.HasValue || e.Id != idActual.Value) &&
                e.Fecha < datos.Fecha &&
                e.Fallecidos.Contains(participanteId));

            if (murioAntes)
                return $"El personaje con ID {participanteId} murió antes de este evento";
        }

        foreach (int fallecidoId in datos.Fallecidos)
        {
            // Un fallecido debe estar entre los participantes.
            if (!datos.Participantes.Contains(fallecidoId))
                return $"El personaje fallecido con ID {fallecidoId} debe ser participante del evento";

            // No dejamos registrar su muerte si ya figura
            // como participante en un evento posterior.
            bool participaDespues = eventos.Any(e =>
                (!idActual.HasValue || e.Id != idActual.Value) &&
                e.Fecha > datos.Fecha &&
                e.Participantes.Contains(fallecidoId));

            if (participaDespues)
                return $"El personaje con ID {fallecidoId} participa en un evento posterior y no puede morir en esta fecha";
        }

        if (datos.GanadorId.HasValue)
        {
            // Si hay ganador, debe existir como personaje.
            if (!personajes.Any(p => p.Id == datos.GanadorId.Value))
                return "El personaje ganador no existe";

            // El ganador tambien debe haber participado.
            if (!datos.Participantes.Contains(datos.GanadorId.Value))
                return "El personaje ganador debe ser participante del evento";
        }

        return null;
    }
}
