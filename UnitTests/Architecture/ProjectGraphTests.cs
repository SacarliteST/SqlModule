using Shouldly;

namespace SQLModule.UnitTests.Architecture;

/// <summary>Проверки графа проектов и NuGet-зависимостей по .csproj (REF-001).</summary>
[Trait("Category", "Architecture")]
public sealed class ProjectGraphTests
{
    [Fact(DisplayName = "Граф ссылок между проектами не содержит циклов")]
    public void ProjectGraph_HasNoCycles()
    {
        var cycle = FindCycle();

        cycle.ShouldBeNull($"Найден цикл ссылок между проектами: {String.Join(" -> ", cycle ?? [])}");
    }

    [Fact(DisplayName = "Production-проекты не ссылаются на тестовые проекты")]
    public void ProductionProjects_DoNotReferenceTestProjects()
    {
        var violations = SolutionModel.ProductionProjects
            .SelectMany(project => project.ProjectReferences
                .Where(SolutionModel.IsTestProject)
                .Select(reference => $"{project.Name} -> {reference}"))
            .ToList();

        violations.ShouldBeEmpty($"Production-код не может зависеть от тестов: {String.Join(", ", violations)}");
    }

    [Fact(DisplayName = "Проекты ссылаются только на разрешённые проекты (сверх списка известных нарушений)")]
    public void ProjectReferences_DoNotBreakTargetGraph()
    {
        var unexpected = FindProjectViolations()
            .Keys
            .Except(ArchitectureRules.KnownProjectViolations.Keys)
            .Order()
            .ToList();

        unexpected.ShouldBeEmpty(Describe("Новые нарушения графа проектов", unexpected, FindProjectViolations()));
    }

    [Fact(DisplayName = "Список известных нарушений графа проектов не содержит исправленных записей")]
    public void KnownProjectViolations_AreStillPresent()
    {
        var actual = FindProjectViolations().Keys;
        var stale = ArchitectureRules.KnownProjectViolations.Keys.Except(actual).Order().ToList();

        stale.ShouldBeEmpty($"Нарушение исправлено — удалите его из KnownProjectViolations: {String.Join(", ", stale)}");
    }

    [Fact(DisplayName = "Проекты не подключают запрещённые NuGet-пакеты (сверх списка известных нарушений)")]
    public void PackageReferences_DoNotBreakTargetRules()
    {
        var unexpected = FindPackageViolations()
            .Keys
            .Except(ArchitectureRules.KnownPackageViolations.Keys)
            .Order()
            .ToList();

        unexpected.ShouldBeEmpty(Describe("Новые нарушения пакетных правил", unexpected, FindPackageViolations()));
    }

    [Fact(DisplayName = "Список известных нарушений пакетных правил не содержит исправленных записей")]
    public void KnownPackageViolations_AreStillPresent()
    {
        var actual = FindPackageViolations().Keys;
        var stale = ArchitectureRules.KnownPackageViolations.Keys.Except(actual).Order().ToList();

        stale.ShouldBeEmpty($"Нарушение исправлено — удалите его из KnownPackageViolations: {String.Join(", ", stale)}");
    }

    [Fact(DisplayName = "Правила графа ссылаются на существующие проекты решения")]
    public void ProjectRules_ReferenceExistingProjectsOrFutureApplication()
    {
        // Application создаётся в REF-004, поэтому его отсутствие допустимо; опечатки в остальных именах — нет.
        var known = SolutionModel.Projects.Keys.Append("Application").ToHashSet(StringComparer.Ordinal);
        var unknown = ArchitectureRules.ProjectDependencies
            .SelectMany(rule => rule.Allowed.Append(rule.Project))
            .Concat(ArchitectureRules.ForbiddenPackages.Select(rule => rule.Project))
            .Where(name => !known.Contains(name))
            .Distinct()
            .Order()
            .ToList();

        unknown.ShouldBeEmpty($"В правилах есть неизвестные проекты: {String.Join(", ", unknown)}");
    }

    private static Dictionary<string, string> FindProjectViolations()
    {
        var violations = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var rule in ArchitectureRules.ProjectDependencies)
        {
            if (!SolutionModel.Projects.TryGetValue(rule.Project, out var project))
            {
                continue;
            }

            foreach (var reference in project.ProjectReferences.Where(name => !rule.Allowed.Contains(name)))
            {
                violations[$"{rule.Project} -> {reference}"] = rule.Reason;
            }
        }

        return violations;
    }

    private static Dictionary<string, string> FindPackageViolations()
    {
        var violations = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var rule in ArchitectureRules.ForbiddenPackages)
        {
            if (!SolutionModel.Projects.TryGetValue(rule.Project, out var project))
            {
                continue;
            }

            foreach (var package in project.PackageReferences.Where(name => rule.Forbidden.Any(pattern => Matches(pattern, name))))
            {
                violations[$"{rule.Project} -> {package}"] = rule.Reason;
            }
        }

        return violations;
    }

    private static bool Matches(string pattern, string packageName) =>
        pattern == "*"
        || (pattern.EndsWith('*')
            ? packageName.StartsWith(pattern[..^1], StringComparison.Ordinal)
            : String.Equals(pattern, packageName, StringComparison.Ordinal));

    private static string Describe(string title, List<string> unexpected, Dictionary<string, string> all) =>
        $"{title}: " + String.Join("; ", unexpected.Select(edge => $"{edge} ({all[edge]})"))
        + ". Исправьте зависимость или (временно) добавьте запись в известные нарушения с номером REF-задачи.";

    private static List<string>? FindCycle()
    {
        var graph = SolutionModel.Projects.Values.ToDictionary(
            project => project.Name,
            project => project.ProjectReferences.Where(SolutionModel.Projects.ContainsKey).ToList(),
            StringComparer.Ordinal);

        var state = new Dictionary<string, int>(StringComparer.Ordinal);
        var path = new List<string>();

        foreach (var name in graph.Keys)
        {
            var cycle = Visit(name);
            if (cycle is not null)
            {
                return cycle;
            }
        }

        return null;

        List<string>? Visit(string node)
        {
            if (state.GetValueOrDefault(node) == 2)
            {
                return null;
            }

            if (state.GetValueOrDefault(node) == 1)
            {
                return [.. path.SkipWhile(item => item != node), node];
            }

            state[node] = 1;
            path.Add(node);
            foreach (var next in graph[node])
            {
                var cycle = Visit(next);
                if (cycle is not null)
                {
                    return cycle;
                }
            }

            path.RemoveAt(path.Count - 1);
            state[node] = 2;
            return null;
        }
    }
}
