using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Wind.Azure.KeyVault.Extensions
{
    using Configuration;

    public static class KeyVaultProviderExtensions
    {
        public const string AddingKeyVaultProvider = "Adding KeyVault Provider...";

        public static IServiceCollection AddKeyVaultProvider(this IServiceCollection services
            , IConfiguration configuration, ILogger logger =  null)
        {
            logger?.LogInformation(AddingKeyVaultProvider);

            var keyVaultConfig = new KeyVault();
            configuration.GetSection($"{nameof(KeyVault)}")
                .Bind(keyVaultConfig);

            services.AddSingleton<IKeyVaultProvider>(s =>
                new KeyVaultProvider(keyVaultConfig));

            return services;
        }

        public static KeyVault GetConfiguration(this IConfiguration config, string baseSectionName = null)
        {
            var keyVaultConfig = new KeyVault();
            string sectionName = nameof(KeyVault);

            if (baseSectionName != null)
            {
                sectionName = $"{baseSectionName}:{nameof(KeyVault)}";
            }

            config.GetSection(sectionName)
                .Bind(keyVaultConfig);

            return keyVaultConfig;
        }
    }
}
