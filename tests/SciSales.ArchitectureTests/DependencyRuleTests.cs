using System.Reflection;
using NetArchTest.Rules;
using SciSales.Application.Products;
using SciSales.Domain.Products;
using Shouldly;

namespace SciSales.ArchitectureTests;

/// <summary>
/// Hexagonal architecture is only real if something enforces it. A stray project
/// reference is easy to add and easy to miss in review; these tests fail the
/// build instead.
/// </summary>
public sealed class DependencyRuleTests
{
    private static readonly Assembly Domain = typeof(Product).Assembly;
    private static readonly Assembly Application = typeof(CreateProductHandler).Assembly;
    private static readonly Assembly Infrastructure = typeof(SciSales.Infrastructure.DependencyInjection).Assembly;

    private const string DomainNamespace = "SciSales.Domain";
    private const string ApplicationNamespace = "SciSales.Application";
    private const string InfrastructureNamespace = "SciSales.Infrastructure";
    private const string ApiNamespace = "SciSales.Api";

    [Fact]
    public void The_domain_depends_on_nothing()
    {
        var result = Types.InAssembly(Domain)
            .ShouldNot()
            .HaveDependencyOnAny(
                ApplicationNamespace,
                InfrastructureNamespace,
                ApiNamespace,
                "Microsoft.AspNetCore",
                "Microsoft.Data.SqlClient",
                "Microsoft.Extensions",
                "Dapper",
                "System.Net.Http")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailureMessage(result));
    }

    [Fact]
    public void The_application_layer_knows_nothing_about_the_adapters()
    {
        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOnAny(
                InfrastructureNamespace,
                ApiNamespace,
                "Microsoft.AspNetCore",
                "Microsoft.Data.SqlClient",
                "Dapper",
                "System.Net.Http")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailureMessage(result));
    }

    [Fact]
    public void Sql_server_stays_inside_the_persistence_adapter()
    {
        var offenders = Types.InAssembly(Infrastructure)
            .That()
            .HaveDependencyOn("Microsoft.Data.SqlClient")
            .GetTypes()
            .Where(type => type.Namespace?.StartsWith($"{InfrastructureNamespace}.Persistence", StringComparison.Ordinal) != true)
            .Select(type => type.FullName)
            .ToList();

        offenders.ShouldBeEmpty();
    }

    private static string FailureMessage(NetArchTest.Rules.TestResult result) =>
        "Types breaking the dependency rule: " +
        string.Join(", ", result.FailingTypeNames ?? []);
}
