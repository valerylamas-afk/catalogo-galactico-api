
namespace PersonajesApi.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Correo { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string? NotaPrivadaCifrada { get; set; }
}
