using LojaApi.Models;
using LojaApi.src.Models;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Venda> Vendas => Set<Venda>();
    public DbSet<ItemVenda> ItensVenda => Set<ItemVenda>();
    public DbSet<MovimentoEstoque> Movimentos => Set<MovimentoEstoque>();
    public DbSet<Loja> Lojas => Set<Loja>();
    public DbSet<UsuarioLoja> UsuariosLoja => Set<UsuarioLoja>();
    public DbSet<Pagamento> Pagamentos => Set<Pagamento>();
    public DbSet<PerfilLoja> PerfisLoja => Set<PerfilLoja>();
    public DbSet<CategoriaPerfilLoja> CategoriasPerfilLoja => Set<CategoriaPerfilLoja>();
    public DbSet<CampoExtraPerfil> CamposExtrasPerfil => Set<CampoExtraPerfil>();
    public DbSet<CategoriaLoja> CategoriasLoja => Set<CategoriaLoja>();
    public DbSet<CampoExtraLoja> CamposExtrasLoja => Set<CampoExtraLoja>();
    public DbSet<ProdutoVariacao> ProdutoVariacoes => Set<ProdutoVariacao>();
    public DbSet<Troca> Trocas => Set<Troca>();
    public DbSet<ItemTroca> ItensTroca => Set<ItemTroca>();
    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<Agendamento> Agendamentos => Set<Agendamento>();
    public DbSet<ServicoPerfilLoja> ServicosPerfilLoja => Set<ServicoPerfilLoja>();
    public DbSet<Plano> Planos => Set<Plano>();
    public DbSet<AssinaturaCliente> AssinaturasCliente => Set<AssinaturaCliente>();
    public DbSet<PagamentoPlano> PagamentosPlano => Set<PagamentoPlano>();
    public DbSet<ConsumoPlano> ConsumosPlano => Set<ConsumoPlano>();
    public DbSet<ContaBancaria> ContasBancarias => Set<ContaBancaria>();
    public DbSet<LancamentoFixo> LancamentosFixos => Set<LancamentoFixo>();
    public DbSet<LancamentoFinanceiro> LancamentosFinanceiros => Set<LancamentoFinanceiro>();
    public DbSet<AjusteContaBancaria> AjustesContaBancaria => Set<AjusteContaBancaria>();
    public DbSet<CategoriaFinanceira> CategoriasFinanceiras => Set<CategoriaFinanceira>();
    public DbSet<CartaoCredito> CartoesCredito => Set<CartaoCredito>();
    public DbSet<LancamentoCartao> LancamentosCartao => Set<LancamentoCartao>();
    public DbSet<FaturaCartao> FaturasCartao => Set<FaturaCartao>();
    public DbSet<CartaoLancamentoFixo> CartaoLancamentosFixos => Set<CartaoLancamentoFixo>();
    public DbSet<Turma> Turmas => Set<Turma>();
    public DbSet<MatriculaTurma> MatriculasTurma => Set<MatriculaTurma>();
    public DbSet<SessaoTurma> SessoesTurma => Set<SessaoTurma>();
    public DbSet<InscricaoSessao> InscricoesSessao => Set<InscricaoSessao>();
    public DbSet<ModuloPreco> ModulosPreco => Set<ModuloPreco>();
    public DbSet<Profissional> Profissionais => Set<Profissional>();
    public DbSet<Seguradora> Seguradoras => Set<Seguradora>();
    public DbSet<Oportunidade> Oportunidades => Set<Oportunidade>();
    public DbSet<Apolice> Apolices => Set<Apolice>();
    public DbSet<NfProdutoMapeamento> NfProdutoMapeamentos => Set<NfProdutoMapeamento>();
    public DbSet<NfImportada> NfsImportadas => Set<NfImportada>();
    public DbSet<VideoAjuda> VideosAjuda => Set<VideoAjuda>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<ConfiguracaoPrecoChacara> ConfiguracoesPrecoChacara => Set<ConfiguracaoPrecoChacara>();
    public DbSet<FotoChacara> FotosChacara => Set<FotoChacara>();
    public DbSet<InfoChacara> InfosChacara => Set<InfoChacara>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // Índices únicos
        mb.Entity<Usuario>().HasIndex(u => u.Email).IsUnique();
        mb.Entity<Produto>()
            .HasIndex(p => new { p.LojaId, p.CodigoBarras })
            .IsUnique()
            .HasDatabaseName("ix_produtos_codigo_barras")
            .HasFilter("codigo_barras IS NOT NULL AND codigo_barras <> ''");

        // Relacionamentos
        mb.Entity<ItemVenda>()
            .HasOne(i => i.Venda)
            .WithMany(v => v.Itens)
            .HasForeignKey(i => i.VendaId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<ItemVenda>()
            .HasOne(i => i.Produto).WithMany(p => p.ItensVenda)
            .HasForeignKey(i => i.ProdutoId).OnDelete(DeleteBehavior.SetNull);

        mb.Entity<ItemVenda>()
            .HasOne(i => i.Servico).WithMany()
            .HasForeignKey(i => i.ServicoId).OnDelete(DeleteBehavior.SetNull);

        mb.Entity<MovimentoEstoque>()
            .HasOne(m => m.Produto)
            .WithMany(p => p.Movimentos)
            .HasForeignKey(m => m.ProdutoId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<Venda>()
            .HasOne(v => v.Cliente)
            .WithMany(c => c.Vendas)
            .HasForeignKey(v => v.ClienteId)
            .OnDelete(DeleteBehavior.SetNull);

        mb.Entity<Loja>().HasIndex(l => l.Email).IsUnique();
        mb.Entity<Loja>().HasIndex(l => l.SchemaNome).IsUnique();

        mb.Entity<UsuarioLoja>()
            .HasOne(ul => ul.Loja).WithMany(l => l.Usuarios)
            .HasForeignKey(ul => ul.LojaId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<UsuarioLoja>()
            .HasOne(ul => ul.Usuario).WithMany()
            .HasForeignKey(ul => ul.UsuarioId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<Pagamento>()
            .HasOne(p => p.Loja).WithMany(l => l.Pagamentos)
            .HasForeignKey(p => p.LojaId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<CategoriaPerfilLoja>()
            .HasOne(c => c.PerfilLoja).WithMany(p => p.Categorias)
            .HasForeignKey(c => c.PerfilLojaId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<ServicoPerfilLoja>()
            .HasOne(s => s.PerfilLoja).WithMany(p => p.Servicos)
            .HasForeignKey(s => s.PerfilLojaId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<CampoExtraPerfil>()
            .HasOne(c => c.PerfilLoja).WithMany(p => p.CamposExtras)
            .HasForeignKey(c => c.PerfilLojaId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<CategoriaLoja>()
            .HasOne(c => c.Loja).WithMany()
            .HasForeignKey(c => c.LojaId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<CampoExtraLoja>()
            .HasOne(c => c.Loja).WithMany()
            .HasForeignKey(c => c.LojaId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<ProdutoVariacao>()
            .HasOne(v => v.Produto).WithMany(p => p.Variacoes)
            .HasForeignKey(v => v.ProdutoId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<Cliente>()
            .Property(c => c.DataNascimento)
            .HasColumnType("date");

        mb.Entity<Troca>()
            .HasOne(t => t.Cliente).WithMany()
            .HasForeignKey(t => t.ClienteId).OnDelete(DeleteBehavior.Restrict);

        mb.Entity<ItemTroca>()
            .HasOne(i => i.Troca).WithMany(t => t.Itens)
            .HasForeignKey(i => i.TrocaId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<Servico>()
            .HasOne(s => s.Loja).WithMany()
            .HasForeignKey(s => s.LojaId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<Agendamento>()
            .HasOne(a => a.Loja).WithMany()
            .HasForeignKey(a => a.LojaId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<Agendamento>()
            .HasOne(a => a.Servico).WithMany()
            .HasForeignKey(a => a.ServicoId).OnDelete(DeleteBehavior.Restrict);

        mb.Entity<Agendamento>()
            .HasOne(a => a.Cliente).WithMany()
            .HasForeignKey(a => a.ClienteId).OnDelete(DeleteBehavior.SetNull);

        mb.Entity<Agendamento>()
            .Property(a => a.DataHora)
            .HasColumnType("timestamp without time zone");

        mb.Entity<Agendamento>()
            .HasOne(a => a.Venda).WithMany()
            .HasForeignKey(a => a.VendaId).OnDelete(DeleteBehavior.SetNull);

        mb.Entity<Loja>()
            .HasIndex(l => l.Slug)
            .IsUnique();

        mb.Entity<ConsumoPlano>()
            .HasOne<AssinaturaCliente>()
            .WithMany()
            .HasForeignKey(c => c.AssinaturaId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<ContaBancaria>()
            .HasOne(c => c.Loja).WithMany()
            .HasForeignKey(c => c.LojaId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<CategoriaFinanceira>()
            .HasOne<Loja>().WithMany()
            .HasForeignKey(c => c.LojaId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<LancamentoFixo>()
            .HasOne(l => l.Categoria).WithMany()
            .HasForeignKey(l => l.CategoriaId).OnDelete(DeleteBehavior.SetNull);

        mb.Entity<LancamentoFinanceiro>()
            .HasOne(l => l.Categoria).WithMany()
            .HasForeignKey(l => l.CategoriaId).OnDelete(DeleteBehavior.SetNull);

        mb.Entity<AjusteContaBancaria>()
            .HasOne(a => a.ContaBancaria).WithMany()
            .HasForeignKey(a => a.ContaBancariaId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<CartaoCredito>()
            .HasOne(c => c.ContaBancaria).WithMany()
            .HasForeignKey(c => c.ContaBancariaId).OnDelete(DeleteBehavior.Restrict);

        mb.Entity<LancamentoCartao>()
            .HasOne(l => l.CartaoCredito).WithMany()
            .HasForeignKey(l => l.CartaoCreditoId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<LancamentoCartao>()
            .HasOne(l => l.Categoria).WithMany()
            .HasForeignKey(l => l.CategoriaId).OnDelete(DeleteBehavior.SetNull);

        mb.Entity<FaturaCartao>()
            .HasOne(f => f.CartaoCredito).WithMany()
            .HasForeignKey(f => f.CartaoCreditoId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<CartaoLancamentoFixo>()
            .HasOne(c => c.CartaoCredito).WithMany()
            .HasForeignKey(c => c.CartaoCreditoId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<CartaoLancamentoFixo>()
            .HasOne(c => c.Categoria).WithMany()
            .HasForeignKey(c => c.CategoriaId).OnDelete(DeleteBehavior.SetNull);

        mb.Entity<MatriculaTurma>()
            .HasOne(m => m.Turma).WithMany()
            .HasForeignKey(m => m.TurmaId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<SessaoTurma>()
            .HasOne(s => s.Turma).WithMany()
            .HasForeignKey(s => s.TurmaId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<InscricaoSessao>()
            .HasOne(i => i.SessaoTurma).WithMany()
            .HasForeignKey(i => i.SessaoTurmaId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<Oportunidade>()
            .HasOne(o => o.Seguradora).WithMany()
            .HasForeignKey(o => o.SeguradoraId).OnDelete(DeleteBehavior.SetNull);

        mb.Entity<Apolice>()
            .HasOne(a => a.Seguradora).WithMany()
            .HasForeignKey(a => a.SeguradoraId).OnDelete(DeleteBehavior.Restrict);

        mb.Entity<NfProdutoMapeamento>()
            .HasIndex(m => new { m.LojaId, m.CnpjFornecedor, m.CodigoFornecedor })
            .IsUnique();

        mb.Entity<NfProdutoMapeamento>()
            .HasOne(m => m.Produto).WithMany()
            .HasForeignKey(m => m.ProdutoId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<NfImportada>()
            .HasIndex(n => new { n.LojaId, n.ChaveAcesso })
            .IsUnique();

        mb.Entity<Reserva>()
            .HasOne(r => r.Loja).WithMany()
            .HasForeignKey(r => r.LojaId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<Reserva>()
            .HasIndex(r => new { r.LojaId, r.DataInicio, r.DataFim });

        mb.Entity<Reserva>()
            .Property(r => r.Valor)
            .HasColumnType("decimal(10,2)");

        mb.Entity<ConfiguracaoPrecoChacara>()
            .HasOne(c => c.Loja).WithMany()
            .HasForeignKey(c => c.LojaId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<ConfiguracaoPrecoChacara>()
            .HasIndex(c => c.LojaId)
            .IsUnique();

        mb.Entity<FotoChacara>()
            .HasOne(f => f.Loja).WithMany()
            .HasForeignKey(f => f.LojaId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<FotoChacara>()
            .HasIndex(f => new { f.LojaId, f.Ordem });

        mb.Entity<InfoChacara>()
            .HasOne(i => i.Loja).WithMany()
            .HasForeignKey(i => i.LojaId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<InfoChacara>()
            .HasIndex(i => i.LojaId)
            .IsUnique();

        // Snake_case para PostgreSQL
        foreach (var entity in mb.Model.GetEntityTypes())
        {
            entity.SetTableName(ToSnakeCase(entity.GetTableName()!));
            foreach (var prop in entity.GetProperties())
                prop.SetColumnName(ToSnakeCase(prop.GetColumnName()!));
            foreach (var key in entity.GetKeys())
                key.SetName(ToSnakeCase(key.GetName()!));
            foreach (var fk in entity.GetForeignKeys())
                fk.SetConstraintName(ToSnakeCase(fk.GetConstraintName()!));
            foreach (var idx in entity.GetIndexes())
                idx.SetDatabaseName(ToSnakeCase(idx.GetDatabaseName()!));
        }

        // Seed — usuário admin padrão
        mb.Entity<Usuario>().HasData(new Usuario
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Nome = "Administrador",
            Email = "admin@loja.com",
            SenhaHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Role = "admin",
            Ativo = true,
            CriadoEm = DateTime.UtcNow,
        });

        mb.Entity<Usuario>().HasData(new Usuario
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
            Nome = "Super Admin",
            Email = "superadmin@suaempresa.com",
            SenhaHash = "$2a$11$pJv1QqzqQHz4rG17gLCUoORPXG8/9fS3mtJpTuULzEYV/qc7heetu",
            Role = "superadmin",
            Ativo = true,
            CriadoEm = DateTime.UtcNow,
        });

        // ── Perfil: Loja personalizada (em branco) ────────────────────────
        var perfilBranco = Guid.Parse("10000000-0000-0000-0000-000000000009");
        mb.Entity<PerfilLoja>().HasData(new PerfilLoja
        {
            Id = perfilBranco,
            Nome = "Começar do zero",
            Descricao = "Loja em branco — você cria suas próprias categorias",
            Icone = "🏬",
            Ativo = true,
            TipoPlanoAplica = "loja",
            CriadoEm = DateTime.UtcNow,
        });

        // ── Perfil: Semi Joias e Maquiagem ────────────────────────────────
        var perfilJoias = Guid.Parse("10000000-0000-0000-0000-000000000001");
        mb.Entity<PerfilLoja>().HasData(new PerfilLoja
        {
            Id = perfilJoias,
            Nome = "Semi Joias e Maquiagem",
            Descricao = "Para lojas de semi joias, bijuterias e maquiagem",
            Icone = "💍",
            Ativo = true,
            CriadoEm = DateTime.UtcNow,
        });
        mb.Entity<CategoriaPerfilLoja>().HasData(
            new CategoriaPerfilLoja { Id = Guid.Parse("11000000-0000-0000-0000-000000000001"), PerfilLojaId = perfilJoias, Nome = "Semi Joias", Ordem = 0 },
            new CategoriaPerfilLoja { Id = Guid.Parse("11000000-0000-0000-0000-000000000002"), PerfilLojaId = perfilJoias, Nome = "Maquiagem", Ordem = 1 },
            new CategoriaPerfilLoja { Id = Guid.Parse("11000000-0000-0000-0000-000000000003"), PerfilLojaId = perfilJoias, Nome = "Acessórios", Ordem = 2 },
            new CategoriaPerfilLoja { Id = Guid.Parse("11000000-0000-0000-0000-000000000004"), PerfilLojaId = perfilJoias, Nome = "Outro", Ordem = 3 }
        );

        // ── Perfil: Vestuário ─────────────────────────────────────────────
        var perfilVest = Guid.Parse("10000000-0000-0000-0000-000000000002");
        mb.Entity<PerfilLoja>().HasData(new PerfilLoja
        {
            Id = perfilVest,
            Nome = "Vestuário",
            Descricao = "Para lojas de roupas e moda",
            Icone = "👕",
            Ativo = true,
            CriadoEm = DateTime.UtcNow,
        });
        mb.Entity<CategoriaPerfilLoja>().HasData(
            new CategoriaPerfilLoja { Id = Guid.Parse("12000000-0000-0000-0000-000000000001"), PerfilLojaId = perfilVest, Nome = "Camiseta", Ordem = 0, TipoTamanho = "letra" },
            new CategoriaPerfilLoja { Id = Guid.Parse("12000000-0000-0000-0000-000000000002"), PerfilLojaId = perfilVest, Nome = "Calça Jeans", Ordem = 1, TipoTamanho = "numero" },
            new CategoriaPerfilLoja { Id = Guid.Parse("12000000-0000-0000-0000-000000000003"), PerfilLojaId = perfilVest, Nome = "Vestido", Ordem = 2, TipoTamanho = "letra" },
            new CategoriaPerfilLoja { Id = Guid.Parse("12000000-0000-0000-0000-000000000004"), PerfilLojaId = perfilVest, Nome = "Bermuda", Ordem = 3, TipoTamanho = "numero" },
            new CategoriaPerfilLoja { Id = Guid.Parse("12000000-0000-0000-0000-000000000005"), PerfilLojaId = perfilVest, Nome = "Blusa", Ordem = 4, TipoTamanho = "letra" },
            new CategoriaPerfilLoja { Id = Guid.Parse("12000000-0000-0000-0000-000000000006"), PerfilLojaId = perfilVest, Nome = "Casaco", Ordem = 5, TipoTamanho = "letra" },
            new CategoriaPerfilLoja { Id = Guid.Parse("12000000-0000-0000-0000-000000000007"), PerfilLojaId = perfilVest, Nome = "Outro", Ordem = 6, TipoTamanho = "letra" }
        );
        mb.Entity<CampoExtraPerfil>().HasData(
            new CampoExtraPerfil
            {
                Id = Guid.Parse("13000000-0000-0000-0000-000000000001"),
                PerfilLojaId = perfilVest,
                Chave = "tamanho",
                Label = "Tamanho",
                Tipo = "lista",
                Opcoes = "PP,P,M,G,GG,XG",
                Obrigatorio = true,
                Ordem = 0,
            },
            new CampoExtraPerfil
            {
                Id = Guid.Parse("13000000-0000-0000-0000-000000000002"),
                PerfilLojaId = perfilVest,
                Chave = "cor",
                Label = "Cor",
                Tipo = "texto",
                Obrigatorio = false,
                Ordem = 1,
            }
        );

        // ── Perfil: Calçados ──────────────────────────────────────────────
        var perfilCalc = Guid.Parse("10000000-0000-0000-0000-000000000003");
        mb.Entity<PerfilLoja>().HasData(new PerfilLoja
        {
            Id = perfilCalc,
            Nome = "Calçados",
            Descricao = "Para lojas de sapatos, tênis e sandálias",
            Icone = "👟",
            Ativo = true,
            CriadoEm = DateTime.UtcNow,
        });
        mb.Entity<CategoriaPerfilLoja>().HasData(
            new CategoriaPerfilLoja { Id = Guid.Parse("14000000-0000-0000-0000-000000000001"), PerfilLojaId = perfilCalc, Nome = "Tênis", Ordem = 0 },
            new CategoriaPerfilLoja { Id = Guid.Parse("14000000-0000-0000-0000-000000000002"), PerfilLojaId = perfilCalc, Nome = "Sandália", Ordem = 1 },
            new CategoriaPerfilLoja { Id = Guid.Parse("14000000-0000-0000-0000-000000000003"), PerfilLojaId = perfilCalc, Nome = "Bota", Ordem = 2 },
            new CategoriaPerfilLoja { Id = Guid.Parse("14000000-0000-0000-0000-000000000004"), PerfilLojaId = perfilCalc, Nome = "Sapato", Ordem = 3 },
            new CategoriaPerfilLoja { Id = Guid.Parse("14000000-0000-0000-0000-000000000005"), PerfilLojaId = perfilCalc, Nome = "Chinelo", Ordem = 4 }
        );
        mb.Entity<CampoExtraPerfil>().HasData(
            new CampoExtraPerfil
            {
                Id = Guid.Parse("15000000-0000-0000-0000-000000000001"),
                PerfilLojaId = perfilCalc,
                Chave = "numero",
                Label = "Número",
                Tipo = "lista",
                Opcoes = "33,34,35,36,37,38,39,40,41,42,43,44,45",
                Obrigatorio = true,
                Ordem = 0,
            },
            new CampoExtraPerfil
            {
                Id = Guid.Parse("15000000-0000-0000-0000-000000000002"),
                PerfilLojaId = perfilCalc,
                Chave = "cor",
                Label = "Cor",
                Tipo = "texto",
                Obrigatorio = false,
                Ordem = 1,
            }
        );

        // ── Perfil: Pet Shop ──────────────────────────────────────────────
        var perfilPet = Guid.Parse("10000000-0000-0000-0000-000000000004");
        mb.Entity<PerfilLoja>().HasData(new PerfilLoja
        {
            Id = perfilPet,
            Nome = "Pet Shop",
            Descricao = "Para pet shops: ração a granel, petiscos, acessórios e mais",
            Icone = "🐾",
            Ativo = true,
            CriadoEm = DateTime.UtcNow,
        });
        mb.Entity<CategoriaPerfilLoja>().HasData(
            new CategoriaPerfilLoja { Id = Guid.Parse("16000000-0000-0000-0000-000000000001"), PerfilLojaId = perfilPet, Nome = "Ração", Ordem = 0 },
            new CategoriaPerfilLoja { Id = Guid.Parse("16000000-0000-0000-0000-000000000002"), PerfilLojaId = perfilPet, Nome = "Petiscos", Ordem = 1 },
            new CategoriaPerfilLoja { Id = Guid.Parse("16000000-0000-0000-0000-000000000003"), PerfilLojaId = perfilPet, Nome = "Brinquedos", Ordem = 2 },
            new CategoriaPerfilLoja { Id = Guid.Parse("16000000-0000-0000-0000-000000000004"), PerfilLojaId = perfilPet, Nome = "Higiene", Ordem = 3 },
            new CategoriaPerfilLoja { Id = Guid.Parse("16000000-0000-0000-0000-000000000005"), PerfilLojaId = perfilPet, Nome = "Acessórios", Ordem = 4 },
            new CategoriaPerfilLoja { Id = Guid.Parse("16000000-0000-0000-0000-000000000006"), PerfilLojaId = perfilPet, Nome = "Medicamentos", Ordem = 5 }
        );

        // ── Perfil: Conveniência ──────────────────────────────────────────
        var perfilConv = Guid.Parse("10000000-0000-0000-0000-000000000005");
        mb.Entity<PerfilLoja>().HasData(new PerfilLoja
        {
            Id = perfilConv,
            Nome = "Conveniência",
            Descricao = "Para lojas de conveniência, mercadinhos e similares",
            Icone = "🏪",
            Ativo = true,
            CriadoEm = DateTime.UtcNow,
        });
        mb.Entity<CategoriaPerfilLoja>().HasData(
            new CategoriaPerfilLoja { Id = Guid.Parse("17000000-0000-0000-0000-000000000001"), PerfilLojaId = perfilConv, Nome = "Bebidas", Ordem = 0 },
            new CategoriaPerfilLoja { Id = Guid.Parse("17000000-0000-0000-0000-000000000002"), PerfilLojaId = perfilConv, Nome = "Salgados", Ordem = 1 },
            new CategoriaPerfilLoja { Id = Guid.Parse("17000000-0000-0000-0000-000000000003"), PerfilLojaId = perfilConv, Nome = "Doces", Ordem = 2 },
            new CategoriaPerfilLoja { Id = Guid.Parse("17000000-0000-0000-0000-000000000004"), PerfilLojaId = perfilConv, Nome = "Cigarros", Ordem = 3 },
            new CategoriaPerfilLoja { Id = Guid.Parse("17000000-0000-0000-0000-000000000005"), PerfilLojaId = perfilConv, Nome = "Higiene", Ordem = 4 },
            new CategoriaPerfilLoja { Id = Guid.Parse("17000000-0000-0000-0000-000000000006"), PerfilLojaId = perfilConv, Nome = "Limpeza", Ordem = 5 },
            new CategoriaPerfilLoja { Id = Guid.Parse("17000000-0000-0000-0000-000000000007"), PerfilLojaId = perfilConv, Nome = "Mercearia", Ordem = 6 }
        );

        // ── Perfil: Material de Construção ─────────────────────────────────
        var perfilMat = Guid.Parse("10000000-0000-0000-0000-000000000006");
        mb.Entity<PerfilLoja>().HasData(new PerfilLoja
        {
            Id = perfilMat,
            Nome = "Material de Construção",
            Descricao = "Para lojas de materiais de construção e ferragens",
            Icone = "🧱",
            Ativo = true,
            CriadoEm = DateTime.UtcNow,
        });
        mb.Entity<CategoriaPerfilLoja>().HasData(
            new CategoriaPerfilLoja { Id = Guid.Parse("18000000-0000-0000-0000-000000000001"), PerfilLojaId = perfilMat, Nome = "Cimento e Argamassa", Ordem = 0 },
            new CategoriaPerfilLoja { Id = Guid.Parse("18000000-0000-0000-0000-000000000002"), PerfilLojaId = perfilMat, Nome = "Tijolos e Blocos", Ordem = 1 },
            new CategoriaPerfilLoja { Id = Guid.Parse("18000000-0000-0000-0000-000000000003"), PerfilLojaId = perfilMat, Nome = "Tintas", Ordem = 2 },
            new CategoriaPerfilLoja { Id = Guid.Parse("18000000-0000-0000-0000-000000000004"), PerfilLojaId = perfilMat, Nome = "Hidráulica", Ordem = 3 },
            new CategoriaPerfilLoja { Id = Guid.Parse("18000000-0000-0000-0000-000000000005"), PerfilLojaId = perfilMat, Nome = "Elétrica", Ordem = 4 },
            new CategoriaPerfilLoja { Id = Guid.Parse("18000000-0000-0000-0000-000000000006"), PerfilLojaId = perfilMat, Nome = "Ferramentas", Ordem = 5 },
            new CategoriaPerfilLoja { Id = Guid.Parse("18000000-0000-0000-0000-000000000007"), PerfilLojaId = perfilMat, Nome = "Madeiras", Ordem = 6 }
        );

        // ══════════════ PERFIS DE SERVIÇO ══════════════

        // ── Banho e Tosa (puro) ───────────────────────────────────────────
        var perfilBanho = Guid.Parse("20000000-0000-0000-0000-000000000001");
        mb.Entity<PerfilLoja>().HasData(new PerfilLoja
        {
            Id = perfilBanho,
            Nome = "Banho e Tosa",
            Descricao = "Para banho e tosa: agenda, serviços e caixa",
            Icone = "🐾",
            Ativo = true,
            TipoPlanoAplica = "servicos",
            CriadoEm = DateTime.UtcNow,
        });
        // ── Banho e Tosa + Loja ───────────────────────────────────────────
        var perfilBanhoLoja = Guid.Parse("20000000-0000-0000-0000-000000000002");
        mb.Entity<PerfilLoja>().HasData(new PerfilLoja
        {
            Id = perfilBanhoLoja,
            Nome = "Banho e Tosa + Loja",
            Descricao = "Banho e tosa com venda de produtos",
            Icone = "🐾",
            Ativo = true,
            TipoPlanoAplica = "loja_modulos",
            CriadoEm = DateTime.UtcNow,
        });

        // ── Barbearia (puro) ──────────────────────────────────────────────
        var perfilBarber = Guid.Parse("20000000-0000-0000-0000-000000000003");
        mb.Entity<PerfilLoja>().HasData(new PerfilLoja
        {
            Id = perfilBarber,
            Nome = "Barbearia",
            Descricao = "Para barbearias: agenda, serviços e caixa",
            Icone = "💈",
            Ativo = true,
            TipoPlanoAplica = "servicos",
            CriadoEm = DateTime.UtcNow,
        });
        // ── Barbearia + Loja ──────────────────────────────────────────────
        var perfilBarberLoja = Guid.Parse("20000000-0000-0000-0000-000000000004");
        mb.Entity<PerfilLoja>().HasData(new PerfilLoja
        {
            Id = perfilBarberLoja,
            Nome = "Barbearia + Loja",
            Descricao = "Barbearia com venda de produtos",
            Icone = "💈",
            Ativo = true,
            TipoPlanoAplica = "loja_modulos",
            CriadoEm = DateTime.UtcNow,
        });

        // ── Salão de Beleza (puro) ────────────────────────────────────────
        var perfilSalao = Guid.Parse("20000000-0000-0000-0000-000000000005");
        mb.Entity<PerfilLoja>().HasData(new PerfilLoja
        {
            Id = perfilSalao,
            Nome = "Salão de Beleza",
            Descricao = "Para salões: agenda, serviços e caixa",
            Icone = "💇",
            Ativo = true,
            TipoPlanoAplica = "servicos",
            CriadoEm = DateTime.UtcNow,
        });
        // ── Salão de Beleza + Loja ────────────────────────────────────────
        var perfilSalaoLoja = Guid.Parse("20000000-0000-0000-0000-000000000006");
        mb.Entity<PerfilLoja>().HasData(new PerfilLoja
        {
            Id = perfilSalaoLoja,
            Nome = "Salão de Beleza + Loja",
            Descricao = "Salão com venda de produtos",
            Icone = "💇",
            Ativo = true,
            TipoPlanoAplica = "loja_modulos",
            CriadoEm = DateTime.UtcNow,
        });

        // ── Financeiro Puro (sem loja) ─────────────────────────────────────
        var perfilFinanceiro = Guid.Parse("20000000-0000-0000-0000-000000000007");
        mb.Entity<PerfilLoja>().HasData(new PerfilLoja
        {
            Id = perfilFinanceiro,
            Nome = "Financeiro Puro",
            Descricao = "Controle financeiro pessoal ou do seu negócio — sem loja, sem estoque",
            Icone = "💰",
            Ativo = true,
            TipoPlanoAplica = "financeiro",
            CriadoEm = DateTime.UtcNow,
        });

        // ── Corretora (funil de vendas, seguradoras/operadoras, apólices) ──
        // TipoPlanoAplica próprio ("corretora"), não "loja" — assim o sistema não libera
        // Produtos/Caixa/Estoque pra loja que é só corretora. A promoção das 10 primeiras
        // continua valendo do mesmo jeito (ver AuthController.Signup, independe desse valor).
        var perfilCorretora = Guid.Parse("20000000-0000-0000-0000-000000000008");
        mb.Entity<PerfilLoja>().HasData(new PerfilLoja
        {
            Id = perfilCorretora,
            Nome = "Corretora",
            Descricao = "Funil de vendas, operadoras e comissões para corretores de seguro/planos",
            Icone = "📋",
            Ativo = true,
            TipoPlanoAplica = "corretora",
            CriadoEm = DateTime.UtcNow,
        });

        // ── Pilates / Turmas (aulas em grupo) ───────────────────────────────
        var perfilTurmas = Guid.Parse("20000000-0000-0000-0000-000000000009");
        mb.Entity<PerfilLoja>().HasData(new PerfilLoja
        {
            Id = perfilTurmas,
            Nome = "Pilates / Aulas em grupo",
            Descricao = "Matrícula fixa, agenda semanal, chamada e controle de faltas",
            Icone = "🧘",
            Ativo = true,
            TipoPlanoAplica = "turmas",
            CriadoEm = DateTime.UtcNow,
        });

        // ══════════════ SERVIÇOS PRÉ-DEFINIDOS DOS PERFIS ══════════════
        mb.Entity<ServicoPerfilLoja>().HasData(
            // ── Banho e Tosa (puro) — 21A ──
            new ServicoPerfilLoja { Id = Guid.Parse("21a00000-0000-0000-0000-000000000001"), PerfilLojaId = perfilBanho, Nome = "Banho (porte pequeno)", Categoria = "Banho", Preco = 40m, DuracaoMin = 60, Ordem = 0 },
            new ServicoPerfilLoja { Id = Guid.Parse("21a00000-0000-0000-0000-000000000002"), PerfilLojaId = perfilBanho, Nome = "Banho (porte médio)", Categoria = "Banho", Preco = 55m, DuracaoMin = 75, Ordem = 1 },
            new ServicoPerfilLoja { Id = Guid.Parse("21a00000-0000-0000-0000-000000000003"), PerfilLojaId = perfilBanho, Nome = "Banho (porte grande)", Categoria = "Banho", Preco = 75m, DuracaoMin = 90, Ordem = 2 },
            new ServicoPerfilLoja { Id = Guid.Parse("21a00000-0000-0000-0000-000000000004"), PerfilLojaId = perfilBanho, Nome = "Tosa higiênica", Categoria = "Tosa", Preco = 35m, DuracaoMin = 45, Ordem = 3 },
            new ServicoPerfilLoja { Id = Guid.Parse("21a00000-0000-0000-0000-000000000005"), PerfilLojaId = perfilBanho, Nome = "Tosa completa", Categoria = "Tosa", Preco = 70m, DuracaoMin = 90, Ordem = 4 },

            // ── Banho e Tosa + Loja — 21B ──
            new ServicoPerfilLoja { Id = Guid.Parse("21b00000-0000-0000-0000-000000000001"), PerfilLojaId = perfilBanhoLoja, Nome = "Banho (porte pequeno)", Categoria = "Banho", Preco = 40m, DuracaoMin = 60, Ordem = 0 },
            new ServicoPerfilLoja { Id = Guid.Parse("21b00000-0000-0000-0000-000000000002"), PerfilLojaId = perfilBanhoLoja, Nome = "Banho (porte médio)", Categoria = "Banho", Preco = 55m, DuracaoMin = 75, Ordem = 1 },
            new ServicoPerfilLoja { Id = Guid.Parse("21b00000-0000-0000-0000-000000000003"), PerfilLojaId = perfilBanhoLoja, Nome = "Banho (porte grande)", Categoria = "Banho", Preco = 75m, DuracaoMin = 90, Ordem = 2 },
            new ServicoPerfilLoja { Id = Guid.Parse("21b00000-0000-0000-0000-000000000004"), PerfilLojaId = perfilBanhoLoja, Nome = "Tosa higiênica", Categoria = "Tosa", Preco = 35m, DuracaoMin = 45, Ordem = 3 },
            new ServicoPerfilLoja { Id = Guid.Parse("21b00000-0000-0000-0000-000000000005"), PerfilLojaId = perfilBanhoLoja, Nome = "Tosa completa", Categoria = "Tosa", Preco = 70m, DuracaoMin = 90, Ordem = 4 },

            // ── Barbearia (puro) — 21C ──
            new ServicoPerfilLoja { Id = Guid.Parse("21c00000-0000-0000-0000-000000000001"), PerfilLojaId = perfilBarber, Nome = "Corte de cabelo", Categoria = "Cabelo", Preco = 35m, DuracaoMin = 30, Ordem = 0 },
            new ServicoPerfilLoja { Id = Guid.Parse("21c00000-0000-0000-0000-000000000002"), PerfilLojaId = perfilBarber, Nome = "Barba", Categoria = "Barba", Preco = 25m, DuracaoMin = 30, Ordem = 1 },
            new ServicoPerfilLoja { Id = Guid.Parse("21c00000-0000-0000-0000-000000000003"), PerfilLojaId = perfilBarber, Nome = "Corte + Barba", Categoria = "Combo", Preco = 55m, DuracaoMin = 60, Ordem = 2 },
            new ServicoPerfilLoja { Id = Guid.Parse("21c00000-0000-0000-0000-000000000004"), PerfilLojaId = perfilBarber, Nome = "Pezinho / acabamento", Categoria = "Acabamento", Preco = 15m, DuracaoMin = 15, Ordem = 3 },
            new ServicoPerfilLoja { Id = Guid.Parse("21c00000-0000-0000-0000-000000000005"), PerfilLojaId = perfilBarber, Nome = "Sobrancelha masculina", Categoria = "Sobrancelha", Preco = 15m, DuracaoMin = 15, Ordem = 4 },

            // ── Barbearia + Loja — 21D ──
            new ServicoPerfilLoja { Id = Guid.Parse("21d00000-0000-0000-0000-000000000001"), PerfilLojaId = perfilBarberLoja, Nome = "Corte de cabelo", Categoria = "Cabelo", Preco = 35m, DuracaoMin = 30, Ordem = 0 },
            new ServicoPerfilLoja { Id = Guid.Parse("21d00000-0000-0000-0000-000000000002"), PerfilLojaId = perfilBarberLoja, Nome = "Barba", Categoria = "Barba", Preco = 25m, DuracaoMin = 30, Ordem = 1 },
            new ServicoPerfilLoja { Id = Guid.Parse("21d00000-0000-0000-0000-000000000003"), PerfilLojaId = perfilBarberLoja, Nome = "Corte + Barba", Categoria = "Combo", Preco = 55m, DuracaoMin = 60, Ordem = 2 },
            new ServicoPerfilLoja { Id = Guid.Parse("21d00000-0000-0000-0000-000000000004"), PerfilLojaId = perfilBarberLoja, Nome = "Pezinho / acabamento", Categoria = "Acabamento", Preco = 15m, DuracaoMin = 15, Ordem = 3 },
            new ServicoPerfilLoja { Id = Guid.Parse("21d00000-0000-0000-0000-000000000005"), PerfilLojaId = perfilBarberLoja, Nome = "Sobrancelha masculina", Categoria = "Sobrancelha", Preco = 15m, DuracaoMin = 15, Ordem = 4 },

            // ── Salão de Beleza (puro) — 21E ──
            new ServicoPerfilLoja { Id = Guid.Parse("21e00000-0000-0000-0000-000000000001"), PerfilLojaId = perfilSalao, Nome = "Corte feminino", Categoria = "Cabelo", Preco = 60m, DuracaoMin = 60, Ordem = 0 },
            new ServicoPerfilLoja { Id = Guid.Parse("21e00000-0000-0000-0000-000000000002"), PerfilLojaId = perfilSalao, Nome = "Escova", Categoria = "Cabelo", Preco = 45m, DuracaoMin = 45, Ordem = 1 },
            new ServicoPerfilLoja { Id = Guid.Parse("21e00000-0000-0000-0000-000000000003"), PerfilLojaId = perfilSalao, Nome = "Coloração / tintura", Categoria = "Química", Preco = 120m, DuracaoMin = 120, Ordem = 2 },
            new ServicoPerfilLoja { Id = Guid.Parse("21e00000-0000-0000-0000-000000000004"), PerfilLojaId = perfilSalao, Nome = "Manicure", Categoria = "Unhas", Preco = 35m, DuracaoMin = 45, Ordem = 3 },
            new ServicoPerfilLoja { Id = Guid.Parse("21e00000-0000-0000-0000-000000000005"), PerfilLojaId = perfilSalao, Nome = "Pedicure", Categoria = "Unhas", Preco = 40m, DuracaoMin = 45, Ordem = 4 },

            // ── Salão de Beleza + Loja — 21F ──
            new ServicoPerfilLoja { Id = Guid.Parse("21f00000-0000-0000-0000-000000000001"), PerfilLojaId = perfilSalaoLoja, Nome = "Corte feminino", Categoria = "Cabelo", Preco = 60m, DuracaoMin = 60, Ordem = 0 },
            new ServicoPerfilLoja { Id = Guid.Parse("21f00000-0000-0000-0000-000000000002"), PerfilLojaId = perfilSalaoLoja, Nome = "Escova", Categoria = "Cabelo", Preco = 45m, DuracaoMin = 45, Ordem = 1 },
            new ServicoPerfilLoja { Id = Guid.Parse("21f00000-0000-0000-0000-000000000003"), PerfilLojaId = perfilSalaoLoja, Nome = "Coloração / tintura", Categoria = "Química", Preco = 120m, DuracaoMin = 120, Ordem = 2 },
            new ServicoPerfilLoja { Id = Guid.Parse("21f00000-0000-0000-0000-000000000004"), PerfilLojaId = perfilSalaoLoja, Nome = "Manicure", Categoria = "Unhas", Preco = 35m, DuracaoMin = 45, Ordem = 3 },
            new ServicoPerfilLoja { Id = Guid.Parse("21f00000-0000-0000-0000-000000000005"), PerfilLojaId = perfilSalaoLoja, Nome = "Pedicure", Categoria = "Unhas", Preco = 40m, DuracaoMin = 45, Ordem = 4 }
        );

        mb.Entity<ModuloPreco>().HasData(
            new ModuloPreco { Id = Guid.Parse("11111111-1111-1111-1111-111111111101"), Chave = "servicos", Nome = "Serviços e Agenda", Valor = 0, DisponivelParaAtivar = true },
            new ModuloPreco { Id = Guid.Parse("11111111-1111-1111-1111-111111111102"), Chave = "financeiro", Nome = "Financeiro (Contas a Pagar/Receber)", Valor = 29.90m, DisponivelParaAtivar = true },
            new ModuloPreco { Id = Guid.Parse("11111111-1111-1111-1111-111111111103"), Chave = "turmas", Nome = "Turmas (aulas em grupo)", Valor = 39.90m, DisponivelParaAtivar = true },
            new ModuloPreco { Id = Guid.Parse("11111111-1111-1111-1111-111111111104"), Chave = "etiquetas", Nome = "Impressão de etiquetas", Valor = 0, DisponivelParaAtivar = false },
            new ModuloPreco { Id = Guid.Parse("11111111-1111-1111-1111-111111111105"), Chave = "nf", Nome = "Importação de NF", Valor = 29.90m, DisponivelParaAtivar = true },
            new ModuloPreco { Id = Guid.Parse("11111111-1111-1111-1111-111111111106"), Chave = "chacara_reservas", Nome = "Reservas (Chácara/Temporada)", Valor = 0, DisponivelParaAtivar = true }
        );
    }

    private static string ToSnakeCase(string name)
    {
        return string.Concat(name.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + char.ToLower(c) : char.ToLower(c).ToString()));
    }
}
