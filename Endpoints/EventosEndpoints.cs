using PersonajesApi.Data;
using PersonajesApi.Models;
using PersonajesApi.Models.DTOs;
using PersonajesApi.Services;

namespace PersonajesApi.Endpoints;

public static class EventosEndpoints
{
    public static void MapEventosEndpoints(this WebApplication app)
    {
        // GET - Obtener todos los eventos
        app.MapGet("/eventos", () =>
        {
            return Results.Ok(CatalogoStore.Eventos);
        })
        .WithTags("Eventos")
        .WithSummary("Listar eventos")
        .WithDescription(
            "Obtiene todos los eventos registrados en el catálogo."
        )
        .Produces<List<Evento>>(StatusCodes.Status200OK);


        // GET - Obtener un evento por ID
        app.MapGet("/eventos/{id:int}", (
            int id,
            CatalogoService service) =>
        {
            var evento = service.BuscarEvento(id);

            if (evento is null)
            {
                return Results.NotFound(
                    "Evento no encontrado"
                );
            }

            return Results.Ok(evento);
        })
        .WithTags("Eventos")
        .WithSummary("Buscar evento por ID")
        .WithDescription(
            "Obtiene un evento utilizando su identificador."
        )
        .Produces<Evento>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status404NotFound);


        // POST - Crear un evento
        app.MapPost("/eventos", (EventoDto evento) =>
        {
            if (string.IsNullOrWhiteSpace(evento.Nombre))
            {
                return Results.BadRequest(
                    "El nombre del evento es obligatorio"
                );
            }

            if (evento.Participantes is null ||
                evento.Participantes.Count == 0)
            {
                return Results.BadRequest(
                    "El evento debe tener al menos un participante"
                );
            }

            if (evento.Fallecidos is null)
            {
                return Results.BadRequest(
                    "La lista de fallecidos es obligatoria"
                );
            }

            // Verificar que todos los participantes existan
            // y que no hayan muerto antes del evento.
            foreach (int participanteId in evento.Participantes)
            {
                bool existePersonaje =
                    CatalogoStore.Personajes.Any(p =>
                        p.Id == participanteId
                    );

                if (!existePersonaje)
                {
                    return Results.BadRequest(
                        $"El personaje con ID {participanteId} no existe"
                    );
                }

                bool murioAnteriormente =
                    CatalogoStore.Eventos.Any(e =>
                        e.Fecha < evento.Fecha &&
                        e.Fallecidos.Contains(participanteId)
                    );

                if (murioAnteriormente)
                {
                    return Results.BadRequest(
                        $"El personaje con ID {participanteId} murió antes de este evento"
                    );
                }
            }

            // Verificar que cada fallecido sea participante.
            foreach (int fallecidoId in evento.Fallecidos)
            {
                if (!evento.Participantes.Contains(fallecidoId))
                {
                    return Results.BadRequest(
                        $"El personaje fallecido con ID {fallecidoId} debe ser participante del evento"
                    );
                }

                // Si muere en este evento, no puede existir
                // un evento posterior donde siga participando.
                bool participaDespues =
                    CatalogoStore.Eventos.Any(e =>
                        e.Fecha > evento.Fecha &&
                        e.Participantes.Contains(fallecidoId)
                    );

                if (participaDespues)
                {
                    return Results.BadRequest(
                        $"El personaje con ID {fallecidoId} participa en un evento posterior y no puede morir en esta fecha"
                    );
                }
            }
            
            if (evento.GanadorId.HasValue)
{
            bool ganadorExiste = CatalogoStore.Personajes
                .Any(p => p.Id == evento.GanadorId.Value);

            if (!ganadorExiste)
            {
                return Results.BadRequest(
                    "El personaje ganador no existe"
                );
            }

            if (!evento.Participantes.Contains(evento.GanadorId.Value))
            {
                return Results.BadRequest(
                    "El personaje ganador debe ser participante del evento"
                );
            }
}
            int nuevoId = CatalogoStore.Eventos.Count == 0
                ? 1
                : CatalogoStore.Eventos.Max(e => e.Id) + 1;

            var nuevoEvento = new Evento(
                nuevoId,
                evento.Nombre,
                evento.Fecha,
                evento.Ubicacion,
                evento.Descripcion,
                evento.Participantes,
                evento.Fallecidos,
                evento.Resultado,
                evento.GanadorId
            );

            CatalogoStore.Eventos.Add(nuevoEvento);

            // Actualizar estado de los fallecidos.
            foreach (int fallecidoId in evento.Fallecidos)
            {
                int posicion = CatalogoStore.Personajes
                    .FindIndex(p => p.Id == fallecidoId);

                var personaje =
                    CatalogoStore.Personajes[posicion];

                CatalogoStore.Personajes[posicion] =
                    personaje with
                    {
                        Estado = "muerto"
                    };
            }

            return Results.Created(
                $"/eventos/{nuevoId}",
                nuevoEvento
            );
        })
        .WithTags("Eventos")
        .WithSummary("Crear evento")
        .WithDescription(
            "Crea un evento y valida la consistencia temporal de sus participantes y fallecidos."
        )
        .Produces<Evento>(StatusCodes.Status201Created)
        .Produces<string>(StatusCodes.Status400BadRequest);


