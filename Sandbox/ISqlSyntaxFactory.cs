namespace SQLModule.Sandbox;

/// <summary>Фабрика синтаксических помощников по системному имени СУБД.</summary>
public interface ISqlSyntaxFactory
{
    /// <summary>
    /// Возвращает <see cref="ISqlSyntax"/> для указанной СУБД
    /// или <c>null</c>, если диалект не поддерживается.
    /// </summary>
    ISqlSyntax? For(string systemName);
}
