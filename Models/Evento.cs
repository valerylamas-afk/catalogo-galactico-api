namespace PersonajesApi.Models;

public record Evento(
    int Id,
    string Nombre,
    int Fecha,
    string Ubicacion,
    string Descripcion,
    List<int> Participantes,
    string Resultado,
    int? GanadorId
);