using Encuesta.Application.Abstractions;
using Encuesta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EncuestaAggregate = Encuesta.Domain.Entities.Encuesta;

namespace Encuesta.Infrastructure.Persistence;

public sealed class EncuestaDbContext(DbContextOptions<EncuestaDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<EncuestaAggregate> Encuestas => Set<EncuestaAggregate>();

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
