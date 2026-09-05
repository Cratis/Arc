// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ReadModelUnresolvableDependencyClassifier.given;

public class a_classifier : Specification
{
    protected ReadModelUnresolvableDependencyClassifier _classifier;
    protected ParameterInfo _parameter;
    protected Exception? _failure;
    protected bool _result;

    void Establish()
    {
        _classifier = new ReadModelUnresolvableDependencyClassifier();
        _parameter = typeof(Consumer).GetMethod(nameof(Consumer.Method))!.GetParameters()[0];
    }

    protected IServiceProvider ServiceProviderWith(bool registerReadModel, string? resolvedKey)
    {
        var commandContextValues = new CommandContextValues();
        if (resolvedKey is not null)
        {
            commandContextValues[CommandContextKeys.ResolvedKey] = resolvedKey;
        }

        var commandContext = new CommandContext(CorrelationId.New(), typeof(TestCommand), new TestCommand(), [], commandContextValues);

        var services = new ServiceCollection();
        services.AddScoped(_ => commandContext);
        if (registerReadModel)
        {
            services.AddSingleton(new RegisteredReadModelTypes([typeof(TestReadModel)]));
        }

        return services.BuildServiceProvider();
    }

    class Consumer
    {
        public void Method(TestReadModel readModel) => _ = readModel;
    }

    protected record TestCommand;
    protected record TestReadModel(string Value);
}
