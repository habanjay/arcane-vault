namespace ArcaneVault.Web.Infrastructure.EntityConfigurations;

internal class CredentialEntityTypeConfiguration : IEntityTypeConfiguration<Credential>
{
    public void Configure(EntityTypeBuilder<Credential> builder)
    {
        builder.ToTable("Credential");

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Username)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.Password)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(c => c.Url)
            .HasMaxLength(2083);

        builder.Property(c => c.Notes)
            .HasColumnType("nvarchar(max)");

        builder.Property(c => c.Created)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .ValueGeneratedOnAdd();

        builder.HasIndex(ci => ci.Username);
    }
}