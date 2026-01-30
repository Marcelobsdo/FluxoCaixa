using Consolidado.Application.UseCases;
using Consolidado.Infrastructure;
using Consolidado.Infrastructure.Messaging;
using Consolidado.Worker;
using RabbitMQ.Client;

var builder = Host.CreateApplicationBuilder(args);

var rabbitSection = builder.Configuration.GetSection("RabbitMQ");

builder.Services.AddSingleton<IConnection>(_ =>
{
    var host = rabbitSection["Host"];
    if (string.IsNullOrWhiteSpace(host))
        throw new InvalidOperationException("Configuração obrigatória ausente: RabbitMQ:Host");

    var portString = rabbitSection["Port"];
    if (string.IsNullOrWhiteSpace(portString))
        throw new InvalidOperationException("Configuração obrigatória ausente: RabbitMQ:Port");

    if (!int.TryParse(portString, out var port))
        throw new InvalidOperationException("Configuração inválida: RabbitMQ:Port deve ser um número inteiro");

    var user = rabbitSection["User"];
    if (string.IsNullOrWhiteSpace(user))
        throw new InvalidOperationException("Configuração obrigatória ausente: RabbitMQ:User");

    var password = rabbitSection["Password"];
    if (string.IsNullOrWhiteSpace(password))
        throw new InvalidOperationException("Configuração obrigatória ausente: RabbitMQ:Password");

    var factory = new ConnectionFactory
    {
        HostName = host,
        Port = port,
        UserName = user,
        Password = password
    };

    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

builder.Services.AddSingleton<RabbitMqConsumer>();
builder.Services.AddScoped<ConsolidarLancamentoUseCase>();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();
