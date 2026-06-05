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
            .HasOne(i => i.Produto)
            .WithMany(p => p.ItensVenda)
            .HasForeignKey(i => i.ProdutoId)
            .OnDelete(DeleteBehavior.Restrict);

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
    }

    private static string ToSnakeCase(string name)
    {
        return string.Concat(name.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + char.ToLower(c) : char.ToLower(c).ToString()));
    }
}
