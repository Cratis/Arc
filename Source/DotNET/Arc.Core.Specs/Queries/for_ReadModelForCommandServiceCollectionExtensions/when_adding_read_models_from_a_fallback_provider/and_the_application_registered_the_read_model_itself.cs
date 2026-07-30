// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ReadModelForCommandServiceCollectionExtensions.when_adding_read_models_from_a_fallback_provider;

public class and_the_application_registered_the_read_model_itself : Specification
{
    ServiceProvider _rootProvider;
    IServiceScope _scope;
    FirstReadModel _fromApplication;
    RegisteredReadModelTypes _registeredReadModelTypes;
    object? _resolved;

    void Establish()
    {
        _fromApplication = new FirstReadModel("the application's own");
        var commandContext = new CommandContext(CorrelationId.New(), typeof(object), new object(), [], new CommandContextValues());

        var services = new ServiceCollection()
            .AddScoped(_ => commandContext)
            .AddSingleton(_fromApplication)
            .AddReadModelsForCommand(new a_read_model_resolver(
                [typeof(FirstReadModel)],
                new Dictionary<Type, object?> { [typeof(FirstReadModel)] = new FirstReadModel("fallback") },
                ReadModelForCommandOwnership.Fallback));

        _registeredReadModelTypes = services
            .First(descriptor => descriptor.ServiceType == typeof(RegisteredReadModelTypes))
            .ImplementationInstance as RegisteredReadModelTypes;

        _rootProvider = services.BuildServiceProvider();
        _scope = _rootProvider.CreateScope();
    }

    void Because() => _resolved = _scope.ServiceProvider.GetService(typeof(FirstReadModel));

    [Fact] void should_leave_the_application_registration_in_place() => _resolved.ShouldEqual(_fromApplication);
    [Fact] void should_not_treat_the_read_model_as_resolved_by_key() => _registeredReadModelTypes.Contains(typeof(FirstReadModel)).ShouldBeFalse();

    void Destroy()
    {
        _scope.Dispose();
        _rootProvider.Dispose();
    }
}
