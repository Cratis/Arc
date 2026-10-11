// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias FakeContracts;

using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.Commands;
using Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;
using Cratis.Arc.Validation;
using Cratis.Concepts;
using Cratis.Monads;

using FakeAttributedValue = FakeContracts::Cratis.Arc.ProxyGenerator.Specs.FakeCommandResponseHandlerDependency.FakeAttributedValue;
using FakeHandledValue = FakeContracts::Cratis.Arc.ProxyGenerator.Specs.FakeCommandResponseHandlerDependency.FakeHandledValue;

namespace Cratis.Arc.ProxyGenerator.for_MethodInfoExtensions.when_getting_a_response_model.given;

public record ClientResponse(string Value);

public record ProjectId(Guid Value) : ConceptAs<Guid>(Value);

public static class command_methods
{
    public static ProjectRegistered ReturnsEvent() => new(string.Empty);

    public static (ProjectId Id, ProjectRegistered Event) ReturnsIdAndEvent() => default;

    public static (ProjectRegistered Event, ClientResponse Response) ReturnsEventAndResponse() => default;

    public static (ProjectId Id, DependencyHandledValue Wrapper) ReturnsIdAndEventWrapper() => default;

    public static (ProjectId Id, ProjectRegistered Event, CommandOperations Operations) ReturnsIdEventAndOperations() => default;

    public static IEnumerable<object> ReturnsObjectCollection() => [];

    public static ProjectRegistered[] ReturnsEventArray() => [];

    public static IEnumerable<ProjectRegistered> ReturnsEventEnumerable() => [];

    public static List<ProjectRegistered> ReturnsEventList() => [];

    public static InheritedProjectRegistered ReturnsInheritedEvent() => new(string.Empty);

    public static ScalarOnlyValue ReturnsScalarOnlyValue() => new(string.Empty);

    public static ScalarOnlyValue[] ReturnsScalarOnlyArray() => [];

    public static EnumerableOnlyValue ReturnsEnumerableOnlyValue() => new(string.Empty);

    public static IEnumerable<EnumerableOnlyValue> ReturnsEnumerableOnlyCollection() => [];

    public static ConsumedStruct[] ReturnsStructEventArray() => [];

    public static IEnumerable<ConsumedStruct> ReturnsStructEventEnumerable() => [];

    [SuppressMessage("Performance", "CA1814:Prefer jagged arrays over multidimensional", Justification = "Regression fixture for rectangular arrays, which must remain client responses.")]
    public static ProjectRegistered[,] ReturnsRectangularEventArray() => new ProjectRegistered[0, 0];

    public static NonInheritedEvent ReturnsDirectNonInheritedEvent() => new(string.Empty);

    public static DerivedNonInheritedEvent ReturnsDerivedNonInheritedEvent() => new(string.Empty);

    public static DerivedDefaultInheritedEvent ReturnsDefaultInheritedEvent() => new(string.Empty);

    public static Result<(ProjectRegistered Event, ClientResponse Response), ValidationResult> ReturnsEventTupleOrValidation() => null!;

    public static OneOf.OneOf<ProjectRegistered, ClientResponse> ReturnsEventOrResponse() => default;

    public static Result<ProjectRegistered, ValidationResult> ReturnsEventOrValidation() => null!;

    public static FakeAttributedValue ReturnsValueWithCounterfeitAttribute() => new();

    public static CounterfeitClaimedValue ReturnsValueClaimedByCounterfeitAttributeMarker() => new();

    public static CounterfeitClaimedValue[] ReturnsCollectionClaimedByCounterfeitAttributeMarker() => [];

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
