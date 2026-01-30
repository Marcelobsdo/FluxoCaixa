using Lancamentos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lancamentos.Infrastructure.Persistence.Configurations;

public sealed class LancamentoConfiguration : IEntityTypeConfiguration<Lancamento>
{
    public void Configure(EntityTypeBuilder<Lancamento> builder)
    {
        builder.ToTable("lancamentos");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.ComercianteId)
            .HasColumnName("comerciante_id")
            .IsRequired();

        builder.Property(x => x.Valor)
            .HasColumnName("valor")
            .IsRequired();

        builder.Property(x => x.Tipo)
            .HasColumnName("tipo")
            .IsRequired();

        builder.Property(x => x.Data)
            .HasColumnName("dia")
            .IsRequired();
    }
}
