using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Infrastructure.Data.EFCore;
using DesafioComIA.Domain.Entities;
using DesafioComIA.Infrastructure.Data.Configurations;

namespace DesafioComIA.Infrastructure.Data;

public class ApplicationDbContext : Mvp24HoursContext
{
    public DbSet<Cliente> Clientes { get; set; } = null!;

    public ApplicationDbContext()
        : base()
    {
    }

    public ApplicationDbContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplicar configurações das entidades
        modelBuilder.ApplyConfiguration(new ClienteConfiguration());
    }
}
