
namespace PersonajesApi.Models;

public class CardPersonaje
{
    public int Id { get; set; }
    public int PersonajeId { get; set; }
    public int Poder { get; set; }
    public string HabilidadEspecial { get; set; } = "";
    public string Arma { get; set; } = "";
    public int NivelPeligrosidad { get; set; }
    public string ImagenUrl { get; set; } = "";

    public CardPersonaje()
    {
    }

    public CardPersonaje(
        int id,
        int personajeId,
        int poder,
        string habilidadEspecial,
        string arma,
        int nivelPeligrosidad,
        string imagenUrl)
    {
        Id = id;
        PersonajeId = personajeId;
        Poder = poder;
        HabilidadEspecial = habilidadEspecial;
        Arma = arma;
        NivelPeligrosidad = nivelPeligrosidad;
        ImagenUrl = imagenUrl;
    }
}
