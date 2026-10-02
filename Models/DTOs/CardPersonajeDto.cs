namespace PersonajesApi.Models.DTOs;

public record CardPersonajeDto(
    int PersonajeId,
    int Poder,
    string HabilidadEspecial,
    string Arma,
    int NivelPeligrosidad,
    string ImagenUrl
);