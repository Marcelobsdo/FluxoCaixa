using Consolidado.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Consolidado.Infrastructure.Persistence.Configurations;

public sealed class LancamentoConfiguration : IEntityTypeConfiguration<Lancamento>
{
    public void Configure(EntityTypeBuilder<Lancamento> builder)
    {
        builder.ToTable("lancamentos");

        builder.HasKey(x => x.EventId);

        builder.Property(x => x.EventId).HasColumnName("event_id");
        builder.Property(x => x.LancamentoId).HasColumnName("lancamento_id");
        builder.Property(x => x.ComercianteId).HasColumnName("comerciante_id");

        builder.Property(x => x.Dia)
            .HasColumnName("dia")
            .HasColumnType("date");

        builder.Property(x => x.Tipo)
            .HasColumnName("tipo")
            .HasConversion<int>();

        builder.Property(x => x.Valor)
            .HasColumnName("valor")
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.ProcessadoEmUtc)
            .HasColumnName("processado_em_utc")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(x => new { x.ComercianteId, x.Dia });
    }
}
