using PersonajesApi.Models;

namespace PersonajesApi.Data;
public static class CatalogoStore
{  
public static List<Personaje> Personajes { get; } =
[
    new Personaje
    {
        Id = 1,
        Nombre = "Luke Skywalker",
        Especie = "Humano",
        Faccion = "Rebelde",
        Afiliacion = "Alianza Rebelde",
        Estado = "vivo",
        FuerzaSensitivo = true
    },
    new Personaje
    {
        Id = 2,
        Nombre = "Darth Vader",
        Especie = "Humano",
        Faccion = "Imperio",
        Afiliacion = "Imperio Galáctico",
        Estado = "vivo",
        FuerzaSensitivo = true
    },
    new Personaje
    {
        Id = 3,
        Nombre = "Han Solo",
        Especie = "Humano",
        Faccion = "Rebelde",
        Afiliacion = "Alianza Rebelde",
        Estado = "vivo",
        FuerzaSensitivo = false
    },
    new Personaje
    {
        Id = 4,
        Nombre = "Chewbacca",
        Especie = "Wookiee",
        Faccion = "Rebelde",
        Afiliacion = "Alianza Rebelde",
        Estado = "vivo",
        FuerzaSensitivo = false
    }
];


    public static List<CardPersonaje> Cartas { get; } =
    [
        new(1, 1, 95, "Dominio de la Fuerza", "Sable de luz", 9, "luke.jpg"),
        new(2, 2, 98, "Estrangulamiento con la Fuerza", "Sable de luz", 10, "vader.jpg"),
        new(3, 3, 80, "Disparo preciso", "Bláster", 7, "han.jpg"),
        new(4, 4, 85, "Fuerza Wookiee", "Ballesta láser", 8, "chewbacca.jpg")
    ];

    public static List<Evento> Eventos { get; } =
[
    new(
        1,
        "Batalla de Yavin",
        0,
        "Yavin 4",
        "Batalla entre la Alianza Rebelde y el Imperio.",
        [1, 2, 3, 4],
        [],
        "Victoria Rebelde",
        1
    ),

    new(
        2,
        "Batalla de Hoth",
        3,
        "Hoth",
        "Ataque del Imperio a la base rebelde.",
        [1, 2, 3, 4],
        [],
        "Victoria del Imperio",
        2
    )
];
}