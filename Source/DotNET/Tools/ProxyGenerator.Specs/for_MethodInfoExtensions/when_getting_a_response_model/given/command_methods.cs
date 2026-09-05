// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias FakeContracts;

using Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;
using Cratis.Arc.Validation;
using Cratis.Monads;
using FakeHandledValue = FakeContracts::Cratis.Arc.ProxyGenerator.Specs.FakeCommandResponseHandlerDependency.FakeHandledValue;

namespace Cratis.Arc.ProxyGenerator.for_MethodInfoExtensions.when_getting_a_response_model.given;

public record ClientResponse(string Value);

public static class command_methods
{
    public static DependencyHandledValue ReturnsServerHandledValue() => null!;

    public static Result<DependencyHandledValue, ValidationResult> ReturnsServerHandledValueOrValidationResult() => null!;

    public static ClientResponse ReturnsClientResponse() => new(string.Empty);

    public static IReadOnlyList<ClientResponse> ReturnsClientResponses() => [];

    public static DependencyHandledValue[] ReturnsHandledValues() => [];

    public static MarkerOnlyValue ReturnsMarkerOnlyValue() => new();

    public static FakeHandledValue ReturnsValueClaimedByCounterfeitContracts() => new();

    public static (DependencyHandledValue Handled, ClientResponse Client) ReturnsHandledAndClientTuple() => default;

    public static (DependencyHandledValue First, DependencyHandledValue Second) ReturnsAllHandledTuple() => default;

    public static Result<(DependencyHandledValue Handled, ClientResponse Client), ValidationResult> ReturnsWrappedHandledAndClientTuple() => null!;
}