        // PUT - Modificar un evento
        app.MapPut("/eventos/{id:int}", (
            int id,
            EventoDto datos) =>
        {
            int posicionEvento = CatalogoStore.Eventos
                .FindIndex(e => e.Id == id);

            if (posicionEvento == -1)
            {
                return Results.NotFound(
                    "Evento no encontrado"
                );
            }

            if (string.IsNullOrWhiteSpace(datos.Nombre))
            {
                return Results.BadRequest(
                    "El nombre del evento es obligatorio"
                );
            }

            if (datos.Participantes is null ||
                datos.Participantes.Count == 0)
            {
                return Results.BadRequest(
                    "El evento debe tener al menos un participante"
                );
            }

            if (datos.Fallecidos is null)
            {
                return Results.BadRequest(
                    "La lista de fallecidos es obligatoria"
                );
            }

            foreach (int participanteId in datos.Participantes)
            {
                bool existePersonaje =
                    CatalogoStore.Personajes.Any(p =>
                        p.Id == participanteId
                    );

                if (!existePersonaje)
                {
                    return Results.BadRequest(
                        $"El personaje con ID {participanteId} no existe"
                    );
                }

                bool murioAnteriormente =
                    CatalogoStore.Eventos.Any(e =>
                        e.Id != id &&
                        e.Fecha < datos.Fecha &&
                        e.Fallecidos.Contains(participanteId)
                    );

                if (murioAnteriormente)
                {
                    return Results.BadRequest(
                        $"El personaje con ID {participanteId} murió antes de este evento"
                    );
                }
            }

            foreach (int fallecidoId in datos.Fallecidos)
            {
                if (!datos.Participantes.Contains(fallecidoId))
                {
                    return Results.BadRequest(
                        $"El personaje fallecido con ID {fallecidoId} debe ser participante del evento"
                    );
                }

                bool participaDespues =
                    CatalogoStore.Eventos.Any(e =>
                        e.Id != id &&
                        e.Fecha > datos.Fecha &&
                        e.Participantes.Contains(fallecidoId)
                    );

                if (participaDespues)
                {
                    return Results.BadRequest(
                        $"El personaje con ID {fallecidoId} participa en un evento posterior y no puede morir en esta fecha"
                    );
                }
            }

            var eventoAnterior =
                CatalogoStore.Eventos[posicionEvento];

            var afectados = eventoAnterior.Fallecidos
                .Union(datos.Fallecidos)
                .ToList();
            
            if (datos.GanadorId.HasValue)
            {
             bool ganadorExiste = CatalogoStore.Personajes
                .Any(p => p.Id == datos.GanadorId.Value);

            if (!ganadorExiste)
            {
                return Results.BadRequest(
                    "El personaje ganador no existe"
                );
            }

            if (!datos.Participantes.Contains(datos.GanadorId.Value))
            {
                return Results.BadRequest(
                    "El personaje ganador debe ser participante del evento"
                );
            }
        }

            var eventoActualizado = new Evento(
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

            CatalogoStore.Eventos[posicionEvento] =
                eventoActualizado;

            // Recalcular el estado de los personajes afectados.
            foreach (int personajeId in afectados)
            {
                int posicionPersonaje =
                    CatalogoStore.Personajes.FindIndex(p =>
                        p.Id == personajeId
                    );

                if (posicionPersonaje == -1)
                {
                    continue;
                }

                bool apareceComoFallecido =
                    CatalogoStore.Eventos.Any(e =>
                        e.Fallecidos.Contains(personajeId)
                    );

                var personaje =
                    CatalogoStore.Personajes[posicionPersonaje];

                CatalogoStore.Personajes[posicionPersonaje] =
                    personaje with
                    {
                        Estado = apareceComoFallecido
                            ? "muerto"
                            : "vivo"
                    };
            }

            return Results.Ok(eventoActualizado);
        })
        .WithTags("Eventos")
        .WithSummary("Modificar evento")
        .WithDescription(
            "Actualiza un evento y vuelve a comprobar la consistencia temporal de los personajes."
        )
        .Produces<Evento>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status400BadRequest)
        .Produces<string>(StatusCodes.Status404NotFound);


