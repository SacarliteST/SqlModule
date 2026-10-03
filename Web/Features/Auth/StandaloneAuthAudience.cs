using Microsoft.Extensions.Configuration;
using SQLModule.Web.Common.Auth;

namespace SQLModule.Web.Features.Auth;

internal static class StandaloneAuthAudience
{
    /// <summary>
    /// AuthOptions намеренно не зарегистрирован как IOptions&lt;AuthOptions&gt; — WebExtensions.AddWeb
    /// читает его в локальную переменную только для настройки JwtBearer. Читаем ту же секцию
    /// напрямую, чтобы всегда обменивать на ту же audience, которую сам SqlModule валидирует.
    /// </summary>
    public static string Resolve(IConfiguration configuration) =>
        configuration.GetSection(AuthOptions.SectionKey)[nameof(AuthOptions.Audience)] ?? String.Empty;
}
