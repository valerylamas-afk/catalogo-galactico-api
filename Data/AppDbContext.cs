
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PersonajesApi.Models;
using System.Text.Json;

namespace PersonajesApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Personaje> Personajes { get; set; }
    public DbSet<CardPersonaje> Cartas { get; set; }
    public DbSet<Evento> Eventos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tabla de personajes
        modelBuilder.Entity<Personaje>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Id)
                .ValueGeneratedOnAdd();
        });

        // Tabla de cartas
        modelBuilder.Entity<CardPersonaje>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Id)
                .ValueGeneratedOnAdd();

            entity.HasOne<Personaje>()
                .WithOne()
                .HasForeignKey<CardPersonaje>(c => c.PersonajeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Tabla de eventos
        modelBuilder.Entity<Evento>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            // Guardar listas de IDs como JSON
            entity.Property(e => e.Participantes)
                .HasConversion(
                    v => JsonSerializer.Serialize(
                        v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<int>>(
                        v, (JsonSerializerOptions?)null)
                        ?? new List<int>())
                .HasColumnType("longtext");

            entity.Property(e => e.Fallecidos)
                .HasConversion(
                    v => JsonSerializer.Serialize(
                        v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<int>>(
                        v, (JsonSerializerOptions?)null)
                        ?? new List<int>())
                .HasColumnType("longtext");

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

            entity.HasOne<Personaje>()
                .WithMany()
                .HasForeignKey(e => e.GanadorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
