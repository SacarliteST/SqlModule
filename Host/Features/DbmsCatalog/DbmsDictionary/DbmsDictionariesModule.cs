using Microsoft.Extensions.Caching.Memory;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Features.DbmsCatalog.DbmsDictionary.CreateDbmsDictionary;
using SQLModule.Host.Features.DbmsCatalog.DbmsDictionary.DeleteDbmsDictionary;
using SQLModule.Host.Features.DbmsCatalog.DbmsDictionary.GetAllDbmsDictionaries;
using SQLModule.Host.Features.DbmsCatalog.DbmsDictionary.GetDbmsDictionaryById;
using SQLModule.Host.Features.DbmsCatalog.DbmsDictionary.Sandbox;
using SQLModule.Host.Features.DbmsCatalog.DbmsDictionary.UpdateDbmsDictionary;
using SQLModule.Host.Features.DbmsCatalog.DbmsDictionary.ValidateDbmsDictionary;

namespace SQLModule.Host.Features.DbmsCatalog.DbmsDictionary;

internal static class DbmsDictionariesModule
{
    internal static IServiceCollection AddDbmsDictionaries(this IServiceCollection services)
    {
        services.AddOptions<DbmsCatalogOptions>().BindConfiguration(DbmsCatalogOptions.SectionKey);
        services.AddMemoryCache();
        services.AddSingleton<IDbmsProbe, TestcontainersDbmsProbe>();

        services.AddScoped<IRequestHandler<CreateDbmsDictionaryCommand, Result<DbmsDictionaryResponse>>, CreateDbmsDictionaryHandler>();
        services.AddScoped<IRequestHandler<ValidateDbmsDictionaryCommand, Result>, ValidateDbmsDictionaryHandler>();
        services.AddScoped<IRequestHandler<GetDbmsDictionaryByIdQuery, Result<DbmsDictionaryResponse>>, GetDbmsDictionaryByIdHandler>();
        services.AddScoped<IRequestHandler<GetAllDbmsDictionariesQuery, Result<PageResponse<DbmsDictionaryResponse>>>, GetAllDbmsDictionariesHandler>();
        services.AddScoped<IRequestHandler<UpdateDbmsDictionaryCommand, Result<DbmsDictionaryResponse>>, UpdateDbmsDictionaryHandler>();
        services.AddScoped<IRequestHandler<DeleteDbmsDictionaryCommand, Result>, DeleteDbmsDictionaryHandler>();

        return services;
    }
}
