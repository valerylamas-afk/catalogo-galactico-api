namespace PersonajesApi.Models;

public record Personaje(
    int Id,
    string Nombre,
    string Especie,
    string Faccion,
    string Afiliacion,
    string Estado,
    bool FuerzaSensitivo
);