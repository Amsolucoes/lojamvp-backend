using Microsoft.EntityFrameworkCore;
using LojaApi.Models;

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

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // Índices únicos
        mb.Entity<Usuario>().HasIndex(u => u.Email).IsUnique();
        mb.Entity<Produto>().HasIndex(p => p.CodigoBarras).IsUnique().HasFilter("codigo_barras IS NOT NULL");

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
    }

    private static string ToSnakeCase(string name)
    {
        return string.Concat(name.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + char.ToLower(c) : char.ToLower(c).ToString()));
    }
}
