using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Persistence;

namespace SQLModule.Data.Core;

/// <summary>Регистронезависимый поиск через PostgreSQL <c>ILIKE</c>.</summary>
internal sealed class PostgresTextSearch : ITextSearch
{
    private static readonly MethodInfo ILikeMethod = typeof(NpgsqlDbFunctionsExtensions).GetMethod(
        nameof(NpgsqlDbFunctionsExtensions.ILike),
        [typeof(DbFunctions), typeof(string), typeof(string), typeof(string)])!;

    private const string EscapeCharacter = "\\";

    public IQueryable<T> WhereAnyMatchesIgnoreCase<T>(
        IQueryable<T> source,
        string likePattern,
        params Expression<Func<T, string?>>[] selectors)
    {
        ArgumentOutOfRangeException.ThrowIfZero(selectors.Length);

        var parameter = Expression.Parameter(typeof(T), "row");
        Expression? predicate = null;
        foreach (var selector in selectors)
        {
            var value = new ParameterReplacer(selector.Parameters[0], parameter).Visit(selector.Body);
            var match = Expression.Call(
                ILikeMethod,
                Expression.Property(null, typeof(EF), nameof(EF.Functions)),
                value,
                Expression.Constant(likePattern),
                Expression.Constant(EscapeCharacter));
            predicate = predicate is null ? match : Expression.OrElse(predicate, match);
        }

        return source.Where(Expression.Lambda<Func<T, bool>>(predicate!, parameter));
    }

    private sealed class ParameterReplacer(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : node;
    }
}
