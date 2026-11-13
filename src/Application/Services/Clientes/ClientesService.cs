using LojaApi.Application.Carteira;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Clientes;

// Cadastro de clientes da loja.
public class ClientesService(IClientesRepository repositorio, CarteiraService carteira)
{
    // Quem não está vinculado a nenhuma loja (lojaId nulo) enxerga o cadastro inteiro, como sempre foi.
    private static bool DeOutraLoja(Cliente c, Guid? lojaId) => lojaId.HasValue && c.LojaId != lojaId;

    public async Task<Resultado<List<ClienteDto>>> ListarAsync(Guid? lojaId, string? busca) =>
        Resultado<List<ClienteDto>>.Ok(await repositorio.ListarAsync(lojaId, busca));

    public async Task<Resultado<ClienteDto>> BuscarAsync(Guid? lojaId, Guid id)
    {
        var c = await repositorio.ObterAsync(id);
        if (c is null || DeOutraLoja(c, lojaId)) return Resultado<ClienteDto>.NaoEncontrado();

        var (qtd, total) = await repositorio.ResumoDeComprasAsync(c.Id);
        return Resultado<ClienteDto>.Ok(Montar(c, qtd, total, await carteira.SaldoPorClienteAsync(c.Id)));
    }

    public async Task<Resultado<ClienteDto>> CriarAsync(Guid? lojaId, SalvarClienteRequest req)
    {
        if (!string.IsNullOrWhiteSpace(req.Cpf) && await repositorio.CpfJaCadastradoAsync(req.Cpf, lojaId))
            return Resultado<ClienteDto>.Conflito("CPF já cadastrado.");

        var cliente = new Cliente
        {
            Nome = req.Nome,
            Telefone = req.Telefone,
            Cpf = req.Cpf,
            Email = req.Email,
            Endereco = req.Endereco,
            Observacoes = req.Observacoes,
            DataNascimento = req.DataNascimento,
            LojaId = lojaId,
        };

        repositorio.Adicionar(cliente);
        await repositorio.SalvarAsync();

        return Resultado<ClienteDto>.Ok(Montar(cliente, 0, 0, 0));
    }

    public async Task<Resultado<ClienteDto>> AtualizarAsync(Guid? lojaId, Guid id, SalvarClienteRequest req)
    {
        var cliente = await repositorio.ObterAsync(id);
        if (cliente is null || DeOutraLoja(cliente, lojaId)) return Resultado<ClienteDto>.NaoEncontrado();

        if (!string.IsNullOrWhiteSpace(req.Cpf) && req.Cpf != cliente.Cpf &&
            await repositorio.CpfJaCadastradoAsync(req.Cpf, lojaId, ignorarClienteId: id))
            return Resultado<ClienteDto>.Conflito("CPF já cadastrado.");

        cliente.Nome = req.Nome; cliente.Telefone = req.Telefone;
        cliente.Cpf = req.Cpf; cliente.Email = req.Email;
        cliente.Endereco = req.Endereco; cliente.Observacoes = req.Observacoes;
        cliente.DataNascimento = req.DataNascimento;

        await repositorio.SalvarAsync();

        var (qtd, total) = await repositorio.ResumoDeComprasAsync(id);
        return Resultado<ClienteDto>.Ok(Montar(cliente, qtd, total, await carteira.SaldoPorClienteAsync(cliente.Id)));
    }

    public async Task<Resultado<object>> ExcluirAsync(Guid? lojaId, Guid id)
    {
        var cliente = await repositorio.ObterAsync(id);
        if (cliente is null || DeOutraLoja(cliente, lojaId)) return Resultado.NaoEncontrado();

        if (await carteira.SaldoPorClienteAsync(cliente.Id) > 0)
            return Resultado.Invalido("Este cliente tem saldo a pagar na carteira. Receba o pagamento antes de excluir o cadastro.");

        repositorio.Remover(cliente);
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { });
    }

    private static ClienteDto Montar(Cliente c, int totalCompras, decimal totalGasto, decimal saldoCarteira) => new(
        c.Id, c.Nome, c.Telefone, c.Cpf, c.Email, c.Endereco, c.Observacoes, c.CriadoEm,
        totalCompras, totalGasto, c.DataNascimento, c.CreditoLoja, saldoCarteira, c.LimiteCarteira);
}
