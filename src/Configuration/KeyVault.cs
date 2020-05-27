using System.Diagnostics.CodeAnalysis;

namespace Wind.Azure.KeyVault.Configuration
{
    [ExcludeFromCodeCoverage]
    public class KeyVault
    {
        /// <summary>
        /// Load secrets from KeyVault.  Currently only meant for
        /// local testing. Secrets are provided from keyvault through the
        /// Service/Web App Configuration.
        /// </summary>
        public bool Enabled { get; set; }

        public string Uri => $"https://{Name}.vault.azure.net/";

        public string Name { get; set; }

        /// <summary>
        /// DirectoryId/SubscriptionId of the KeyVault
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// ServicePrincipal or ApplicationId
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Client or ApplicationSecret
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Populate ClientId/ClientSecret or CertificateThumbprint
        /// Preference to certificate for authentication.
        /// </summary>
        public string CertificateThumbprint { get; set; }
    }
}
