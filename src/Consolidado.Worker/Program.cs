using Consolidado.Application.UseCases;
using Consolidado.Infrastructure;
using Consolidado.Infrastructure.Messaging;
using Consolidado.Infrastructure.Persistence;
using Consolidado.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .WriteTo.Console()
    .CreateLogger();

builder.Services.AddSerilog();

builder.Services.Configure<HostOptions>(o =>
{
    o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

var rabbitSection = builder.Configuration.GetSection("RabbitMQ");

builder.Services.AddSingleton(_ =>
{
    var host = rabbitSection["Host"] ?? throw new InvalidOperationException("RabbitMQ:Host ausente");
    var portString = rabbitSection["Port"] ?? throw new InvalidOperationException("RabbitMQ:Port ausente");
    if (!int.TryParse(portString, out var port))
        throw new InvalidOperationException("RabbitMQ:Port inválida");

    var user = rabbitSection["User"] ?? throw new InvalidOperationException("RabbitMQ:User ausente");
    var password = rabbitSection["Password"] ?? throw new InvalidOperationException("RabbitMQ:Password ausente");

    return new ConnectionFactory
    {
        HostName = host,
        Port = port,
        UserName = user,
        Password = password,

        AutomaticRecoveryEnabled = true,
        NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
        TopologyRecoveryEnabled = true
    };
});

builder.Services.AddSingleton<RabbitMqConsumer>();
builder.Services.AddScoped<ConsolidarLancamentoUseCase>();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHostedService<Worker>();

try
{
    var host = builder.Build();

    var env = host.Services.GetRequiredService<IHostEnvironment>();
    if (env.IsDevelopment())
    {
        using var scope = host.Services.CreateScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var db = scope.ServiceProvider.GetRequiredService<ConsolidadoDbContext>();

        const int maxAttempts = 10;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await db.Database.MigrateAsync();
                logger.LogInformation("Migrations do Consolidado aplicadas com sucesso.");
                break;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex,
                    "ConsolidadoDb não está pronto ainda. Tentativa {Attempt}/{MaxAttempts}",
                    attempt, maxAttempts);

                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }
    }

    host.Run();
}
finally
{
    Log.CloseAndFlush();
}
