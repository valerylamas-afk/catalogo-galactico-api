namespace PersonajesApi.Models.DTOs;

public record EventoDto(
    string Nombre,
    int Fecha,
    string Ubicacion,
    string Descripcion,
    List<int> Participantes,
    List<int> Fallecidos,
    string Resultado,
    int? GanadorId
);