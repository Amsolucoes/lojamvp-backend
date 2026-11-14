using LojaApi.Domain.Produtos;
using LojaApi.src.Models.Etiquetas;

namespace LojaApi.Application.Produtos;

// O que criar e atualizar modelo de etiqueta compartilham.
public static class ModeloEtiquetaApoio
{
    public static void Preencher(ConfiguracaoEtiqueta modelo, SalvarModeloRequest req)
    {
        modelo.Nome = req.Nome.Trim();
        modelo.IncluirLogo = req.IncluirLogo;
        modelo.UsarLogoPropria = req.UsarLogoPropria;
        modelo.LogoEtiquetaUrl = ProdutosRegras.LogoDaEtiqueta(req.LogoEtiquetaUrl);
        modelo.IncluirNomeMarca = req.IncluirNomeMarca;
        modelo.IncluirNomeProduto = req.IncluirNomeProduto;
        modelo.IncluirPreco = req.IncluirPreco;
        modelo.IncluirCodigoBarras = req.IncluirCodigoBarras;
        modelo.LarguraMm = req.LarguraMm;
        modelo.AlturaMm = req.AlturaMm;
        modelo.CorTexto = ProdutosRegras.CorDoTexto(req.CorTexto);
        modelo.CorFundo = ProdutosRegras.CorDeFundo(req.CorFundo);
        modelo.FonteFamilia = ProdutosRegras.FonteDaEtiqueta(req.FonteFamilia);
        modelo.EscalaFonte = ProdutosRegras.EscalaDaFonte(req.EscalaFonte);
        modelo.AtualizadoEm = DateTime.UtcNow;
    }
}
