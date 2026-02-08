using Microsoft.EntityFrameworkCore;
using Yugo.Server.Models;

namespace Yugo.Server.Data;

public class YugoDbContext : DbContext
{
    public YugoDbContext(DbContextOptions<YugoDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<SharedLevel> SharedLevels => Set<SharedLevel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<SharedLevel>()
            .HasOne(l => l.Author)
            .WithMany()
            .HasForeignKey(l => l.AuthorId);
    }
}
