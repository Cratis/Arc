// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver.given;

public class a_mongo_collection_parameter : Specification
{
    protected IMongoCollection<Balance> _collection;
    protected IMongoCollection<AdvisoryBalance> _advisoryCollection;
    protected Activity _activity;
    ServiceProvider _services;

    void Establish()
    {
        _collection = Substitute.For<IMongoCollection<Balance>>();
        _advisoryCollection = Substitute.For<IMongoCollection<AdvisoryBalance>>();
        var accessor = Substitute.For<ICommandContextAccessor>();
        accessor.Current.Returns(new CommandContext(Cratis.Execution.CorrelationId.New(), typeof(ReadCommand), new ReadCommand(), [], new()));
        var resolver = new ChronicleParameterDependencyResolver([typeof(Balance), typeof(AdvisoryBalance)]);
        _services = new ServiceCollection()
            .AddSingleton<IInstancesOf<ICanResolveParameterDependency>>(new KnownInstancesOf<ICanResolveParameterDependency>([resolver]))
            .AddSingleton<ICommandProtectedDecisionSupport>(new DecisionDependencySafety())
            .AddSingleton(accessor)
            .AddSingleton(_collection)
            .AddSingleton(_advisoryCollection)
            .BuildServiceProvider();
        _activity = new Activity("collection-parameter");
    }

    protected ValueTask<object?> Resolve(string method, bool asynchronously)
    {
        _activity.Start();
        var parameter = method == nameof(Validator)
            ? typeof(Validator).GetConstructors()[0].GetParameters()[0]
            : typeof(ReadCommand).GetMethod(method)!.GetParameters()[0];
        return asynchronously
            ? ParameterDependencyResolver.ResolveAsync(_services, parameter, MissingDependency)
            : new(ResolveSynchronously(parameter));
    }

    object? ResolveSynchronously(ParameterInfo parameter) => ParameterDependencyResolver.Resolve(_services, parameter, MissingDependency);

    protected object? AdvisoryTag(string name) => _activity.Events.Single().Tags.Single(tag => tag.Key == name).Value;

    void Destroy()
    {
        _activity.Dispose();
        _services.Dispose();
    }

    static Exception MissingDependency(ParameterInfo parameter) => new CannotResolveCommandDependency(parameter);

    public record Balance(string Id);
    [Unprotected("display only")]
    public record AdvisoryBalance(string Id);

    [ReadProtection(ReadProtectionMode.On)]
    public class ReadCommand
    {
        public void Handle([Unprotected("context only")] IMongoCollection<Balance> collection) { }
        public void FromType(IMongoCollection<AdvisoryBalance> collection) { }
        public void WithoutAnnotation(IMongoCollection<Balance> collection) { }
        public void WithoutReason([Unprotected] IMongoCollection<Balance> collection) { }
        public void OverrideReason([Unprotected("parameter reason")] IMongoCollection<AdvisoryBalance> collection) { }
    }

    [ReadProtection(ReadProtectionMode.Off)]
    public class OffCommand;

    public class Validator([Unprotected("context only")] IMongoCollection<Balance> collection) : CommandValidator<ReadCommand>
    {
        public IMongoCollection<Balance> Collection { get; } = collection;
    }
}
