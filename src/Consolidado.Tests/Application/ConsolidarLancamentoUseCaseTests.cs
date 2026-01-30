using System;
using System.Threading;
using System.Threading.Tasks;
using Consolidado.Application.UseCases;
using Consolidado.Application.Interfaces;
using Consolidado.Domain.Entities;
using NSubstitute;
using FluentAssertions;
using Shared.Events;
using Xunit;

namespace Consolidado.Tests.Application;

public class ConsolidarLancamentoUseCaseTests
{
    [Fact]
    public async Task Deve_adicionar_lancamento_quando_evento_nao_processado()
    {
        // Arrange
        var lancamentosRepo = Substitute.For<Consolidado.Domain.Interfaces.ILancamentoRepository>();
        var idempotency = Substitute.For<IIdempotencyStore>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var action = callInfo.ArgAt<Func<CancellationToken, Task>>(0);
                var ct = callInfo.ArgAt<CancellationToken>(1);
                return action(ct);
            });

        idempotency.TryMarkProcessedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var useCase = new ConsolidarLancamentoUseCase(lancamentosRepo, idempotency, unitOfWork);

        var evento = new LancamentoEfetuadoEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            123.45m,
            TipoLancamento.Credito,
            DateTime.Today);

        // Act
        await useCase.ExecuteAsync(evento, CancellationToken.None);

        // Assert
        await idempotency.Received(1).TryMarkProcessedAsync(evento.EventId, nameof(LancamentoEfetuadoEvent), Arg.Any<CancellationToken>());

        await lancamentosRepo.Received(1).AddAsync(
            Arg.Is<Consolidado.Domain.Entities.Lancamento>(l =>
                l.LancamentoId == evento.LancamentoId &&
                l.EventId == evento.EventId &&
                l.ComercianteId == evento.ComercianteId &&
                l.Valor == evento.Valor &&
                l.Tipo == evento.Tipo &&
                l.Dia == evento.Data.Date),
            Arg.Any<CancellationToken>());

        await unitOfWork.Received(1).ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Nao_deve_adicionar_quando_evento_ja_processado()
    {
        // Arrange
        var lancamentosRepo = Substitute.For<Consolidado.Domain.Interfaces.ILancamentoRepository>();
        var idempotency = Substitute.For<IIdempotencyStore>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var action = callInfo.ArgAt<Func<CancellationToken, Task>>(0);
                var ct = callInfo.ArgAt<CancellationToken>(1);
                return action(ct);
            });

        idempotency.TryMarkProcessedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var useCase = new ConsolidarLancamentoUseCase(lancamentosRepo, idempotency, unitOfWork);

        var evento = new LancamentoEfetuadoEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            50m,
            TipoLancamento.Debito,
            DateTime.Today);

        // Act
        await useCase.ExecuteAsync(evento, CancellationToken.None);

        // Assert
        await idempotency.Received(1).TryMarkProcessedAsync(evento.EventId, nameof(LancamentoEfetuadoEvent), Arg.Any<CancellationToken>());

        await lancamentosRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await unitOfWork.Received(1).ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>());
    }
}