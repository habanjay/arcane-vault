namespace ArcaneVault.Web.Extensions;

public static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.AddSqlServerDbContext<ArcaneVaultContext>("arcanevaultdb");
    }
}