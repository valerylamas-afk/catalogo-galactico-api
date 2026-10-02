using PersonajesApi.Data;
using PersonajesApi.Models;
using PersonajesApi.Services;

namespace PersonajesApi.Endpoints;

public static class CartasEndpoints
{
    public static void MapCartasEndpoints(this WebApplication app)
    {
        // GET - Obtener todas las cartas
        app.MapGet("/cartas", () =>
        {
            return Results.Ok(CatalogoStore.Cartas);
        });

        // GET - Obtener una carta por ID
        app.MapGet("/cartas/{id:int}", (
            int id,
            CatalogoService service) =>
        {
            var carta = service.BuscarCarta(id);

            if (carta is null)
            {
                return Results.NotFound("Carta no encontrada");
            }

            return Results.Ok(carta);
        });

        // POST - Crear una carta
        app.MapPost("/cartas", (
            CardPersonaje carta,
            CatalogoService service) =>
        {
            if (!service.ExistePersonaje(carta.PersonajeId))
            {
                return Results.BadRequest(
                    "El personaje indicado no existe"
                );
            }

            if (service.PersonajeTieneCarta(carta.PersonajeId))
            {
                return Results.BadRequest(
                    "El personaje ya tiene una carta"
                );
            }

            int nuevoId = CatalogoStore.Cartas.Count == 0
                ? 1
                : CatalogoStore.Cartas.Max(c => c.Id) + 1;

            var nuevaCarta = carta with
            {
                Id = nuevoId
            };

            CatalogoStore.Cartas.Add(nuevaCarta);

            return Results.Created(
                $"/cartas/{nuevoId}",
                nuevaCarta
            );
        });

        // PUT - Modificar una carta
        app.MapPut("/cartas/{id:int}", (
            int id,
            CardPersonaje datos,
            CatalogoService service) =>
        {
            int posicion = CatalogoStore.Cartas
                .FindIndex(c => c.Id == id);

            if (posicion == -1)
            {
                return Results.NotFound("Carta no encontrada");
            }

            if (!service.ExistePersonaje(datos.PersonajeId))
            {
                return Results.BadRequest(
                    "El personaje indicado no existe"
                );
            }

            bool tieneOtraCarta = CatalogoStore.Cartas
                .Any(c =>
                    c.PersonajeId == datos.PersonajeId &&
                    c.Id != id
                );

            if (tieneOtraCarta)
            {
                return Results.BadRequest(
                    "El personaje ya tiene otra carta"
                );
            }

            var cartaActualizada = datos with
            {
                Id = id
            };

            CatalogoStore.Cartas[posicion] = cartaActualizada;

            return Results.Ok(cartaActualizada);
        });
    }
}