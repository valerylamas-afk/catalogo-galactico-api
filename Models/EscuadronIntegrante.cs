
namespace PersonajesApi.Models;

public class EscuadronIntegrante
{
    public int EscuadronId { get; set; }
    public Escuadron Escuadron { get; set; } = null!;

    public int PersonajeId { get; set; }
    public Personaje Personaje { get; set; } = null!;
}
