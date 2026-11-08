
namespace PersonajesApi.Models;

public class Personaje
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Especie { get; set; } = "";
    public string Faccion { get; set; } = "";
    public string Afiliacion { get; set; } = "";
    public string Estado { get; set; } = "vivo";
    public bool FuerzaSensitivo { get; set; }
}
