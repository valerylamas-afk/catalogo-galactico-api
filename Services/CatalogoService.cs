using PersonajesApi.Data;
using PersonajesApi.Models;

namespace PersonajesApi.Services;

public class CatalogoService
{
    public Personaje? BuscarPersonaje(int id)
    {
        return CatalogoStore.Personajes
            .FirstOrDefault(p => p.Id == id);
    }

    public CardPersonaje? BuscarCarta(int id)
    {
        return CatalogoStore.Cartas
            .FirstOrDefault(c => c.Id == id);
    }

    public Evento? BuscarEvento(int id)
    {
        return CatalogoStore.Eventos
            .FirstOrDefault(e => e.Id == id);
    }

    public bool ExistePersonaje(int id)
    {
        return CatalogoStore.Personajes
            .Any(p => p.Id == id);
    }

    public bool PersonajeTieneCarta(int personajeId)
    {
        return CatalogoStore.Cartas
            .Any(c => c.PersonajeId == personajeId);
    }

    public bool MurioAntesDe(int personajeId, int fecha)
    {
        return CatalogoStore.Eventos
            .Any(e =>
                e.Fecha < fecha &&
                e.Fallecidos.Contains(personajeId)
            );
    }
}
