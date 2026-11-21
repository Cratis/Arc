// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Cratis.Chronicle;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions.given;

public class an_application_parameter_resolver : Specification
{
    protected IServiceCollection _services;
    protected ServiceProvider _root;
    protected IServiceScope _scope;
    protected CommandHandlerArgumentResolution _result;
    protected ApplicationDependency _dependency;

    void Establish()
    {
        _dependency = new();
        _services = new ServiceCollection()
            .AddScoped(_ => _dependency)
            .AddReadModels(Substitute.For<IClientArtifactsProvider>());
    }

    protected void BuildScope()
    {
        _root = _services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        _scope = _root.CreateScope();
    }

    protected object? ResolveSynchronously()
    {
        BuildScope();
        return ParameterDependencyResolver.Resolve(_scope.ServiceProvider, typeof(Handler).GetMethod(nameof(Handler.Handle))!.GetParameters()[0], _ => new InvalidOperationException());
    }

    protected async Task Resolve()
    {
        BuildScope();
        var handler = Substitute.For<ICommandHandler>();
        handler.Parameters.Returns(typeof(Handler).GetMethod(nameof(Handler.Handle))!.GetParameters());
        var context = new CommandContext(CorrelationId.New(), typeof(Handler), new Handler(), [], new());
        _result = await new CommandHandlerArgumentResolver(new CommandProvideInvoker()).Resolve(handler, context, _scope.ServiceProvider, allowedSeverity: null);
    }

    void Destroy()
    {
        _scope?.Dispose();
        _root?.Dispose();
    }

    public record BusinessDate(DateOnly Value);

    public class ApplicationDependency
    {
        public BusinessDate Date { get; } = new(new DateOnly(2026, 5, 7));
        public int ResolverInstances { get; set; }
    }

    public class ApplicationResolver(ApplicationDependency dependency) : ICanResolveParameterDependency
    {
        readonly ApplicationDependency _dependency = CountInstance(dependency);

        public bool TryResolve(IServiceProvider serviceProvider, ParameterInfo parameter, out object? dependency)
        {
            dependency = _dependency.Date;
            return parameter.ParameterType == typeof(BusinessDate);
        }

        static ApplicationDependency CountInstance(ApplicationDependency dependency)
        {
            dependency.ResolverInstances++;
            return dependency;
        }
    }

    public class AsyncApplicationResolver(ApplicationDependency state) : IAsyncParameterDependencyResolver
    {
        public bool TryResolve(IServiceProvider serviceProvider, ParameterInfo parameter, out object? dependency)
        {
            dependency = null;
            return false;
        }

        public async ValueTask<ParameterDependencyResolution> Resolve(IServiceProvider serviceProvider, ParameterInfo parameter)
        {
            await Task.Yield(); // Widen the interleaving window to exercise asynchronous binding.
            return parameter.ParameterType == typeof(BusinessDate)
                ? ParameterDependencyResolution.Resolved(state.Date)
                : ParameterDependencyResolution.NotResolved;
        }
    }

    class Handler
    {
        public void Handle(BusinessDate date) { }
    }
}
