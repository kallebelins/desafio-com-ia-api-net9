using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Infrastructure.Data.EFCore;

namespace DesafioComIA.Infrastructure.Data;

public class ApplicationDbContext : Mvp24HoursContext
{
    public ApplicationDbContext()
        : base()
    {
    }

    public ApplicationDbContext(DbContextOptions options)
        : base(options)
    {
    }
}
