using Consolidado.Application.Interfaces;
using Consolidado.Domain.Interfaces;
using Consolidado.Infrastructure.Persistence;
using Consolidado.Infrastructure.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Consolidado.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var cs = config.GetConnectionString("ConsolidadoDb");
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("ConnectionStrings:ConsolidadoDb não configurada.");


        services.AddDbContextPool<ConsolidadoDbContext>(opt =>
        {
            opt.UseNpgsql(cs);
        });

        services.AddScoped<ILancamentoRepository, LancamentoRepository>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        services.AddScoped<IConsolidadoReadService, ConsolidadoReadService>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();


        return services;
    }

    public static IServiceCollection AddInfrastructureQueries(this IServiceCollection services, IConfiguration config)
    {
        // se quiser, reutilize a mesma cs do ConsolidadoDb
        var cs = config.GetConnectionString("ConsolidadoDb");
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("ConnectionStrings:ConsolidadoDb não configurada.");

        services.AddScoped<IConsolidadoReadService, ConsolidadoReadService>();
        return services;
    }
}
