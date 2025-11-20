using System.Reflection;
using Dima.Api.Models;
using Dima.Core.Models;
using Dima.Core.Models.Reports;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Dima.Api.Data;

//representacao do banco
public class AppDbContext(DbContextOptions<AppDbContext> options)
//prepara o banco para trbalhar com a parte de segurança
    : IdentityDbContext<
        User, IdentityRole<long>, long,
        IdentityUserClaim<long>,
        IdentityUserRole<long>,
        IdentityUserLogin<long>,
        IdentityRoleClaim<long>,
        IdentityUserToken<long>
    >(options)
{
    //cria a tabela no banco
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Voucher> Vouchers { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    
    public DbSet<IncomesAndExpenses> IncomesAndExpenses { get; set; } = null!;
    public DbSet<IncomesByCategory> IncomesByCategories { get; set; } = null!;
    public DbSet<ExpensesByCategory> ExpensesByCategories { get; set; } = null!;

    //mapeia(ligar/conectar) todas as classes que possuem o IEntityTypeConfiguration<>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); 

        // --- Correções para o Suporte a Passkeys (WebAuthn) ---

        // ERRO 1 (IdentityPasskeyData): Define IdentityPasskeyData como entidade sem chave.
        // Causa: EF Core a detecta, mas ela não tem chave primária.
        modelBuilder.Entity<IdentityPasskeyData>().HasNoKey();
        
        // ERRO 2 (IdentityUserPasskey.Data): Ignora a propriedade complexa 'Data'.
        // Causa: EF Core tenta mapear o relacionamento, mas a propriedade é serializada internamente.
        modelBuilder.Entity<IdentityUserPasskey<long>>()
            .Ignore(p => p.Data);

        // ERRO 3 (IdentityUserPasskey<long>): Define a chave primária composta.
        // Causa: EF Core não encontrou uma chave simples por convenção.
        modelBuilder.Entity<IdentityUserPasskey<long>>()
            .HasKey(p => new { p.UserId, p.CredentialId });
        
        
        //mapeia tabela para todas as classes que possuem IEntityTypeConfiguration<>
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        
        //mapeia um model para uma respectiva view no banco de dados
        modelBuilder.Entity<IncomesAndExpenses>()
            .HasNoKey()
            .ToView("vwGetIncomesAndExpenses");

        modelBuilder.Entity<IncomesByCategory>()
            .HasNoKey()
            .ToView("vwGetIncomesByCategory");

        modelBuilder.Entity<ExpensesByCategory>()
            .HasNoKey()
            .ToView("vwGetExpensesByCategory");
    }
}