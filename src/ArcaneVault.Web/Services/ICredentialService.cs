namespace ArcaneVault.Web.Services;

public interface ICredentialService
{
    Task<IEnumerable<Credential>> GetCredentialsAsync();
}
