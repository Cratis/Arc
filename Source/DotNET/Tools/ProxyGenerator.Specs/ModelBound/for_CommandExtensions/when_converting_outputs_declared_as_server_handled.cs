// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.ControllerBased.for_CommandExtensions.given;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_CommandExtensions;

public class when_converting_outputs_declared_as_server_handled : Specification
{
    Dictionary<string, CommandDescriptor> _descriptors;
    Dictionary<string, string> _proxies;
    Exception _bareCollectionError;

    void Because()
    {
        var types = typeof(response_commands).GetNestedTypes().Select(_ => _.GetTypeInfo()).ToArray();
        _descriptors = types.Where(_ => _.Name != nameof(response_commands.OperationCollectionResponse))
            .ToDictionary(_ => _.Name, _ => _.ToCommandDescriptor("/output", 0, false, "api", types));
        _proxies = _descriptors.ToDictionary(_ => _.Key, _ => InMemoryProxyGenerator.GenerateCommand(_.Value));
        _bareCollectionError = Catch.Exception(() => typeof(response_commands.OperationCollectionResponse).GetTypeInfo()
            .ToCommandDescriptor("/output", 0, false, "api", types));
    }

    [Theory]
    [InlineData(nameof(response_commands.EventResponse))]
    [InlineData(nameof(response_commands.AsyncEventResponse))]
    [InlineData(nameof(response_commands.EventCollectionResponse))]
    [InlineData(nameof(response_commands.TypedResponse))]
    [InlineData(nameof(response_commands.OperationResponse))]
    public void should_exclude_consumed_model_bound_values(string command)
    {
        _descriptors[command].HasResponse.ShouldBeFalse();
        _proxies[command].ShouldContain($"extends Command<I{command}>");
        _proxies[command].ShouldContain("super(Object, false)");
    }

    [Theory]
    [InlineData(nameof(response_commands.CollectionInUnionResponse))]
    [InlineData(nameof(response_commands.TypedInUnionResponse))]
    [InlineData(nameof(response_commands.NestedTupleResponse))]
    [InlineData(nameof(response_commands.TupleInUnionResponse))]
    [InlineData(nameof(response_commands.UnionInTupleResponse))]
    [InlineData(nameof(response_commands.UnionResponse))]
    [InlineData(nameof(response_commands.NestedOperationResponse))]
    [InlineData(nameof(response_commands.OrdinaryResponse))]
    public void should_select_the_unconsumed_model_bound_response(string command)
    {
        _descriptors[command].HasResponse.ShouldBeTrue();
        _descriptors[command].ResponseType.Type.ShouldEqual(typeof(ClientResponse));
        _proxies[command].ShouldContain($"extends Command<I{command}, ClientResponse>");
        _proxies[command].ShouldContain("super(ClientResponse, false)");
    }

    [Fact] void should_still_reject_bare_model_bound_operation_collections() => _bareCollectionError.ShouldBeOfExactType<UnsupportedCommandOperationCollection>();
}
