using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Consolidado.Infrastructure.Persistence;

public sealed class ConsolidadoDbContextFactory : IDesignTimeDbContextFactory<ConsolidadoDbContext>
{
    public ConsolidadoDbContext CreateDbContext(string[] args)
    {
        // 1) Preferência: variável de ambiente (ideal em CI/Docker)
        var cs = Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__CONSOLIDADODB")
                 ?? Environment.GetEnvironmentVariable("ConnectionStrings__ConsolidadoDb");

        // 2) Fallback: string padrão do seu docker-compose (ajuste usuário/senha/db se necessário)
        cs ??= "Host=localhost;Port=5433;Database=consolidado;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<ConsolidadoDbContext>()
            .UseNpgsql(cs)
            .Options;

        return new ConsolidadoDbContext(options);
    }
}
