using System.Reflection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using SQLModule.Web;
using SQLModule.Web.Common;

namespace SQLModule.UnitTests.Architecture;

/// <summary>Проверки скомпилированных сборок по метаданным (REF-001).</summary>
[Trait("Category", "Architecture")]
public sealed class AssemblyRulesTests
{
    private const BindingFlags AllMembers = BindingFlags.Public
                                            | BindingFlags.NonPublic
                                            | BindingFlags.Instance
                                            | BindingFlags.Static
                                            | BindingFlags.DeclaredOnly;

    [Fact(DisplayName = "Сборки слоёв не ссылаются на запрещённые сборки (сверх списка известных нарушений)")]
    public void LayerAssemblies_DoNotReferenceForbiddenAssemblies()
    {
        var violations = FindAssemblyViolations();
        var unexpected = violations.Except(ArchitectureRules.KnownAssemblyViolations.Keys).Order().ToList();

        unexpected.ShouldBeEmpty(
            $"Новые запрещённые ссылки между сборками: {String.Join(", ", unexpected)}. "
            + "Исправьте зависимость или добавьте запись в KnownAssemblyViolations с номером REF-задачи.");
    }

    [Fact(DisplayName = "Список известных нарушений ссылок на сборки не содержит исправленных записей")]
    public void KnownAssemblyViolations_AreStillPresent()
    {
        var stale = ArchitectureRules.KnownAssemblyViolations.Keys
            .Except(FindAssemblyViolations())
            .Order()
            .ToList();

        stale.ShouldBeEmpty($"Нарушение исправлено — удалите запись из KnownAssemblyViolations: {String.Join(", ", stale)}");
    }

    [Fact(DisplayName = "Web не содержит фоновых процессов (сверх списка известных нарушений)")]
    public void Web_DoesNotContainBackgroundServices_BeyondKnown()
    {
        var actual = WebBackgroundServices();

        actual.Except(ArchitectureRules.KnownWebBackgroundServices.Keys).Order().ToList()
            .ShouldBeEmpty("Новые BackgroundService в Web: фоновые процессы принадлежат Application/hosting, а не HTTP-слою.");
        ArchitectureRules.KnownWebBackgroundServices.Keys.Except(actual).Order().ToList()
            .ShouldBeEmpty("BackgroundService уже вынесен из Web — удалите его из KnownWebBackgroundServices.");
    }

    [Fact(DisplayName = "Endpoint-классы не получают AppDbContext, sandbox и интеграционные клиенты напрямую (сверх известных)")]
    public void Endpoints_DoNotDependOnInfrastructureDirectly_BeyondKnown()
    {
        var actual = FindEndpointViolations();

        actual.Except(ArchitectureRules.KnownEndpointViolations.Keys).Order().ToList()
            .ShouldBeEmpty(
                "Endpoint должен работать через ISender и не знать об инфраструктуре. "
                + "Проверяются сигнатуры (конструкторы, поля, свойства, параметры и результаты методов, "
                + "включая вложенные типы лямбд); вызовы GetRequiredService внутри тела не анализируются.");
        ArchitectureRules.KnownEndpointViolations.Keys.Except(actual).Order().ToList()
            .ShouldBeEmpty("Endpoint уже не использует инфраструктуру напрямую — удалите его из KnownEndpointViolations.");
    }

    [Fact(DisplayName = "Все проверяемые сборки загружаются (защита от тихого пропуска правил)")]
    public void AssemblyRules_CoverLoadedAssemblies()
    {
        // Application появится в REF-004; остальные сборки обязаны быть доступны тестовому проекту.
        var missing = ArchitectureRules.AssemblyDependencies
            .Select(rule => rule.Assembly)
            .Where(name => name != "SQLModule.Application" && TryLoad(name) is null)
            .ToList();

        missing.ShouldBeEmpty($"Не удалось загрузить сборки: {String.Join(", ", missing)}");
    }

    [Fact(DisplayName = "Все endpoint-классы Web найдены (защита от тихого пропуска проверки)")]
    public void Endpoints_AreDiscovered()
    {
        EndpointTypes().Count.ShouldBeGreaterThan(50);
    }

    private static List<string> FindAssemblyViolations()
    {
        var violations = new List<string>();
        foreach (var rule in ArchitectureRules.AssemblyDependencies)
        {
            var assembly = TryLoad(rule.Assembly);
            if (assembly is null)
            {
                continue;
            }

            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                var name = reference.Name ?? String.Empty;
                if (rule.ForbiddenPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
                {
                    violations.Add($"{rule.Assembly} -> {name}");
                }
            }
        }

        return violations;
    }

    private static Assembly? TryLoad(string name)
    {
        try
        {
            return Assembly.Load(name);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    private static List<string> WebBackgroundServices() =>
        typeof(IWebMarker).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.IsSubclassOf(typeof(BackgroundService)))
            .Select(type => type.Name)
            .Order()
            .ToList();

    private static List<Type> EndpointTypes() =>
        typeof(IWebMarker).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.IsAssignableTo(typeof(IEndpoint)))
            .ToList();

    private static List<string> FindEndpointViolations()
    {
        var violations = new List<string>();
        foreach (var endpoint in EndpointTypes())
        {
            var used = TypesUsedBy(endpoint).Select(type => type.Name).ToHashSet(StringComparer.Ordinal);
            if (ArchitectureRules.TypesForbiddenInEndpoints.Any(used.Contains))
            {
                violations.Add(endpoint.Name);
            }
        }

        return violations.Distinct().Order().ToList();
    }

    private static IEnumerable<Type> TypesUsedBy(Type type)
    {
        foreach (var constructor in type.GetConstructors(AllMembers))
        {
            foreach (var parameter in constructor.GetParameters())
            {
                foreach (var inner in Flatten(parameter.ParameterType))
                {
                    yield return inner;
                }
            }
        }

        foreach (var field in type.GetFields(AllMembers))
        {
            foreach (var inner in Flatten(field.FieldType))
            {
                yield return inner;
            }
        }

        foreach (var property in type.GetProperties(AllMembers))
        {
            foreach (var inner in Flatten(property.PropertyType))
            {
                yield return inner;
            }
        }

        foreach (var method in type.GetMethods(AllMembers))
        {
            foreach (var inner in Flatten(method.ReturnType))
            {
                yield return inner;
            }

            foreach (var parameter in method.GetParameters())
            {
                foreach (var inner in Flatten(parameter.ParameterType))
                {
                    yield return inner;
                }
            }
        }

        foreach (var nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
        {
            foreach (var inner in TypesUsedBy(nested))
            {
                yield return inner;
            }
        }
    }

    private static IEnumerable<Type> Flatten(Type type)
    {
        yield return type;

        if (type.HasElementType && type.GetElementType() is { } element)
        {
            foreach (var inner in Flatten(element))
            {
                yield return inner;
            }
        }

        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
            {
                foreach (var inner in Flatten(argument))
                {
                    yield return inner;
                }
            }
        }
    }
}