        // GET - Obtener el MVP de un evento
        app.MapGet("/eventos/{id:int}/mvp", (
            int id,
            CatalogoService service) =>
        {
            var evento = service.BuscarEvento(id);

            if (evento is null)
            {
                return Results.NotFound(
                    "Evento no encontrado"
                );
            }

            var mvp = CatalogoStore.Cartas
                .Where(c =>
                    evento.Participantes.Contains(
                        c.PersonajeId
                    )
                )
                .OrderByDescending(c => c.Poder)
                .FirstOrDefault();

            if (mvp is null)
            {
                return Results.BadRequest(
                    "No hay cartas disponibles para los participantes del evento"
                );
            }

            var personaje =
                service.BuscarPersonaje(mvp.PersonajeId);

            return Results.Ok(new
            {
                Evento = evento.Nombre,
                PersonajeId = mvp.PersonajeId,
                Personaje = personaje?.Nombre,
                Poder = mvp.Poder,
                HabilidadEspecial =
                    mvp.HabilidadEspecial
            });
        })
        .WithTags("Eventos")
        .WithSummary("Obtener MVP del evento")
        .WithDescription(
            "Obtiene al participante con mayor poder de carta dentro del evento."
        )
        .Produces(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status400BadRequest)
        .Produces<string>(StatusCodes.Status404NotFound);


        // POST - Simular un evento
        app.MapPost("/eventos/{id:int}/simular", (
            int id,
            CatalogoService service) =>
        {
            var evento = service.BuscarEvento(id);

            if (evento is null)
            {
                return Results.NotFound(
                    "Evento no encontrado"
                );
            }

            if (evento.Participantes.Count < 2)
            {
                return Results.BadRequest(
                    "Se necesitan al menos dos participantes para simular el evento"
                );
            }

            var participantesConPoder =
                evento.Participantes
                    .Join(
                        CatalogoStore.Personajes,
                        participanteId => participanteId,
                        personaje => personaje.Id,
                        (participanteId, personaje) =>
                            personaje
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
            {
                return Results.BadRequest(
                    "No existen suficientes participantes con cartas para realizar la simulación"
                );
            }

            var bandos = participantesConPoder
                .GroupBy(p => p.Faccion)
                .Select(grupo => new
                {
                    Faccion = grupo.Key,
                    PoderBase =
                        grupo.Sum(p => p.Poder)
                })
                .ToList();

            if (bandos.Count < 2)
            {
                return Results.BadRequest(
                    "Se necesitan al menos dos facciones diferentes para simular el evento"
                );
            }

            var resultados = bandos
                .Select(bando =>
                {
                    double factorAleatorio =
                        Random.Shared.Next(90, 111)
                        / 100.0;

                    double poderFinal =
                        bando.PoderBase *
                        factorAleatorio;

                    return new
                    {
                        bando.Faccion,
                        bando.PoderBase,
                        FactorAleatorio =
                            factorAleatorio,
                        PoderFinal =
                            Math.Round(
                                poderFinal,
                                2
                            )
                    };
                })
                .OrderByDescending(b =>
                    b.PoderFinal
                )
                .ToList();

            var ganador = resultados.First();

            return Results.Ok(new
            {
                EventoId = evento.Id,
                Evento = evento.Nombre,
                Resultado =
                    $"Victoria de {ganador.Faccion}",
                Ganador = ganador.Faccion,
                Totales = resultados,
                Criterio =
                    "Suma del poder de las cartas por facción con un factor aleatorio entre 0.90 y 1.10"
            });
        })
        .WithTags("Eventos")
        .WithSummary("Simular evento")
        .WithDescription(
            "Simula el resultado del evento sumando el poder por facción y aplicando un factor aleatorio entre 0.90 y 1.10."
        )
        .Produces(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status400BadRequest)
        .Produces<string>(StatusCodes.Status404NotFound);
    }
}