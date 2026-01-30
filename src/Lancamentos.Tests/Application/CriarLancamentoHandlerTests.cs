using FluentAssertions;
using Lancamentos.Application.Commands;
using Lancamentos.Application.Handlers;
using Lancamentos.Application.Interfaces;
using Lancamentos.Domain.Entities;
using Lancamentos.Domain.Enums;
using Lancamentos.Domain.Interfaces;
using NSubstitute;
using Shared.Events;

namespace Lancamentos.Tests.Application;

public class CriarLancamentoHandlerTests
{
    [Fact]
    public async Task Deve_persistir_lancamento_e_publicar_evento()
    {
        // Arrange
        var repository = Substitute.For<ILancamentoRepository>();
        var eventBus = Substitute.For<IEventBus>();

        var handler = new EfetuarLancamentoHandler(repository, eventBus);

        var command = new EfetuarLancamentoCommand(
            ComercianteId: Guid.NewGuid(),
            Valor: 150,
            Tipo: Lancamentos.Domain.Enums.TipoLancamento.Credito,
            Data: DateTime.Today);

        // Act
        var id = await handler.Handle(command, default);

        // Assert
        id.Should().NotBeEmpty();

        await repository.Received(1)
            .AddAsync(Arg.Any<Lancamento>(), Arg.Any<CancellationToken>());

        await eventBus.Received(1)
            .PublishAsync(Arg.Any<LancamentoEfetuadoEvent>(), Arg.Any<CancellationToken>());
    }
}
