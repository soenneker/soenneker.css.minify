using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Css.Minify.Abstract;
using Soenneker.Utils.File.Registrars;

namespace Soenneker.Css.Minify.Registrars;

/// <summary>
/// A utility that minifies stylesheets
/// </summary>
public static class CssMinifierRegistrar
{
    /// <summary>
    /// Adds as a scoped service.
    /// </summary>
    /// <param name="services">Service collection that receives the registration.</param>
    /// <returns>The same service collection, so additional registrations can be chained.</returns>
    public static IServiceCollection AddCssMinifierAsScoped(this IServiceCollection services)
    {
        services.AddFileUtilAsScoped().TryAddScoped<ICssMinifier, CssMinifier>();
        return services;
    }

    /// <summary>
    /// Adds as a singleton service.
    /// </summary>
    /// <param name="services">Service collection that receives the registration.</param>
    /// <returns>The same service collection, so additional registrations can be chained.</returns>
    public static IServiceCollection AddCssMinifierAsSingleton(this IServiceCollection services)
    {
        services.AddFileUtilAsSingleton().TryAddSingleton<ICssMinifier, CssMinifier>();
        return services;
    }
}
