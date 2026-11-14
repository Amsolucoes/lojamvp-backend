using LojaApi.Domain.Acessorios;
using LojaApi.Shared.Application;
using LojaApi.src.Models;
using static LojaApi.Application.Acessorios.AcessoriosApoio;

namespace LojaApi.Application.Acessorios;

// Cria o pedido (reservando o estoque) e gera a cobrança Pix. Se o Mercado Pago falhar, desfaz tudo.
public class CriarPedidoPixCommandHandler(IAcessoriosRepository repositorio, IPagamentoOnlineAcessorio pagamento) : ICommandHandler<CriarPedidoPixCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarPedidoPixCommand command)
    {
        var req = command.Req;
        await LiberarPedidosExpiradosAsync(repositorio);

        if (req.Itens.Count == 0)
            return Resultado.Invalido("O pedido precisa ter ao menos um item.");

        if (string.IsNullOrWhiteSpace(req.ClienteCpfCnpj))
            return Resultado.Invalido("CPF é obrigatório para gerar o Pix.");

        var (pedido, erro) = await MontarPedidoAsync(repositorio, new DadosDoPedido(
            req.ClienteNome, req.ClienteEmail, req.ClienteTelefone, req.ClienteCpfCnpj, req.Cep, req.Endereco, req.Numero,
            req.Complemento, req.Bairro, req.Cidade, req.Uf, req.Itens));
        if (pedido is null) return Resultado.Invalido(erro!);

        repositorio.Adicionar(pedido);
        await repositorio.SalvarAsync();

        var resultadoPix = await pagamento.CriarPix(
            valor: pedido.Total,
            descricao: DescricaoDoPedido(pedido),
            emailPagador: pedido.ClienteEmail,
            cpfPagador: pedido.ClienteCpfCnpj!,
            nomePagador: pedido.ClienteNome,
            pagamentoId: pedido.Id
        );

        if (!resultadoPix.Sucesso)
        {
            // Desfaz a reserva de estoque se o Pix falhar ao gerar
            await DevolverEstoqueAsync(repositorio, pedido);
            repositorio.Remover(pedido);
            await repositorio.SalvarAsync();
            return Resultado.Invalido("Não foi possível gerar o pagamento. Tente novamente.");
        }

        pedido.MpPaymentId = resultadoPix.MpPaymentId;
        pedido.MpStatus = resultadoPix.Status;
        await repositorio.SalvarAsync();

        return Resultado.Ok(new
        {
            pedido.Id,
            pedido.Total,
            qrCode = resultadoPix.QrCode,
            qrCodeBase64 = resultadoPix.QrCodeBase64,
        });
    }
}
