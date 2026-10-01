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

// GET - Obtener todos los personajes
app.MapGet("/personajes", () =>
{
    return Results.Ok(CatalogoStore.Personajes);
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


app.Run();