using EF.Models;
using Microsoft.EntityFrameworkCore;

namespace EF.Contexts;
public class SQLiteContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderPayment> OrderPayments { get; set; }
    public DbSet<OrderRating> OrderRating { get; set; }
    public DbSet<Token> Token { get; set; }

    public SQLiteContext(DbContextOptions<SQLiteContext> options) : base(options) { }

    // The following configures EF to create a Sqlite database file in the
    // special "local" folder for your platform.
    // protected override void OnConfiguring(DbContextOptionsBuilder options)
    //     => options.UseSqlite($"Data Source=GostVentDB.db");
}