using Lancamentos.Application.Interfaces;
using Lancamentos.Domain.Interfaces;
using Lancamentos.Infrastructure.Messaging;
using Lancamentos.Infrastructure.Persistence;
using Lancamentos.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lancamentos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        var dbCs = config.GetConnectionString("LancamentosDb");
        if (string.IsNullOrWhiteSpace(dbCs))
            throw new InvalidOperationException("ConnectionStrings:LancamentosDb não configurada.");

        services.AddDbContext<LancamentosDbContext>(options =>
        {
            options.UseNpgsql(dbCs);
        });


        services.AddScoped<ILancamentoRepository, LancamentoRepository>();

        var rabbitCs = config.GetConnectionString("RabbitMq");
        if (string.IsNullOrWhiteSpace(rabbitCs))
            throw new InvalidOperationException("ConnectionStrings:RabbitMq não configurada.");

        services.AddSingleton<IEventBus>(_ => new RabbitMqEventBus(rabbitCs));

        return services;
    }
}
