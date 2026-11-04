// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Cratis.Chronicle;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

public class when_an_application_overrides_a_decision_token : Specification
{
    [Theory]
    [InlineData(false, "before")]
    [InlineData(false, "after")]
    [InlineData(false, "replace")]
    [InlineData(true, "before")]
    [InlineData(true, "after")]
    [InlineData(true, "replace")]
    public async Task should_use_the_effective_application_factory(bool attested, string position)
    {
        var token = for_CommandDecisionReads.DecisionFixtures.Protected("application", new ModelBoundReadModel("application"));
        var calls = 0;
        var application = ServiceDescriptor.Transient<DecisionRead<ModelBoundReadModel>>(_ =>
        {
            calls++;
            return token;
        });
        var services = Registrations();
        if (position == "before") services.Add(application);
        AddModels(services);
        if (position == "after") services.Add(application);
        if (position == "replace") services.Replace(application);
        await using var root = (ServiceProvider)(attested ? services.BuildProtectedDecisionServiceProvider() : services.BuildServiceProvider());
        await using var scope = root.CreateAsyncScope();
        var parameter = typeof(Handler).GetMethod(nameof(Handler.Handle))!.GetParameters()[0];

        var first = await ParameterDependencyResolver.ResolveAsync(scope.ServiceProvider, parameter, _ => new InvalidOperationException());
        var second = await ParameterDependencyResolver.ResolveAsync(scope.ServiceProvider, parameter, _ => new InvalidOperationException());

        first.ShouldBeSame(token);
        second.ShouldBeSame(token);
        calls.ShouldEqual(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_keep_each_built_roots_effective_registrations(bool attested)
    {
        var services = AddModels(Registrations());
        await using var original = (ServiceProvider)(attested ? services.BuildProtectedDecisionServiceProvider() : services.BuildServiceProvider());
        var application = for_CommandDecisionReads.DecisionFixtures.Protected("application", new ModelBoundReadModel("application"));
        services.AddTransient(_ => application);
        await using var changed = (ServiceProvider)(attested ? services.BuildProtectedDecisionServiceProvider() : services.BuildServiceProvider());
        await using var originalScope = original.CreateAsyncScope();
        await using var changedScope = changed.CreateAsyncScope();
        var parameter = typeof(Handler).GetMethod(nameof(Handler.Handle))!.GetParameters()[0];

        var first = (DecisionRead<ModelBoundReadModel>)(await ParameterDependencyResolver.ResolveAsync(originalScope.ServiceProvider, parameter, _ => new InvalidOperationException()))!;
        var second = await ParameterDependencyResolver.ResolveAsync(changedScope.ServiceProvider, parameter, _ => new InvalidOperationException());

        first.Instance!.Value.ShouldEqual("framework");
        second.ShouldBeSame(application);
    }

    static IServiceCollection Registrations()
    {
        var accessor = Substitute.For<ICommandContextAccessor>();
        accessor.Current.Returns(new CommandContext(Cratis.Execution.CorrelationId.New(), typeof(Handler), new Handler(), [], new()
        {
            [WellKnownCommandContextKeys.EventSourceId] = (Cratis.Chronicle.Events.EventSourceId)"framework"
        }));
        var reader = Substitute.For<IDecisionReads>();
        reader.Get<ModelBoundReadModel>(Arg.Any<ReadModelKey>(), Arg.Any<CancellationToken>())
            .Returns(for_CommandDecisionReads.DecisionFixtures.Protected("framework", new ModelBoundReadModel("framework")));
        return new ServiceCollection().AddSingleton(accessor).AddSingleton(reader);
    }

    static IServiceCollection AddModels(IServiceCollection services)
    {
        var reader = (IDecisionReads)services.Single(descriptor => descriptor.ServiceType == typeof(IDecisionReads)).ImplementationInstance!;
        return services.AddReadModels(Artifacts()).AddSingleton(reader);
    }

    static IClientArtifactsProvider Artifacts()
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.ModelBoundProjections.Returns([typeof(ModelBoundReadModel)]);
        return artifacts;
    }

    class Handler
    {
        public void Handle(DecisionRead<ModelBoundReadModel> model) { }
    }
}
