using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;
using SQLModule.Application.Cqrs;
using SQLModule.Application.DbmsCatalog.DbmsDictionary.CreateDbmsDictionary;
using SQLModule.Application.DbmsCatalog.DbmsDictionary.DeleteDbmsDictionary;
using SQLModule.Application.DbmsCatalog.DbmsDictionary.GetAllDbmsDictionaries;
using SQLModule.Application.DbmsCatalog.DbmsDictionary.GetDbmsDictionaryById;
using SQLModule.Application.DbmsCatalog.DbmsDictionary.UpdateDbmsDictionary;
using SQLModule.Application.DbmsCatalog.DbmsDictionary.ValidateDbmsDictionary;

namespace SQLModule.Application.DbmsCatalog.DbmsDictionary;

public static class DbmsDictionariesModule
{
    public static IServiceCollection AddDbmsDictionaries(this IServiceCollection services)
    {

        services.AddScoped<IRequestHandler<CreateDbmsDictionaryCommand, Result<DbmsDictionaryResponse>>, CreateDbmsDictionaryHandler>();
        services.AddScoped<IRequestHandler<ValidateDbmsDictionaryCommand, Result>, ValidateDbmsDictionaryHandler>();
        services.AddScoped<IRequestHandler<GetDbmsDictionaryByIdQuery, Result<DbmsDictionaryResponse>>, GetDbmsDictionaryByIdHandler>();
        services.AddScoped<IRequestHandler<GetAllDbmsDictionariesQuery, Result<PageResponse<DbmsDictionaryResponse>>>, GetAllDbmsDictionariesHandler>();
        services.AddScoped<IRequestHandler<UpdateDbmsDictionaryCommand, Result<DbmsDictionaryResponse>>, UpdateDbmsDictionaryHandler>();
        services.AddScoped<IRequestHandler<DeleteDbmsDictionaryCommand, Result>, DeleteDbmsDictionaryHandler>();

        return services;
    }
}
