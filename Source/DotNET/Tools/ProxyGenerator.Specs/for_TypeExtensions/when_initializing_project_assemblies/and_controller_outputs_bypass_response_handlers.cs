// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.ControllerBased.for_CommandExtensions.given;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_initializing_project_assemblies;

public class and_controller_outputs_bypass_response_handlers : Specification
{
    bool _initialized;
    List<string> _errors;
    Dictionary<string, (bool HasResponse, string Name, bool IsEnumerable, string Proxy)> _controllerResponses;
    Dictionary<string, (bool HasResponse, string Name, string Proxy)> _modelBoundResponses;

    void Establish() => _errors = [];

    void Because()
    {
        _initialized = TypeExtensions.TryInitializeProjectAssemblies(GetType().Assembly.Location, _ => { }, _errors.Add);
        using (TypeExtensions.OwnProjectAssemblies())
        {
            var assembly = TypeExtensions.Assemblies.Single(_ => _.FullName == GetType().Assembly.FullName);
            var controller = assembly.GetType(typeof(response_actions).FullName!)!;
            _controllerResponses = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .ToDictionary(_ => _.Name, method =>
                {
                    var descriptor = ControllerBased.CommandExtensions.ToCommandDescriptor(method, "/output", 0);
                    return (descriptor.HasResponse, descriptor.ResponseType.Name, descriptor.ResponseType.IsEnumerable, InMemoryProxyGenerator.GenerateCommand(descriptor));
                });
            var commands = assembly.GetType(typeof(response_commands).FullName!)!.GetNestedTypes().Select(_ => _.GetTypeInfo()).ToArray();
            _modelBoundResponses = commands.Where(_ => _.Name != nameof(response_commands.OperationCollectionResponse))
                .ToDictionary(_ => _.Name, command =>
                {
                    var descriptor = ModelBound.CommandExtensions.ToCommandDescriptor(command, "/output", 0, false, "api", commands);
                    return (descriptor.HasResponse, descriptor.ResponseType.Name, InMemoryProxyGenerator.GenerateCommand(descriptor));
                });
        }
    }

    [Fact] void should_initialize_the_real_metadata_graph() => Assert.True(_initialized, string.Join(Environment.NewLine, _errors));

    [Theory]
    [InlineData(nameof(response_actions.EventResponse), "ProjectRegistered", false)]
    [InlineData(nameof(response_actions.AsyncEventResponse), "ProjectRegistered", false)]
    [InlineData(nameof(response_actions.EventCollectionResponse), "ProjectRegistered", true)]
    [InlineData(nameof(response_actions.NestedTupleResponse), "ProjectRegistered", false)]
    [InlineData(nameof(response_actions.TupleInUnionResponse), "ProjectRegistered", false)]
    [InlineData(nameof(response_actions.UnionInTupleResponse), "ProjectRegistered", false)]
    [InlineData(nameof(response_actions.UnionResponse), "ProjectRegistered", false)]
    [InlineData(nameof(response_actions.CollectionInUnionResponse), "ProjectRegistered", true)]
    [InlineData(nameof(response_actions.TypedInUnionResponse), "DependencyHandledValue", false)]
    [InlineData(nameof(response_actions.TypedResponse), "DependencyHandledValue", false)]
    [InlineData(nameof(response_actions.OperationResponse), "ReturnedOperation", false)]
    [InlineData(nameof(response_actions.OperationCollectionResponse), "ReturnedOperation", true)]
    [InlineData(nameof(response_actions.NestedOperationResponse), "ReturnedOperation", false)]
    public void should_preserve_controller_values_in_metadata_descriptors_and_proxies(string method, string responseName, bool enumerable)
    {
        var response = _controllerResponses[method];
        response.HasResponse.ShouldBeTrue();
        response.Name.ShouldEqual(responseName);
        response.IsEnumerable.ShouldEqual(enumerable);
        response.Proxy.ShouldContain($"extends Command<I{method}, {responseName}{(enumerable ? "[]" : string.Empty)}>");
        response.Proxy.ShouldContain($"super({responseName}, {enumerable.ToString().ToLowerInvariant()})");
    }

    [Theory]
    [InlineData(nameof(response_commands.EventResponse))]
    [InlineData(nameof(response_commands.AsyncEventResponse))]
    [InlineData(nameof(response_commands.EventCollectionResponse))]
    [InlineData(nameof(response_commands.TypedResponse))]
    [InlineData(nameof(response_commands.OperationResponse))]
    public void should_still_suppress_model_bound_values_in_metadata_descriptors_and_proxies(string command)
    {
        _modelBoundResponses[command].HasResponse.ShouldBeFalse();
        _modelBoundResponses[command].Proxy.ShouldContain($"extends Command<I{command}>");
    }

    [Theory]
    [InlineData(nameof(response_commands.CollectionInUnionResponse))]
    [InlineData(nameof(response_commands.TypedInUnionResponse))]
    [InlineData(nameof(response_commands.NestedTupleResponse))]
    [InlineData(nameof(response_commands.TupleInUnionResponse))]
    [InlineData(nameof(response_commands.UnionInTupleResponse))]
    [InlineData(nameof(response_commands.UnionResponse))]
    [InlineData(nameof(response_commands.NestedOperationResponse))]
    public void should_still_select_the_model_bound_client_value_from_nested_metadata_shapes(string command)
    {
        _modelBoundResponses[command].HasResponse.ShouldBeTrue();
        _modelBoundResponses[command].Name.ShouldEqual("ClientResponse");
        _modelBoundResponses[command].Proxy.ShouldContain($"extends Command<I{command}, ClientResponse>");
    }
}
