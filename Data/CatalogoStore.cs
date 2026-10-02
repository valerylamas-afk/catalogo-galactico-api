using PersonajesApi.Models;

namespace PersonajesApi.Data;

public static class CatalogoStore
{
    public static List<Personaje> Personajes { get; } =
    [
        new(1, "Luke Skywalker", "Humano", "Rebelde", "Alianza Rebelde", "vivo", true),
        new(2, "Darth Vader", "Humano", "Imperio", "Imperio Galáctico", "muerto", true),
        new(3, "Han Solo", "Humano", "Rebelde", "Alianza Rebelde", "vivo", false),
        new(4, "Chewbacca", "Wookiee", "Rebelde", "Alianza Rebelde", "vivo", false)
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