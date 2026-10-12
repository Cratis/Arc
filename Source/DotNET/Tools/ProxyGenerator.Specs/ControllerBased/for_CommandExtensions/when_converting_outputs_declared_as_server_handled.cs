// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.ControllerBased.for_CommandExtensions;

public class when_converting_outputs_declared_as_server_handled : Specification
{
    Dictionary<string, CommandDescriptor> _descriptors;
    Dictionary<string, string> _proxies;

    void Because()
    {
        _descriptors = typeof(given.response_actions).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .ToDictionary(_ => _.Name, _ => _.ToCommandDescriptor("/output", 0));
        _proxies = _descriptors.ToDictionary(_ => _.Key, _ => InMemoryProxyGenerator.GenerateCommand(_.Value));
    }

    [Theory]
    [InlineData(nameof(given.response_actions.EventResponse), typeof(ProjectRegistered), false)]
    [InlineData(nameof(given.response_actions.AsyncEventResponse), typeof(ProjectRegistered), false)]
    [InlineData(nameof(given.response_actions.EventCollectionResponse), typeof(ProjectRegistered), true)]
    [InlineData(nameof(given.response_actions.NestedTupleResponse), typeof(ProjectRegistered), false)]
    [InlineData(nameof(given.response_actions.TupleInUnionResponse), typeof(ProjectRegistered), false)]
    [InlineData(nameof(given.response_actions.UnionInTupleResponse), typeof(ProjectRegistered), false)]
    [InlineData(nameof(given.response_actions.UnionResponse), typeof(ProjectRegistered), false)]
    [InlineData(nameof(given.response_actions.CollectionInUnionResponse), typeof(ProjectRegistered), true)]
    [InlineData(nameof(given.response_actions.TypedInUnionResponse), typeof(DependencyHandledValue), false)]
    [InlineData(nameof(given.response_actions.TypedResponse), typeof(DependencyHandledValue), false)]
    [InlineData(nameof(given.response_actions.OperationResponse), typeof(given.ReturnedOperation), false)]
    [InlineData(nameof(given.response_actions.OperationCollectionResponse), typeof(given.ReturnedOperation), true)]
    [InlineData(nameof(given.response_actions.NestedOperationResponse), typeof(given.ReturnedOperation), false)]
    [InlineData(nameof(given.response_actions.OrdinaryResponse), typeof(given.ClientResponse), false)]
    public void should_keep_the_controller_response_and_constructor(string method, Type responseType, bool enumerable)
    {
        var descriptor = _descriptors[method];
        descriptor.HasResponse.ShouldBeTrue();
        descriptor.ResponseType.Type.ShouldEqual(responseType);
        descriptor.ResponseType.IsEnumerable.ShouldEqual(enumerable);
        _proxies[method].ShouldContain($"extends Command<I{method}, {responseType.Name}{(enumerable ? "[]" : string.Empty)}>");
        _proxies[method].ShouldContain($"super({responseType.Name}, {enumerable.ToString().ToLowerInvariant()})");
    }
}
