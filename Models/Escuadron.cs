
namespace PersonajesApi.Models;

public class Escuadron
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Faccion { get; set; } = "";
    public int Nivel { get; set; }
    public int LiderId { get; set; }

    public Personaje Lider { get; set; } = null!;

    public List<EscuadronIntegrante> Integrantes { get; set; } = new();
}
