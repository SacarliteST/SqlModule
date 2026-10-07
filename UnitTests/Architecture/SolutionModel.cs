using System.Xml.Linq;

namespace SQLModule.UnitTests.Architecture;

/// <summary>Описание проекта решения, собранное из .csproj без запуска MSBuild.</summary>
/// <param name="Name">Имя проекта (имя файла .csproj без расширения).</param>
/// <param name="ProjectReferences">Имена проектов, на которые есть прямая ссылка.</param>
/// <param name="PackageReferences">Имена NuGet-пакетов, подключённых напрямую.</param>
internal sealed record ProjectInfo(
    string Name,
    IReadOnlySet<string> ProjectReferences,
    IReadOnlySet<string> PackageReferences);

/// <summary>
/// Модель решения для архитектурных тестов: читает .csproj репозитория, поэтому тесты
/// не зависят от Docker, базы данных и запущенного приложения.
/// </summary>
internal static class SolutionModel
{
    private const string SolutionFileName = "SQLModule.sln";

    private static readonly string[] TestProjectNames = ["UnitTests", "IntegrationTests", "TestsCommon"];

    /// <summary>Все проекты решения по имени.</summary>
    public static IReadOnlyDictionary<string, ProjectInfo> Projects { get; } = Load();

    /// <summary>Проекты, не относящиеся к тестовым.</summary>
    public static IEnumerable<ProjectInfo> ProductionProjects =>
        Projects.Values.Where(project => !IsTestProject(project.Name));

    /// <summary>Определяет, что проект является тестовым.</summary>
    public static bool IsTestProject(string name) => TestProjectNames.Contains(name);

    private static Dictionary<string, ProjectInfo> Load()
    {
        var root = FindSolutionRoot();
        var separator = Path.DirectorySeparatorChar;
        return Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{separator}bin{separator}")
                           && !path.Contains($"{separator}obj{separator}"))
            .Select(Parse)
            .ToDictionary(project => project.Name, StringComparer.Ordinal);
    }

    private static ProjectInfo Parse(string path)
    {
        var document = XDocument.Load(path);
        var projectReferences = IncludeValues(document, "ProjectReference")
            .Select(value => Path.GetFileNameWithoutExtension(value.Replace('\\', '/')))
            .ToHashSet(StringComparer.Ordinal);
        var packageReferences = IncludeValues(document, "PackageReference")
            .ToHashSet(StringComparer.Ordinal);

        return new ProjectInfo(
            Path.GetFileNameWithoutExtension(path),
            projectReferences,
            packageReferences);
    }

    private static IEnumerable<string> IncludeValues(XDocument document, string elementName) =>
        document.Descendants(elementName)
            .Select(element => (string?)element.Attribute("Include"))
            .OfType<string>();

    private static string FindSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Не найден {SolutionFileName} выше каталога {AppContext.BaseDirectory}.");
    }
}
