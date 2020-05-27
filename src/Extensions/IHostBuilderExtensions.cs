using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace Wind.Azure.KeyVault.Extensions
{
    public static class IHostBuilderExtensions
    {
        public const string TestEnvironment = "TestEnvironment";

        /// <summary>
        /// Will load values directly from KeyVault with authentication through X509  
        /// certificate or client credentials.
        /// </summary>
        /// <param name="hostBuilder"><see cref="IHostBuilder"/></param>
        /// <returns><see cref="IHostBuilder"/></returns>
        public static IHostBuilder ConfigureFromKeyVault(this IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureAppConfiguration((context, configBuilder) =>
            {
                AddAzureKeyVault(configBuilder);
            });

            return hostBuilder;
        }

        public static IConfigurationBuilder AddAzureKeyVault(this IConfigurationBuilder configBuilder)
        {
            // We have to build the configuration first to get whether
            // or not we need to load KeyVault values explicitly.
            //var config = configBuilder.Build();
            var keyVaultConfig = configBuilder.Build().GetConfiguration();

            if (! keyVaultConfig.Enabled)
            {
                return configBuilder;
            }

            if (string.IsNullOrWhiteSpace(keyVaultConfig.CertificateThumbprint))
            {
                configBuilder.AddAzureKeyVault(keyVaultConfig.Uri, keyVaultConfig.ClientId
                    , keyVaultConfig.ClientSecret);

                return configBuilder;
            }

            // Preference for authentication by certificate.
            X509Certificate2 cert = KeyVaultProvider.LoadCertificate(keyVaultConfig.CertificateThumbprint);
            configBuilder.AddAzureKeyVault(keyVaultConfig.Uri, keyVaultConfig.ClientId, cert);

            return configBuilder;
        }


        public static IHostBuilder ConfigureEnvironmentVariables(this IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureAppConfiguration((context, configBuilder) =>
            {
                SetEnvironmentVariablesFromConfiguration(configBuilder.Build());
            });

            return hostBuilder;
        }

        public static IConfiguration SetEnvironmentVariablesFromConfiguration(this IConfiguration config)
        {
            var dict = config.AsEnumerable().ToDictionary(x => x.Key, x => x.Value);
            KeyVaultProvider.SetEnvironmentVariables(dict);
            return config;
        }

        public static IConfigurationBuilder UpdateConfigurationForEnvironment(this IConfigurationBuilder configBuilder
            , KeyValuePair<string, List<string>> maps)
        {
            IConfiguration config = configBuilder.Build();

            IConfigurationSection testEnvSection = config.GetChildren().Where(c =>
                c.Key.Equals(TestEnvironment, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            string testEnv = testEnvSection?.Value;

            if (string.IsNullOrWhiteSpace(testEnv))
            {
                return configBuilder;
            }

            if (maps.Value != null && maps.Value.Any(e => e.Equals(testEnv, StringComparison.OrdinalIgnoreCase)))
            {
                testEnv = maps.Key;
            }

            var envSpecificSettings = new Dictionary<string, string>();
            foreach (var section in config.AsEnumerable().ToList())
            {
                string value = config.GetValue<string>(section.Key);
                string searchToken = $"{testEnv}-";

                if (!string.IsNullOrWhiteSpace(value) && section.Key.Contains(testEnv
                    , StringComparison.OrdinalIgnoreCase))
                {
                    string key = Regex.Replace(section.Key, searchToken, string.Empty, RegexOptions.IgnoreCase);
                    envSpecificSettings.Add(key, value);
                }
            }

            configBuilder.AddInMemoryCollection(envSpecificSettings);

            return configBuilder;
        }




    }
}
