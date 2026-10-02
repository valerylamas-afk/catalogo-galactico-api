using PersonajesApi.Data;
using PersonajesApi.Models;
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
        });

        // GET - Obtener un evento por ID
        app.MapGet("/eventos/{id:int}", (
            int id,
            CatalogoService service) =>
        {
            var evento = service.BuscarEvento(id);

            if (evento is null)
            {
                return Results.NotFound("Evento no encontrado");
            }

            return Results.Ok(evento);
        });

        // POST - Crear un evento
        app.MapPost("/eventos", (Evento evento) =>
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

            foreach (int participanteId in evento.Participantes)
            {
                bool existePersonaje = CatalogoStore.Personajes
                    .Any(p => p.Id == participanteId);

                if (!existePersonaje)
                {
                    return Results.BadRequest(
                        $"El personaje con ID {participanteId} no existe"
                    );
                }

                bool murioAnteriormente = CatalogoStore.Eventos
                    .Any(e =>
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

            foreach (int fallecidoId in evento.Fallecidos)
            {
                if (!evento.Participantes.Contains(fallecidoId))
                {
                    return Results.BadRequest(
                        $"El personaje fallecido con ID {fallecidoId} debe ser participante del evento"
                    );
                }
            }

            int nuevoId = CatalogoStore.Eventos.Count == 0
                ? 1
                : CatalogoStore.Eventos.Max(e => e.Id) + 1;

            var nuevoEvento = evento with
            {
                Id = nuevoId
            };

            CatalogoStore.Eventos.Add(nuevoEvento);

            foreach (int fallecidoId in evento.Fallecidos)
            {
                int posicion = CatalogoStore.Personajes
                    .FindIndex(p => p.Id == fallecidoId);

                var personaje = CatalogoStore.Personajes[posicion];

                CatalogoStore.Personajes[posicion] = personaje with
                {
                    Estado = "muerto"
                };
            }

            return Results.Created(
                $"/eventos/{nuevoId}",
                nuevoEvento
            );
        });

        // PUT - Modificar un evento
        app.MapPut("/eventos/{id:int}", (
            int id,
            Evento datos) =>
        {
            int posicionEvento = CatalogoStore.Eventos
                .FindIndex(e => e.Id == id);

            if (posicionEvento == -1)
            {
                return Results.NotFound("Evento no encontrado");
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

            foreach (int participanteId in datos.Participantes)
            {
                bool existePersonaje = CatalogoStore.Personajes
                    .Any(p => p.Id == participanteId);

                if (!existePersonaje)
                {
                    return Results.BadRequest(
                        $"El personaje con ID {participanteId} no existe"
                    );
                }

                bool murioAnteriormente = CatalogoStore.Eventos
                    .Any(e =>
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
            }

            var eventoActualizado = datos with
            {
                Id = id
            };

            CatalogoStore.Eventos[posicionEvento] =
                eventoActualizado;

            for (int i = 0;
                 i < CatalogoStore.Personajes.Count;
                 i++)
            {
                var personaje = CatalogoStore.Personajes[i];

                bool apareceComoFallecido =
                    CatalogoStore.Eventos.Any(e =>
                        e.Fallecidos.Contains(personaje.Id)
                    );

                if (apareceComoFallecido)
                {
                    CatalogoStore.Personajes[i] =
                        personaje with
                        {
                            Estado = "muerto"
                        };
                }
            }

            return Results.Ok(eventoActualizado);
        });

        // GET - Obtener el MVP de un evento
        app.MapGet("/eventos/{id:int}/mvp", (
            int id,
            CatalogoService service) =>
        {
            var evento = service.BuscarEvento(id);

            if (evento is null)
            {
                return Results.NotFound("Evento no encontrado");
            }

            var mvp = CatalogoStore.Cartas
                .Where(c =>
                    evento.Participantes.Contains(c.PersonajeId)
                )
                .OrderByDescending(c => c.Poder)
                .FirstOrDefault();

            if (mvp is null)
            {
                return Results.BadRequest(
                    "No hay cartas disponibles para los participantes del evento"
                );
            }

            var personaje = service.BuscarPersonaje(
                mvp.PersonajeId
            );

            return Results.Ok(new
            {
                Evento = evento.Nombre,
                PersonajeId = mvp.PersonajeId,
                Personaje = personaje?.Nombre,
                Poder = mvp.Poder,
                HabilidadEspecial = mvp.HabilidadEspecial
            });
        });

        // POST - Simular un evento
        app.MapPost("/eventos/{id:int}/simular", (
            int id,
            CatalogoService service) =>
        {
            var evento = service.BuscarEvento(id);

            if (evento is null)
            {
                return Results.NotFound("Evento no encontrado");
            }

            if (evento.Participantes.Count < 2)
            {
                return Results.BadRequest(
                    "Se necesitan al menos dos participantes para simular el evento"
                );
            }

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
                    PoderBase = grupo.Sum(p => p.Poder)
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
                        Random.Shared.Next(90, 111) / 100.0;

                    double poderFinal =
                        bando.PoderBase * factorAleatorio;

                    return new
                    {
                        bando.Faccion,
                        bando.PoderBase,
                        FactorAleatorio = factorAleatorio,
                        PoderFinal = Math.Round(
                            poderFinal,
                            2
                        )
                    };
                })
                .OrderByDescending(b => b.PoderFinal)
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
        });
    }
}