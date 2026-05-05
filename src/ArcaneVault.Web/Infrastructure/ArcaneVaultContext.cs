namespace ArcaneVault.Web.Infrastructure;

public class ArcaneVaultContext : DbContext
{
    public ArcaneVaultContext(DbContextOptions<ArcaneVaultContext> options, IConfiguration configuration) : 
        base(options)
    {
    }

    public DbSet<Credential> Credentials { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CredentialEntityTypeConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}