using Lancamentos.Domain.Entities;
using Lancamentos.Domain.Enums;
using Lancamentos.Domain.Exceptions;
using FluentAssertions;

namespace Lancamentos.Tests.Domain;

public class LancamentoTests
{
    [Fact]
    public void Deve_criar_lancamento_valido()
    {
        var dataHoje = DateTime.Today;
        var lancamento = Lancamento.Criar(
            comercianteId: Guid.NewGuid(),
            valor: 100,
            tipo: TipoLancamento.Credito,
            data: dataHoje);

        lancamento.Id.Should().NotBeEmpty();
        lancamento.Valor.Should().Be(100);
        lancamento.Tipo.Should().Be(TipoLancamento.Credito);
        lancamento.Data.Should().Be(dataHoje);
    }

    [Fact]
    public void Nao_deve_criar_lancamento_com_valor_zero_ou_negativo()
    {
        Action act = () =>
            Lancamento.Criar(Guid.NewGuid(),0, TipoLancamento.Debito, DateTime.Today);

        act.Should().Throw<DomainException>()
           .WithMessage("*maior que zero*");
    }

    [Fact]
    public void Nao_deve_criar_lancamento_com_data_invalida()
    {
        Action act = () =>
            Lancamento.Criar(Guid.NewGuid(),100, TipoLancamento.Credito, default);

        act.Should().Throw<DomainException>()
           .WithMessage("*data*");
    }
}
