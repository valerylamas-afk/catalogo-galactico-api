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

app.MapGet("/", () =>
{
    return Results.Ok("API Catálogo Galáctico funcionando");
});


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

    int nuevoId = CatalogoStore.Personajes.Max(p => p.Id) + 1;

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


app.Run();