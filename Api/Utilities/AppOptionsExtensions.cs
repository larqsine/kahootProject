using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Api.Utilities;

public static class AppOptionsExtensions
{
    public static AppOptions AddAppOptions(this IServiceCollection services)
    {
        var appOptions = services.BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<AppOptions>>()
            .CurrentValue;
        ICollection<ValidationResult> results = new List<ValidationResult>();
        var validated = Validator.TryValidateObject(appOptions, new ValidationContext(appOptions), results, true);
        if (!validated)
            throw new Exception(
                $"hey buddy, alex here. You're probably missing an environment variable / appsettings.json stuff / repo secret on github. Here's the technical error: " +
                $"{string.Join(", ", results.Select(r => r.ErrorMessage))}");
        return appOptions;
    }
}