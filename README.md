# KeyVaultProvider
A library that loads up KeyVault secrets manually to IConfiguration.


## Usage:

Example with IHostBuilder for use with WebHost:
```csharp
public static IHostBuilder CreateWebHostBuilder(string[] args) =>
 Host.CreateDefaultBuilder(args)
    .ConfigureFromKeyVault()
    .ConfigureAppConfiguration((context, configBuilder) =>
    {
        var mapping = new KeyValuePair<string, List<string>>("Environment_ONE", new List<string> { "Env1", "EnvOne" });
        configBuilder.UpdateConfigurationForEnvironment(mapping);
    })
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseStartup<Startup>();
    });
```


Example with IFunctionHostBuilder for use with Azure Function:
```csharp
public const string Azure_Functions_Environment = "AZURE_FUNCTIONS_ENVIRONMENT";
public const string Development = "Development";

public override void Configure(IFunctionsHostBuilder builder)
{
    IConfiguration config;
    IServiceCollection services = builder.Services;
    ILogger logger = LoggingConfigurator.CreateAppInsightsLogger();

    try
    {
        services.AddSingleton(logger);

        // Only for local debugging/development.
        string azEnv = Environment.GetEnvironmentVariable(Azure_Functions_Environment);
        config = IsLocalDevelopmentAsync(azEnv).GetAwaiter().GetResult();
        if (config == null)
        {
            var sp = services.BuildServiceProvider();
            config = sp.GetRequiredService<IConfiguration>();
        }

        var mapping = new KeyValuePair<string, List<string>>("Environment_ONE", new List<string> { "Env1", "EnvOne" });
        config = UpdateConfigurationForEnvironment(config, mapping).Build();
        services.AddAddressDependencies(config, logger);
    }
    catch(Exception ex)
    {
        if (logger != null)
        {
            logger.Error(ex, StartupError);
        }

        throw;
    }
}

        
internal async Task<IConfigurationRoot> IsLocalDevelopmentAsync(string environment
, string appSettingsFile = "local.settings.json", IKeyVaultProvider keyVaultProvider = null)
{
IConfigurationRoot config;

if (string.IsNullOrWhiteSpace(environment) || !environment
    .Equals(Development, StringComparison.OrdinalIgnoreCase))
{
    return null;
}

config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile(appSettingsFile, optional: true, reloadOnChange: true)
    .AddUserSecrets<Startup>()
    .AddEnvironmentVariables()
    .Build();

// Don't use AddAzureKeyVault here since there is assembly version issues
// leading to inablility to build the IConfigurationBuilder within that
// extension.  Resort to doing this manually.

var keyVaultConfig = config.GetConfiguration();
if (keyVaultConfig.Enabled)
{
    if (keyVaultProvider == null)
    {
        keyVaultProvider = new KeyVaultProvider(keyVaultConfig);
    }

    // Update manually:
    var dictSecrets = await keyVaultProvider.GetKeyVaultSecretsAsync();

    IConfigurationBuilder configBuilder = new ConfigurationBuilder();
    configBuilder.AddConfiguration(config);
    configBuilder.AddInMemoryCollection(dictSecrets);

    return configBuilder.Build();
}

return config;
}
```

