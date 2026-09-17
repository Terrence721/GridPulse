using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Hosting;

public static class OptionsValidationExtensions
{
    public static IHostApplicationBuilder AddValidatedOptions<TOptions>(this IHostApplicationBuilder builder, string sectionName)
        where TOptions : class
    {
        builder.Services.Configure<TOptions>(builder.Configuration.GetSection(sectionName));
        builder.Services.AddOptions<TOptions>()
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return builder;
    }

    public static bool TryValidateStartupOptions<TOptions>(this IServiceProvider services)
        where TOptions : class
    {
        try
        {
            _ = services.GetRequiredService<IOptions<TOptions>>().Value;
            return true;
        }
        catch (OptionsValidationException ex)
        {
            foreach (var failure in ex.Failures)
            {
                Console.Error.WriteLine(failure);
            }

            return false;
        }
    }
}
