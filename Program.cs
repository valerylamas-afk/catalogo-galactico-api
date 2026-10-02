using PersonajesApi.Data;
using PersonajesApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// PERSONAJES

// GET - Obtener personajes con filtros opcionales
app.MapGet("/personajes", (string? faccion, bool? fuerzaSensitivo) =>
{
    var personajes = CatalogoStore.Personajes.AsEnumerable();

    if (!string.IsNullOrWhiteSpace(faccion))
    {
        personajes = personajes
            .Where(p => p.Faccion.Equals(
                faccion,
                StringComparison.OrdinalIgnoreCase
            ));
    }

    if (fuerzaSensitivo.HasValue)
    {
        personajes = personajes
            .Where(p => p.FuerzaSensitivo == fuerzaSensitivo.Value);
    }

    return Results.Ok(personajes);
});


// GET - Obtener un personaje por ID
app.MapGet("/personajes/{id:int}", (int id) =>
{
    var personaje = CatalogoStore.Personajes
        .FirstOrDefault(p => p.Id == id);

    if (personaje is null)
    {
        return Results.NotFound("Personaje no encontrado");
    }

    return Results.Ok(personaje);
});

// GET - Obtener los eventos de un personaje
app.MapGet("/personajes/{id:int}/eventos", (int id) =>
{
    var personaje = CatalogoStore.Personajes
        .FirstOrDefault(p => p.Id == id);

    if (personaje is null)
    {
        return Results.NotFound("Personaje no encontrado");
    }

    var eventos = CatalogoStore.Eventos
        .Where(e => e.Participantes.Contains(id))
        .ToList();

    return Results.Ok(eventos);
});

