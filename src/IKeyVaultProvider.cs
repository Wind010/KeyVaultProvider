using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wind.Azure.KeyVault
{
    public interface IKeyVaultProvider
    {
        Task<Dictionary<string, string>> GetKeyVaultSecretsAsync(List<string> secretNames = null
            , bool replaceDashesWithColons = true);
    }
}
