using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Wind.Azure.KeyVault
{
    using Configuration;

    public class KeyVaultProvider : IKeyVaultProvider
    {
        readonly KeyVault _keyVaultConfig;

        public KeyVaultProvider(KeyVault keyVaultConfig)
        {
            _keyVaultConfig = keyVaultConfig
                ?? throw new ArgumentNullException(nameof(keyVaultConfig));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="secretNames"></param>
        /// <param name="replaceDashesWithColons">
        ///   Set to false if the secrets are to be used with environment variables and not consumed by IConfigurationBuilder.
        /// </param>
        /// <returns></returns>
        public async Task<Dictionary<string, string>> GetKeyVaultSecretsAsync(
            List<string> secretNames = null, bool replaceDashesWithColons = true)
        {
            TokenCredential tokenCredential;

            if (string.IsNullOrEmpty(_keyVaultConfig.CertificateThumbprint))
            {
                // Can be set via VisualStudio under Options -> Azure Service Authentication.
                Environment.SetEnvironmentVariable("AZURE_TENANT_ID", _keyVaultConfig.TenantId);
                Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", _keyVaultConfig.ClientId);
                Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", _keyVaultConfig.ClientSecret);

                tokenCredential = new DefaultAzureCredential();
            }
            else
            {
                tokenCredential = GetCertificateCredential();
            }

            var options = new SecretClientOptions()
            {
                Retry =
                {
                    Delay = TimeSpan.FromSeconds(2),
                    MaxDelay = TimeSpan.FromSeconds(16),
                    MaxRetries = 5,
                    Mode = RetryMode.Exponential
                 }
            };
            var client = new SecretClient(new Uri(_keyVaultConfig.Uri)
                , tokenCredential, options);

            var secrets = new Dictionary<string, string>();

            if (secretNames == null || !secretNames.Any())
            {
                var secretProps = client.GetPropertiesOfSecretsAsync(CancellationToken.None);
                await foreach (var secretProp in secretProps)
                {
                    string secretName = replaceDashesWithColons 
                        ? secretProp.Name.Replace("--", ":") : secretProp.Name;
                    KeyVaultSecret secret = await client.GetSecretAsync(secretProp.Name);
                    secrets.Add(secretName, secret.Value);
                }

                return secrets;
            }

            foreach (string name in secretNames)
            {
                string secretName = replaceDashesWithColons
                    ? name.Replace("--", ":") : name;
                KeyVaultSecret secret = await client.GetSecretAsync(name);
                secrets.Add(secretName, secret.Value);
            }

            return secrets;
        }


        private async Task<string> GetAccessTokenByClientIdSecretAsync(string authority
            , string resource, string scope)
        {
            var clientCredentials = new ClientCredential(_keyVaultConfig.ClientId, _keyVaultConfig.ClientSecret);
            var context = new AuthenticationContext(authority, TokenCache.DefaultShared);
            var authResult = await context.AcquireTokenAsync(resource, clientCredentials);
            return authResult.AccessToken;
        }

        private async Task<string> GetAccessTokenByCertificateAsync(string authority
            , string resource, string scope)
        {
            X509Certificate2 cert = LoadCertificate(_keyVaultConfig.CertificateThumbprint);
            var clientAssertionCertificate = new ClientAssertionCertificate(_keyVaultConfig.ClientId, cert);
            var context = new AuthenticationContext(authority, TokenCache.DefaultShared);
            var authResult = await context.AcquireTokenAsync(resource, clientAssertionCertificate);
            return authResult.AccessToken;
        }

        private ClientCertificateCredential GetCertificateCredential()
        {
            X509Certificate2 cert = LoadCertificate(_keyVaultConfig.CertificateThumbprint);
            var clientCertificateCredential = new ClientCertificateCredential(
                _keyVaultConfig.TenantId, _keyVaultConfig.ClientId, cert);
            return clientCertificateCredential;
        }

        public static X509Certificate2 LoadCertificate(string thumbprint
            , StoreName storeName = StoreName.My
            , StoreLocation storeLocation = StoreLocation.LocalMachine)
        {
            using (var store = new X509Store(storeName, storeLocation))
            {
                store.Open(OpenFlags.ReadOnly);
                var certs = store
                    .Certificates
                    .Find(X509FindType.FindByThumbprint
                        , thumbprint, false);
                var cert = certs
                    .OfType<X509Certificate2>()
                    .Single();

                return cert;
            }
        }

        public static void SetEnvironmentVariables(IDictionary<string, string> data)
        {
            foreach (var kvp in data)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key)) { continue; }
                Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
            }
        }

    }
}
