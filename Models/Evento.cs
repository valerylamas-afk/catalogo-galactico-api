
namespace PersonajesApi.Models;

public class Evento
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public int Fecha { get; set; }
    public string Ubicacion { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public List<int> Participantes { get; set; } = new();
    public List<int> Fallecidos { get; set; } = new();
    public string Resultado { get; set; } = "";
    public int? GanadorId { get; set; }

    // Constructor vacío necesario para Entity Framework Core
    public Evento()
    {
    }

    // Constructor utilizado por CatalogoStore y EventosController
    public Evento(
        int id,
        string nombre,
        int fecha,
        string ubicacion,
        string descripcion,
        List<int> participantes,
        List<int> fallecidos,
        string resultado,
        int? ganadorId)
    {
        Id = id;
        Nombre = nombre;
        Fecha = fecha;
        Ubicacion = ubicacion;
        Descripcion = descripcion;
        Participantes = participantes;
        Fallecidos = fallecidos;
        Resultado = resultado;
        GanadorId = ganadorId;
    }
}
