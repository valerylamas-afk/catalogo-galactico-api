using System.Security.Cryptography;
using System.Text;

namespace PersonajesApi.Services;

// Servicio encargado de cifrar y descifrar informacion.
// Utilizamos AES-GCM, un algoritmo de cifrado simetrico.
public class CifradoService
{
    // Clave secreta que se utiliza para cifrar y descifrar.
    // Es de 32 bytes, equivalentes a 256 bits.
    private readonly byte[] _clave;

    // IConfiguration permite leer valores de configuracion.
    // En nuestro proyecto la clave se guarda en User Secrets.
    public CifradoService(IConfiguration configuracion)
    {
        // Leemos la clave sin escribirla directamente en el codigo.
        string? clave = configuracion["Encryption:Key"];

        // Si no existe la clave, impedimos continuar.
        if (string.IsNullOrWhiteSpace(clave))
            throw new Exception("Falta la clave de cifrado");

        // La clave se guardo en Base64.
        // La convertimos nuevamente a bytes.
        _clave = Convert.FromBase64String(clave);

        // AES-256 necesita una clave de 32 bytes.
        if (_clave.Length != 32)
            throw new Exception("La clave debe tener 32 bytes");
    }

    // CIFRAR: transforma el texto original en datos cifrados.
    public string Cifrar(string texto)
    {
        // Nonce: valor aleatorio de 12 bytes.
        // Generamos uno nuevo cada vez que ciframos.
        byte[] nonce = RandomNumberGenerator.GetBytes(12);

        // Convertimos el texto original a bytes UTF-8.
        byte[] datos = Encoding.UTF8.GetBytes(texto);

        // Arreglo donde se guardara el contenido cifrado.
        byte[] cifrado = new byte[datos.Length];

        // Tag: etiqueta de 16 bytes para comprobar
        // que los datos cifrados no fueron alterados.
        byte[] tag = new byte[16];

        // Creamos AES-GCM con nuestra clave secreta.
        using var aes = new AesGcm(_clave, 16);

        // Ciframos el texto utilizando nonce, datos y tag.
        // El resultado se escribe en cifrado.
        aes.Encrypt(nonce, datos, cifrado, tag);

        // Unimos nonce + tag + texto cifrado.
        // Necesitamos los tres para poder descifrar luego.
        byte[] resultado =
            new byte[nonce.Length + tag.Length + cifrado.Length];

        // Copiamos el nonce al comienzo del arreglo.
        Buffer.BlockCopy(
            nonce, 0, resultado, 0, nonce.Length);

        // Despues copiamos el tag.
        Buffer.BlockCopy(
            tag, 0, resultado, nonce.Length, tag.Length);

        // Finalmente copiamos el contenido cifrado.
        Buffer.BlockCopy(
            cifrado, 0, resultado,
            nonce.Length + tag.Length, cifrado.Length);

        // Convertimos todo a Base64 para guardarlo
        // como una cadena de texto en MySQL.
        return Convert.ToBase64String(resultado);
    }

    // DESCIFRAR: recupera el texto original usando la clave.
    public string Descifrar(string textoCifrado)
    {
        // Convertimos el Base64 almacenado nuevamente a bytes.
        byte[] contenido = Convert.FromBase64String(textoCifrado);

        // El minimo son 12 bytes de nonce y 16 de tag.
        if (contenido.Length < 28)
            throw new Exception("Datos cifrados invalidos");

        // Extraemos los componentes en el mismo orden:
        // nonce (12), tag (16) y contenido cifrado (resto).
        byte[] nonce = contenido[..12];
        byte[] tag = contenido[12..28];
        byte[] cifrado = contenido[28..];

        // Preparamos un arreglo para recuperar el texto.
        byte[] resultado = new byte[cifrado.Length];

        // Utilizamos la misma clave AES-GCM.
        using var aes = new AesGcm(_clave, 16);

        // Si la clave, el nonce o el tag no coinciden,
        // el descifrado falla y no devuelve el texto.
        aes.Decrypt(nonce, cifrado, tag, resultado);

        // Convertimos los bytes descifrados a texto UTF-8.
        return Encoding.UTF8.GetString(resultado);
    }
}
