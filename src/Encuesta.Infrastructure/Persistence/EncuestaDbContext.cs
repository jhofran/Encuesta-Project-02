using Encuesta.Application.Abstractions;
using Encuesta.Domain.Entities;
using Encuesta.Domain.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EncuestaAggregate = Encuesta.Domain.Entities.Encuesta;

namespace Encuesta.Infrastructure.Persistence;

public sealed class EncuestaDbContext(DbContextOptions<EncuestaDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public const string IndiceRespuestaUnica = "IX_Respuesta_EncuestaId_Huella";

    public DbSet<EncuestaAggregate> Encuestas => Set<EncuestaAggregate>();
    public DbSet<RespuestaEncuesta> Respuestas => Set<RespuestaEncuesta>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is SqlException { Number: 2601 or 2627 } sql && sql.Message.Contains(IndiceRespuestaUnica))
        {
            // Carrera entre dos envíos del mismo participante: el índice único es la garantía final (RF-05).
            throw new DomainConflictException("Ya has respondido esta encuesta");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EncuestaDbContext).Assembly);
}

internal sealed class EncuestaConfiguration : IEntityTypeConfiguration<EncuestaAggregate>
{
    public void Configure(EntityTypeBuilder<EncuestaAggregate> builder)
    {
        builder.ToTable("Encuesta");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Titulo).HasMaxLength(EncuestaAggregate.MaxTitulo).IsRequired();
        builder.Property(e => e.Descripcion).HasMaxLength(EncuestaAggregate.MaxDescripcion);
        builder.Property(e => e.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.Token).HasMaxLength(64);
        builder.HasIndex(e => e.Token).IsUnique().HasFilter("[Token] IS NOT NULL");
        builder.HasIndex(e => e.CreadorId);

        builder.Ignore(e => e.DomainEvents);

        builder.HasMany(e => e.Preguntas).WithOne().HasForeignKey("EncuestaId").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(e => e.Preguntas).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class PreguntaConfiguration : IEntityTypeConfiguration<Pregunta>
{
    public void Configure(EntityTypeBuilder<Pregunta> builder)
    {
        builder.ToTable("Pregunta");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Texto).HasMaxLength(EncuestaAggregate.MaxTextoPregunta).IsRequired();
        builder.Property(p => p.Tipo).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasMany(p => p.Opciones).WithOne().HasForeignKey("PreguntaId").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Opciones).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class OpcionPreguntaConfiguration : IEntityTypeConfiguration<OpcionPregunta>
{
    public void Configure(EntityTypeBuilder<OpcionPregunta> builder)
    {
        builder.ToTable("OpcionPregunta");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Texto).HasMaxLength(EncuestaAggregate.MaxTextoOpcion).IsRequired();
    }
}

internal sealed class RespuestaEncuestaConfiguration : IEntityTypeConfiguration<RespuestaEncuesta>
{
    public void Configure(EntityTypeBuilder<RespuestaEncuesta> builder)
    {
        builder.ToTable("Respuesta");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Huella).HasMaxLength(64);
        builder.HasIndex(r => new { r.EncuestaId, r.Huella })
            .IsUnique()
            .HasFilter("[Huella] IS NOT NULL")
            .HasDatabaseName(EncuestaDbContext.IndiceRespuestaUnica);
        builder.Ignore(r => r.DomainEvents);

        // Referencia por Id (agregado independiente): sin propiedad de navegación hacia Encuesta.
        builder.HasOne<EncuestaAggregate>().WithMany().HasForeignKey(r => r.EncuestaId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Items).WithOne().HasForeignKey("RespuestaId").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ItemRespuestaConfiguration : IEntityTypeConfiguration<ItemRespuesta>
{
    public void Configure(EntityTypeBuilder<ItemRespuesta> builder)
    {
        builder.ToTable("ItemRespuesta");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Valor).HasMaxLength(RespuestaEncuesta.MaxTextoLibre).IsRequired();
    }
}
