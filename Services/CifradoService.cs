
using System.Security.Cryptography;
using System.Text;

namespace PersonajesApi.Services;

public class CifradoService
{
    private readonly byte[] _clave;

    public CifradoService(IConfiguration configuracion)
    {
        string? clave = configuracion["Encryption:Key"];

        if (string.IsNullOrWhiteSpace(clave))
            throw new Exception("Falta la clave de cifrado");

        _clave = Convert.FromBase64String(clave);

        if (_clave.Length != 32)
            throw new Exception("La clave debe tener 32 bytes");
    }

    public string Cifrar(string texto)
    {
        byte[] nonce = RandomNumberGenerator.GetBytes(12);
        byte[] datos = Encoding.UTF8.GetBytes(texto);
        byte[] cifrado = new byte[datos.Length];
        byte[] tag = new byte[16];

        using var aes = new AesGcm(_clave, 16);

        aes.Encrypt(nonce, datos, cifrado, tag);

        byte[] resultado =
            new byte[nonce.Length + tag.Length + cifrado.Length];

        Buffer.BlockCopy(
            nonce, 0, resultado, 0, nonce.Length);

        Buffer.BlockCopy(
            tag, 0, resultado, nonce.Length, tag.Length);

        Buffer.BlockCopy(
            cifrado, 0, resultado,
            nonce.Length + tag.Length, cifrado.Length);

        return Convert.ToBase64String(resultado);
    }

    public string Descifrar(string textoCifrado)
    {
        byte[] contenido = Convert.FromBase64String(textoCifrado);

        if (contenido.Length < 28)
            throw new Exception("Datos cifrados invalidos");

        byte[] nonce = contenido[..12];
        byte[] tag = contenido[12..28];
        byte[] cifrado = contenido[28..];
        byte[] resultado = new byte[cifrado.Length];

        using var aes = new AesGcm(_clave, 16);

        aes.Decrypt(nonce, cifrado, tag, resultado);

        return Encoding.UTF8.GetString(resultado);
    }
}
