using Microsoft.EntityFrameworkCore;
using PlataformaELearning.Models;

namespace PlataformaELearning.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
    }
}