// POST - Crear un personaje
app.MapPost("/personajes", (Personaje personaje) =>
{
    if (string.IsNullOrWhiteSpace(personaje.Nombre))
    {
        return Results.BadRequest("El nombre es obligatorio");
    }

    if (personaje.Faccion != "Rebelde" &&
        personaje.Faccion != "Imperio" &&
        personaje.Faccion != "Neutral")
    {
        return Results.BadRequest(
            "La facción debe ser Rebelde, Imperio o Neutral"
        );
    }

    if (personaje.Estado != "vivo" &&
        personaje.Estado != "muerto" &&
        personaje.Estado != "desconocido")
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


// PUT - Modificar un personaje
app.MapPut("/personajes/{id:int}", (int id, Personaje datos) =>
{
    int posicion = CatalogoStore.Personajes
        .FindIndex(p => p.Id == id);

    if (posicion == -1)
    {
        return Results.NotFound("Personaje no encontrado");
    }

    if (string.IsNullOrWhiteSpace(datos.Nombre))
    {
        return Results.BadRequest("El nombre es obligatorio");
    }

    if (datos.Faccion != "Rebelde" &&
        datos.Faccion != "Imperio" &&
        datos.Faccion != "Neutral")
    {
        return Results.BadRequest(
            "La facción debe ser Rebelde, Imperio o Neutral"
        );
    }

    if (datos.Estado != "vivo" &&
        datos.Estado != "muerto" &&
        datos.Estado != "desconocido")
    {
        return Results.BadRequest(
            "El estado debe ser vivo, muerto o desconocido"
        );
    }

    var personajeActualizado = datos with
    {
        Id = id
    };

    CatalogoStore.Personajes[posicion] = personajeActualizado;

    return Results.Ok(personajeActualizado);
});


// DELETE - Eliminar un personaje
app.MapDelete("/personajes/{id:int}", (int id) =>
{
    var personaje = CatalogoStore.Personajes
        .FirstOrDefault(p => p.Id == id);

    if (personaje is null)
    {
        return Results.NotFound("Personaje no encontrado");
    }

    CatalogoStore.Personajes.Remove(personaje);

    return Results.NoContent();
});


// GET - Ranking de personajes por poder
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


// CARTAS

// GET - Obtener todas las cartas
app.MapGet("/cartas", () =>
{
    return Results.Ok(CatalogoStore.Cartas);
});


// GET - Obtener una carta por ID
app.MapGet("/cartas/{id:int}", (int id) =>
{
    var carta = CatalogoStore.Cartas
        .FirstOrDefault(c => c.Id == id);

    if (carta is null)
    {
        return Results.NotFound("Carta no encontrada");
    }

    return Results.Ok(carta);
});


// POST - Crear una carta
app.MapPost("/cartas", (CardPersonaje carta) =>
{
    var personaje = CatalogoStore.Personajes
        .FirstOrDefault(p => p.Id == carta.PersonajeId);

    if (personaje is null)
    {
        return Results.BadRequest("El personaje indicado no existe");
    }

    bool tieneCarta = CatalogoStore.Cartas
        .Any(c => c.PersonajeId == carta.PersonajeId);

    if (tieneCarta)
    {
        return Results.BadRequest("El personaje ya tiene una carta");
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
app.MapPut("/cartas/{id:int}", (int id, CardPersonaje datos) =>
{
    int posicion = CatalogoStore.Cartas
        .FindIndex(c => c.Id == id);

    if (posicion == -1)
    {
        return Results.NotFound("Carta no encontrada");
    }

    var personaje = CatalogoStore.Personajes
        .FirstOrDefault(p => p.Id == datos.PersonajeId);

    if (personaje is null)
    {
        return Results.BadRequest("El personaje indicado no existe");
    }

    bool tieneOtraCarta = CatalogoStore.Cartas
        .Any(c => c.PersonajeId == datos.PersonajeId && c.Id != id);

    if (tieneOtraCarta)
    {
        return Results.BadRequest("El personaje ya tiene otra carta");
    }

    var cartaActualizada = datos with
    {
        Id = id
    };

    CatalogoStore.Cartas[posicion] = cartaActualizada;

    return Results.Ok(cartaActualizada);
});

// ==========================================
// EVENTOS
// ==========================================

// GET - Obtener todos los eventos
app.MapGet("/eventos", () =>
{
    return Results.Ok(CatalogoStore.Eventos);
});


// GET - Obtener un evento por ID
app.MapGet("/eventos/{id:int}", (int id) =>
{
    var evento = CatalogoStore.Eventos
        .FirstOrDefault(e => e.Id == id);

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
        return Results.BadRequest("El nombre del evento es obligatorio");
    }

    if (evento.Participantes is null || evento.Participantes.Count == 0)
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
    }

    int nuevoId = CatalogoStore.Eventos.Count == 0
        ? 1
        : CatalogoStore.Eventos.Max(e => e.Id) + 1;

    var nuevoEvento = evento with
    {
        Id = nuevoId
    };

    CatalogoStore.Eventos.Add(nuevoEvento);

    return Results.Created(
        $"/eventos/{nuevoId}",
        nuevoEvento
    );
});


// PUT - Modificar un evento
app.MapPut("/eventos/{id:int}", (int id, Evento datos) =>
{
    int posicion = CatalogoStore.Eventos
        .FindIndex(e => e.Id == id);

    if (posicion == -1)
    {
        return Results.NotFound("Evento no encontrado");
    }

    if (string.IsNullOrWhiteSpace(datos.Nombre))
    {
        return Results.BadRequest(
            "El nombre del evento es obligatorio"
        );
    }

    if (datos.Participantes is null || datos.Participantes.Count == 0)
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
    }

    var eventoActualizado = datos with
    {
        Id = id
    };

    CatalogoStore.Eventos[posicion] = eventoActualizado;

    return Results.Ok(eventoActualizado);
});

// GET - Obtener el MVP de un evento
app.MapGet("/eventos/{id:int}/mvp", (int id) =>
{
    var evento = CatalogoStore.Eventos
        .FirstOrDefault(e => e.Id == id);

    if (evento is null)
    {
        return Results.NotFound("Evento no encontrado");
    }

    var mvp = CatalogoStore.Cartas
        .Where(c => evento.Participantes.Contains(c.PersonajeId))
        .OrderByDescending(c => c.Poder)
        .FirstOrDefault();

    if (mvp is null)
    {
        return Results.BadRequest(
            "No hay cartas disponibles para los participantes del evento"
        );
    }

    var personaje = CatalogoStore.Personajes
        .FirstOrDefault(p => p.Id == mvp.PersonajeId);

    return Results.Ok(new
    {
        Evento = evento.Nombre,
        PersonajeId = mvp.PersonajeId,
        Personaje = personaje?.Nombre,
        Poder = mvp.Poder,
        HabilidadEspecial = mvp.HabilidadEspecial
    });
});

app.Run();