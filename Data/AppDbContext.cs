
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PersonajesApi.Models;
using System.Text.Json;

namespace PersonajesApi.Data;

// DbContext representa el contexto para trabajar con MySQL.
// Permite consultar, agregar, modificar y eliminar datos.
public class AppDbContext : DbContext
{
    // Recibe la configuracion de la conexion desde Program.cs.
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Cada DbSet representa una tabla de nuestra base de datos.
    public DbSet<Personaje> Personajes { get; set; }
    public DbSet<CardPersonaje> Cartas { get; set; }
    public DbSet<Evento> Eventos { get; set; }
    public DbSet<Escuadron> Escuadrones { get; set; }
    public DbSet<EscuadronIntegrante> EscuadronIntegrantes { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }

    // OnModelCreating permite configurar las tablas y relaciones.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ============================
        // TABLA PERSONAJES
        // ============================

        modelBuilder.Entity<Personaje>(entity =>
        {
            // HasKey establece la clave primaria.
            entity.HasKey(p => p.Id);

            // El ID se genera automaticamente al insertar.
            entity.Property(p => p.Id)
                .ValueGeneratedOnAdd();
        });

        // ============================
        // TABLA CARTAS
        // ============================

        modelBuilder.Entity<CardPersonaje>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Id)
                .ValueGeneratedOnAdd();

            // Relacion uno a uno entre Personaje y Carta.
            // Una carta pertenece a un personaje.
            // Un personaje puede tener como maximo una carta.
            entity.HasOne<Personaje>()
                .WithOne()
                .HasForeignKey<CardPersonaje>(c => c.PersonajeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Restrict evita borrar el personaje
            // mientras tenga una carta relacionada.
        });

        // ============================
        // TABLA EVENTOS
        // ============================

        modelBuilder.Entity<Evento>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            // Participantes es una lista de IDs.
            // HasConversion la convierte en texto JSON
            // para poder guardarla y recuperarla de MySQL.
            entity.Property(e => e.Participantes)
                .HasConversion(
                    v => JsonSerializer.Serialize(
                        v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<int>>(
                        v, (JsonSerializerOptions?)null)
                        ?? new List<int>())
                .HasColumnType("longtext");

            // Aplicamos la misma conversion para fallecidos.
            entity.Property(e => e.Fallecidos)
                .HasConversion(
                    v => JsonSerializer.Serialize(
                        v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<int>>(
                        v, (JsonSerializerOptions?)null)
                        ?? new List<int>())
                .HasColumnType("longtext");

            // ValueComparer permite que EF Core compare
            // el contenido de las listas y detecte cambios.
            var comparador = new ValueComparer<List<int>>(
                (a, b) => a!.SequenceEqual(b!),
                v => v.Aggregate(
                    0, (hash, numero) =>
                        HashCode.Combine(hash, numero)),
                v => v.ToList());

            entity.Property(e => e.Participantes)
                .Metadata.SetValueComparer(comparador);

            entity.Property(e => e.Fallecidos)
                .Metadata.SetValueComparer(comparador);

            // Un evento puede tener un ganador.
            // GanadorId es una clave foranea hacia Personajes.
            entity.HasOne<Personaje>()
                .WithMany()
                .HasForeignKey(e => e.GanadorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ============================
        // TABLA ESCUADRONES
        // ============================

        modelBuilder.Entity<Escuadron>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            // Cada escuadron tiene un lider.
            // LiderId apunta a un personaje existente.
            entity.HasOne(e => e.Lider)
                .WithMany()
                .HasForeignKey(e => e.LiderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ============================
        // TABLA ESCUADRON INTEGRANTES
        // ============================

        modelBuilder.Entity<EscuadronIntegrante>(entity =>
        {
            // Clave primaria compuesta por dos IDs.
            // Evita repetir un personaje en el mismo escuadron.
            entity.HasKey(e => new
            {
                e.EscuadronId,
                e.PersonajeId
            });

            // Relacion del integrante con su escuadron.
            // Cascade borra estas relaciones al borrar
            // el escuadron, pero no borra los personajes.
            entity.HasOne(e => e.Escuadron)
                .WithMany(e => e.Integrantes)
                .HasForeignKey(e => e.EscuadronId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relacion del integrante con el personaje.
            // Restrict evita eliminar un personaje
            // mientras pertenezca a un escuadron.
            entity.HasOne(e => e.Personaje)
                .WithMany()
                .HasForeignKey(e => e.PersonajeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ============================
        // TABLA USUARIOS
        // ============================

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Id)
                .ValueGeneratedOnAdd();

            // IsUnique evita registrar dos usuarios
            // con el mismo correo electronico.
            entity.HasIndex(u => u.Correo)
                .IsUnique();

            // El correo puede tener hasta 200 caracteres.
            entity.Property(u => u.Correo)
                .HasMaxLength(200);
        });
    }
}
