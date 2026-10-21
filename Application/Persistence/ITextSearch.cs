using System.Linq.Expressions;

namespace SQLModule.Application.Persistence;

/// <summary>Регистронезависимый поиск по подстроке, трансляция которого зависит от провайдера БД.</summary>
public interface ITextSearch
{
    /// <summary>
    /// Оставляет элементы, у которых хотя бы одно из полей соответствует LIKE-шаблону без учёта регистра.
    /// Символ экранирования в шаблоне — обратная косая черта.
    /// </summary>
    IQueryable<T> WhereAnyMatchesIgnoreCase<T>(
        IQueryable<T> source,
        string likePattern,
        params Expression<Func<T, string?>>[] selectors);
}
