using LojaApi.Domain.Acessorios;
using LojaApi.Shared.Application;
using LojaApi.src.Models;
using static LojaApi.Application.Acessorios.AcessoriosApoio;

namespace LojaApi.Application.Acessorios;

// Cria o pedido e cobra no cartão (Payment Brick). Recusado ou com erro: devolve o estoque e apaga o pedido.
public class CriarPedidoCartaoCommandHandler(IAcessoriosRepository repositorio, IPagamentoOnlineAcessorio pagamento) : ICommandHandler<CriarPedidoCartaoCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarPedidoCartaoCommand command)
    {
        var req = command.Req;
        await LiberarPedidosExpiradosAsync(repositorio);

        if (req.Itens.Count == 0)
            return Resultado.Invalido("O pedido precisa ter ao menos um item.");

        if (string.IsNullOrWhiteSpace(req.ClienteCpfCnpj))
            return Resultado.Invalido("CPF é obrigatório.");

        var (pedido, erro) = await MontarPedidoAsync(repositorio, new DadosDoPedido(
            req.ClienteNome, req.ClienteEmail, req.ClienteTelefone, req.ClienteCpfCnpj, req.Cep, req.Endereco, req.Numero,
            req.Complemento, req.Bairro, req.Cidade, req.Uf, req.Itens));
        if (pedido is null) return Resultado.Invalido(erro!);

        repositorio.Adicionar(pedido);
        await repositorio.SalvarAsync();

        var resultado = await pagamento.CriarCartao(
            valor: pedido.Total,
            descricao: DescricaoDoPedido(pedido),
            cardToken: req.Token,
            parcelas: req.Installments,
            emailPagador: pedido.ClienteEmail,
            cpfPagador: pedido.ClienteCpfCnpj!,
            nomePagador: pedido.ClienteNome,
            pagamentoId: pedido.Id
        );

        if (!resultado.Sucesso || resultado.Status == "rejected")
        {
            // Desfaz a reserva de estoque se o pagamento falhar/for recusado
            await DevolverEstoqueAsync(repositorio, pedido);
            repositorio.Remover(pedido);
            await repositorio.SalvarAsync();
            return Resultado.Invalido(resultado.Erro ?? "Pagamento recusado pela operadora do cartão. Confira os dados e tente novamente.");
        }

        pedido.MpPaymentId = resultado.MpPaymentId;
        pedido.MpStatus = resultado.Status;

        // Cartão costuma aprovar na hora — já marca como pago se vier "approved"
        if (resultado.Status == "approved")
        {
            pedido.Status = StatusPedidoAcessorio.Pago;
            pedido.PagoEm = DateTime.UtcNow;
            pedido.ExpiraEm = null;
        }

        await repositorio.SalvarAsync();

        return Resultado.Ok(new { pedido.Id, pedido.Status, pedido.Total });
    }
}
