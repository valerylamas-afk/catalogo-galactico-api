using PersonajesApi.Data;
using PersonajesApi.Models;
using PersonajesApi.Services;

namespace PersonajesApi.Endpoints;

public static class PersonajesEndpoints
{
    public static void MapPersonajesEndpoints(this WebApplication app)
    {
        // GET - Listar personajes con filtros opcionales
        app.MapGet("/personajes", (
            string? faccion,
            bool? fuerzaSensitivo) =>
        {
            var personajes = CatalogoStore.Personajes.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(faccion))
            {
                personajes = personajes.Where(p =>
                    p.Faccion.Equals(
                        faccion,
                        StringComparison.OrdinalIgnoreCase
                    )
                );
            }

            if (fuerzaSensitivo.HasValue)
            {
                personajes = personajes.Where(p =>
                    p.FuerzaSensitivo == fuerzaSensitivo.Value
                );
            }

            return Results.Ok(personajes);
        });

        // GET - Buscar personaje por ID
        app.MapGet("/personajes/{id:int}", (
            int id,
            CatalogoService service) =>
        {
            var personaje = service.BuscarPersonaje(id);

            if (personaje is null)
            {
                return Results.NotFound(
                    "Personaje no encontrado"
                );
            }

            return Results.Ok(personaje);
        });

        // POST - Crear personaje
        app.MapPost("/personajes", (Personaje personaje) =>
        {
            if (string.IsNullOrWhiteSpace(personaje.Nombre))
            {
                return Results.BadRequest(
                    "El nombre es obligatorio"
                );
            }

            string[] facciones =
            [
                "Rebelde",
                "Imperio",
                "Neutral"
            ];

            if (!facciones.Contains(personaje.Faccion))
            {
                return Results.BadRequest(
                    "La facción debe ser Rebelde, Imperio o Neutral"
                );
            }

            string[] estados =
            [
                "vivo",
                "muerto",
                "desconocido"
            ];

            if (!estados.Contains(personaje.Estado))
            {
                return Results.BadRequest(
                    "El estado debe ser vivo, muerto o desconocido"
                );
            }

            int nuevoId = CatalogoStore.Personajes.Count == 0
                ? 1
                : CatalogoStore.Personajes.Max(p => p.Id) + 1;

            var nuevoPersonaje = personaje with
            {
                Id = nuevoId
            };

            CatalogoStore.Personajes.Add(nuevoPersonaje);

            return Results.Created(
                $"/personajes/{nuevoId}",
                nuevoPersonaje
            );
        });

        // PUT - Modificar personaje
        app.MapPut("/personajes/{id:int}", (
            int id,
            Personaje datos) =>
        {
            int posicion = CatalogoStore.Personajes
                .FindIndex(p => p.Id == id);

            if (posicion == -1)
            {
                return Results.NotFound(
                    "Personaje no encontrado"
                );
            }

            if (string.IsNullOrWhiteSpace(datos.Nombre))
            {
                return Results.BadRequest(
                    "El nombre es obligatorio"
                );
            }

            string[] facciones =
            [
                "Rebelde",
                "Imperio",
                "Neutral"
            ];

            if (!facciones.Contains(datos.Faccion))
            {
                return Results.BadRequest(
                    "La facción debe ser Rebelde, Imperio o Neutral"
                );
            }

            string[] estados =
            [
                "vivo",
                "muerto",
                "desconocido"
            ];

            if (!estados.Contains(datos.Estado))
            {
                return Results.BadRequest(
                    "El estado debe ser vivo, muerto o desconocido"
                );
            }

            var personajeActualizado = datos with
            {
                Id = id
            };

            CatalogoStore.Personajes[posicion] =
                personajeActualizado;

            return Results.Ok(personajeActualizado);
        });

        // DELETE - Eliminar personaje
        app.MapDelete("/personajes/{id:int}", (int id) =>
        {
            var personaje = CatalogoStore.Personajes
                .FirstOrDefault(p => p.Id == id);

            if (personaje is null)
            {
                return Results.NotFound(
                    "Personaje no encontrado"
                );
            }

            CatalogoStore.Personajes.Remove(personaje);

            return Results.NoContent();
        });

        // GET - Eventos en los que participó un personaje
        app.MapGet("/personajes/{id:int}/eventos", (
            int id,
            CatalogoService service) =>
        {
            var personaje = service.BuscarPersonaje(id);

            if (personaje is null)
            {
                return Results.NotFound(
                    "Personaje no encontrado"
                );
            }

            var eventos = CatalogoStore.Eventos
                .Where(e => e.Participantes.Contains(id))
                .ToList();

            return Results.Ok(eventos);
        });

        // GET - Ranking de personajes
        app.MapGet("/personajes/ranking", (string? por) =>
        {
            if (por != "poder")
            {
                return Results.BadRequest(
                    "El parámetro 'por' debe ser 'poder'"
                );
            }

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
                    }
                )
                .OrderByDescending(x => x.Poder)
                .ToList();

            return Results.Ok(ranking);
        });
    }
}