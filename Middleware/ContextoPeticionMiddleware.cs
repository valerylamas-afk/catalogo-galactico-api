
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace PersonajesApi.Middleware;

public class ContextoPeticionMiddleware
{
    private readonly RequestDelegate _siguiente;
    private readonly ILogger<ContextoPeticionMiddleware> _logger;

    public ContextoPeticionMiddleware(
        RequestDelegate siguiente,
        ILogger<ContextoPeticionMiddleware> logger)
    {
        _siguiente = siguiente;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        // Identificador unico de la peticion.
        string cid = Guid.NewGuid().ToString("N")[..8];

        contexto.Response.Headers["X-Correlation-Id"] = cid;

        // Informacion del cliente.
        string ip = contexto.Connection.RemoteIpAddress?
            .ToString() ?? "desconocido";

        string ua = contexto.Request.Headers.UserAgent.ToString();

        // Generamos una huella con SHA-256.
        byte[] bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes($"{ip}|{ua}")
        );

        string huella = Convert.ToHexString(bytes)
            [..12].ToLowerInvariant();

        // Asociamos los datos a los logs de la peticion.
        using var scope = _logger.BeginScope(
            "cid={Cid} ip={Ip} ua={Ua} huella={Huella}",
            cid, ip, ua, huella
        );

        var reloj = Stopwatch.StartNew();

        try
        {
            await _siguiente(contexto);
        }
        finally
        {
            reloj.Stop();

            string usuario = contexto.User
                .FindFirst("sub")?.Value ?? "anonimo";

            _logger.LogInformation(
                "{Metodo} {Ruta} respondió {Estado} en {Duracion} ms (usuario {Usuario})",
                contexto.Request.Method,
                contexto.Request.Path.Value,
                contexto.Response.StatusCode,
                reloj.ElapsedMilliseconds,
                usuario
            );
        }
    }
}
