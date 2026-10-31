using ECommerce.Application.Common.EventHandlers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Infrastructure.Persistence.Repositories;
using MediatR;
using NetArchTest.Rules;
using Xunit;

namespace ECommerce.ArchitectureTests;

public class ConventionTests
{
    [Fact]
    public void Handlers_Should_BeSealed()
    {
        var result = Types.InAssembly(typeof(IApplicationDbContext).Assembly)
            .That()
            .HaveNameEndingWith("CommandHandler")
            .Or()
            .HaveNameEndingWith("QueryHandler")
            .Or()
            .HaveNameEndingWith("DomainEventHandler")
            .Should()
            .BeSealed()
            .GetResult();

        Assert.True(result.IsSuccessful, DescribeFailures(result));
    }

    [Fact]
    public void NotificationHandlers_Should_DeriveFrom_IdempotentNotificationHandler()
    {
        var handlerInterface = typeof(INotificationHandler<>);
        var idempotentBaseType = typeof(IdempotentNotificationHandler<>);

        var offenders = typeof(IApplicationDbContext).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.GetInterfaces().Any(
                candidate =>
                    candidate.IsGenericType &&
                    candidate.GetGenericTypeDefinition() == handlerInterface))
            .Where(type => !DerivesFromOpenGeneric(type, idempotentBaseType))
            .Select(type => type.FullName)
            .ToList();

        Assert.Empty(offenders);
    }

    private static bool DerivesFromOpenGeneric(Type type, Type openGenericType)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType &&
                current.GetGenericTypeDefinition() == openGenericType)
            {
                return true;
            }
        }

        return false;
    }

    [Fact]
    public void Validators_Should_BeSealed()
    {
        var result = Types.InAssembly(typeof(IApplicationDbContext).Assembly)
            .That()
            .HaveNameEndingWith("Validator")
            .Should()
            .BeSealed()
            .GetResult();

        Assert.True(result.IsSuccessful, DescribeFailures(result));
    }

    [Fact]
    public void Domain_ShouldNot_Contain_RepositoryImplementations()
    {
        var offenders = GetConcreteRepositoryTypes(typeof(IDomainEvent).Assembly);

        Assert.Empty(offenders);
    }

    [Fact]
    public void Application_ShouldNot_Contain_RepositoryImplementations()
    {
        var offenders = GetConcreteRepositoryTypes(typeof(IApplicationDbContext).Assembly);

        Assert.Empty(offenders);
    }

    private static List<string?> GetConcreteRepositoryTypes(System.Reflection.Assembly assembly)
        => assembly.GetTypes()
            .Where(t => t.IsClass && t.Name.EndsWith("Repository", StringComparison.Ordinal))
            .Select(t => t.FullName)
            .ToList();

    [Fact]
    public void InfrastructureRepositories_Should_ResideIn_RepositoriesNamespace()
    {
        var result = Types.InAssembly(typeof(ProductRepository).Assembly)
            .That()
            .AreClasses()
            .And()
            .HaveNameEndingWith("Repository")
            .Should()
            .ResideInNamespace("ECommerce.Infrastructure.Persistence.Repositories")
            .GetResult();

        Assert.True(result.IsSuccessful, DescribeFailures(result));
    }

    [Fact]
    public void DomainEvents_Should_Implement_IDomainEvent()
    {
        var result = Types.InAssembly(typeof(IDomainEvent).Assembly)
            .That()
            .AreClasses()
            .And()
            .HaveNameEndingWith("DomainEvent")
            .Should()
            .ImplementInterface(typeof(IDomainEvent))
            .GetResult();

        Assert.True(result.IsSuccessful, DescribeFailures(result));
    }

    private static string DescribeFailures(TestResult result)
        => result.IsSuccessful
            ? string.Empty
            : "Violating types: " + string.Join(", ", result.FailingTypeNames ?? []);
}
