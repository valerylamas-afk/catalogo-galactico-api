
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonajesApi.Data;
using PersonajesApi.Models;
using PersonajesApi.Models.DTOs;

namespace PersonajesApi.Controllers;

// ApiController permite manejar peticiones HTTP.
// Todas las rutas comienzan con /personajes.
[ApiController]
[Route("personajes")]
public class PersonajesController : ControllerBase
{
    private readonly AppDbContext _db;

    // Inyeccion de dependencias:
    // Recibimos AppDbContext para trabajar con MySQL.
    public PersonajesController(AppDbContext db)
    {
        _db = db;
    }

    // GET /personajes
    // Lista personajes y permite filtros opcionales.
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? faccion,
        [FromQuery] bool? fuerzaSensitivo)
    {
        // AsQueryable permite construir una consulta
        // antes de ejecutarla en la base de datos.
        var consulta = _db.Personajes.AsQueryable();

        // Si se recibe una faccion, filtramos por ella.
        // ToLower permite comparar sin distinguir mayusculas.
        if (!string.IsNullOrWhiteSpace(faccion))
        {
            consulta = consulta.Where(p =>
                p.Faccion.ToLower() == faccion.ToLower());
        }

        // HasValue comprueba si enviaron el filtro booleano.
        // Si no se envia, se muestran ambos valores.
        if (fuerzaSensitivo.HasValue)
        {
            consulta = consulta.Where(p =>
                p.FuerzaSensitivo == fuerzaSensitivo.Value);
        }

        // ToListAsync ejecuta la consulta en MySQL.
        // Ok devuelve HTTP 200 con los resultados.
        return Ok(await consulta.ToListAsync());
    }

    // GET /personajes/ranking?por=poder
    // Ordena personajes segun el poder de sus cartas.
    [HttpGet("ranking")]
    public async Task<IActionResult> Ranking(
        [FromQuery] string? por)
    {
        // Solo aceptamos el criterio de ordenamiento "poder".
        if (por != "poder")
            return BadRequest(
                "El parámetro 'por' debe ser 'poder'");

        // Join relaciona Personajes y Cartas
        // mediante Personaje.Id y Carta.PersonajeId.
        var ranking = await _db.Personajes
            .Join(
                _db.Cartas,
                personaje => personaje.Id,
                carta => carta.PersonajeId,
                (personaje, carta) => new
                {
                    personaje.Id,
                    personaje.Nombre,
                    personaje.Faccion,
                    Poder = carta.Poder
                })

            // Mayor poder primero.
            .OrderByDescending(x => x.Poder)
            .ToListAsync();

        return Ok(ranking);
    }

    // GET /personajes/{id}
    // Busca un personaje mediante su ID.
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        // FindAsync busca mediante la clave primaria.
        var personaje = await _db.Personajes.FindAsync(id);

        // HTTP 404 si el personaje no existe.
        if (personaje is null)
            return NotFound("Personaje no encontrado");

        return Ok(personaje);
    }

    // POST /personajes
    // Crea un nuevo personaje en MySQL.
    [HttpPost]
    public async Task<IActionResult> Crear(PersonajeDto datos)
    {
        // Primero comprobamos las reglas de negocio.
        var error = Validar(datos);

        if (error != null)
            return BadRequest(error);

        // Creamos el objeto a partir de los datos del DTO.
        // No asignamos Id porque MySQL lo genera.
        var nuevo = new Personaje
        {
            Nombre = datos.Nombre,
            Especie = datos.Especie,
            Faccion = datos.Faccion,
            Afiliacion = datos.Afiliacion,
            Estado = datos.Estado,
            FuerzaSensitivo = datos.FuerzaSensitivo
        };

        // Add prepara la insercion del personaje.
        _db.Personajes.Add(nuevo);

        // SaveChangesAsync guarda los cambios en MySQL.
        await _db.SaveChangesAsync();

        // HTTP 201 significa que se creo correctamente.
        return Created($"/personajes/{nuevo.Id}", nuevo);
    }

    // PUT /personajes/{id}
    // Actualiza la informacion de un personaje existente.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(
        int id, PersonajeDto datos)
    {
        var personaje = await _db.Personajes.FindAsync(id);

        if (personaje is null)
            return NotFound("Personaje no encontrado");

        // Reutilizamos las mismas validaciones del POST.
        var error = Validar(datos);

        if (error != null)
            return BadRequest(error);

        // Modificamos las propiedades del registro.
        // EF Core detecta los cambios de la entidad.
        personaje.Nombre = datos.Nombre;
        personaje.Especie = datos.Especie;
        personaje.Faccion = datos.Faccion;
        personaje.Afiliacion = datos.Afiliacion;
        personaje.Estado = datos.Estado;
        personaje.FuerzaSensitivo = datos.FuerzaSensitivo;

        await _db.SaveChangesAsync();

        return Ok(personaje);
    }

    // DELETE /personajes/{id}
    // Elimina personajes sin relaciones asociadas.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var personaje = await _db.Personajes.FindAsync(id);

        if (personaje is null)
            return NotFound("Personaje no encontrado");

        // AnyAsync comprueba si tiene una carta relacionada.
        // Si tiene carta, no permitimos eliminarlo.
        bool tieneCarta = await _db.Cartas
            .AnyAsync(c => c.PersonajeId == id);

        if (tieneCarta)
            return BadRequest(
                "No se puede eliminar el personaje porque tiene una carta asociada");

        // Comprobamos si aparece como ganador de un evento.
        bool esGanador = await _db.Eventos
            .AnyAsync(e => e.GanadorId == id);

        // Participantes y Fallecidos se guardan como JSON.
        // Por eso consultamos los eventos y revisamos sus listas.
        var eventos = await _db.Eventos.ToListAsync();

        bool participaEnEvento = esGanador ||
            eventos.Any(e =>
                e.Participantes.Contains(id) ||
                e.Fallecidos.Contains(id));

        if (participaEnEvento)
            return BadRequest(
                "No se puede eliminar el personaje porque está relacionado con un evento");
        // Comprobamos si el personaje lidera un escuadron.
        bool esLider = await _db.Escuadrones
            .AnyAsync(e => e.LiderId == id);

        if (esLider)
            return BadRequest(
                "No se puede eliminar el personaje porque lidera un escuadrón");

        // Comprobamos si pertenece a algun escuadron.
        bool esIntegrante = await _db.EscuadronIntegrantes
            .AnyAsync(i => i.PersonajeId == id);

        if (esIntegrante)
            return BadRequest(
                "No se puede eliminar el personaje porque pertenece a un escuadrón");

        // Solo eliminamos si supera las validaciones anteriores.
        _db.Personajes.Remove(personaje);
        await _db.SaveChangesAsync();

        // HTTP 204 indica eliminacion sin contenido de respuesta.
        return NoContent();
    }

    // GET /personajes/{id}/eventos
    // Devuelve los eventos donde participa un personaje.
    [HttpGet("{id:int}/eventos")]
    public async Task<IActionResult> ObtenerEventos(int id)
    {
        var personaje = await _db.Personajes.FindAsync(id);

        if (personaje is null)
            return NotFound("Personaje no encontrado");

        var eventos = await _db.Eventos.ToListAsync();

        // Contains comprueba si el ID del personaje
        // se encuentra en la lista de participantes.
        var relacionados = eventos
            .Where(e => e.Participantes.Contains(id))
            .ToList();

        return Ok(relacionados);
    }

    // Metodo privado compartido por POST y PUT.
    // Devuelve un mensaje cuando encuentra un error
    // y null cuando todos los datos son validos.
    private string? Validar(PersonajeDto datos)
    {
        // El nombre del personaje es obligatorio.
        if (string.IsNullOrWhiteSpace(datos.Nombre))
            return "El nombre es obligatorio";

        // Solo permitimos estas tres facciones.
        string[] facciones = { "Rebelde", "Imperio", "Neutral" };

        if (!facciones.Contains(datos.Faccion))
            return "La facción debe ser Rebelde, Imperio o Neutral";

        // Solo permitimos estos tres estados.
        string[] estados = { "vivo", "muerto", "desconocido" };

        if (!estados.Contains(datos.Estado))
            return "El estado debe ser vivo, muerto o desconocido";

        return null;
    }
}
