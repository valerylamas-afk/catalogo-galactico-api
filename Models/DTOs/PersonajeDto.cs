namespace PersonajesApi.Models.DTOs;

public record PersonajeDto(
    string Nombre,
    string Especie,
    string Faccion,
    string Afiliacion,
    string Estado,
    bool FuerzaSensitivo
);