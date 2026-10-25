// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Concepts;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

public enum DictionaryValueStatus
{
    Active = 0,
    Inactive = 1
}

public record DictionaryValueCode(string Value) : ConceptAs<string>(Value);

public record DictionaryValueIdentifier(Guid Value) : ConceptAs<Guid>(Value);

[ReadModel]
public record DictionaryDependencyReadModel
{
    public static DictionaryDependencyReadModel CountDictionaryValues(
        IDictionary<string, DictionaryValueStatus> statuses,
        Dictionary<string, DictionaryValueCode> codes,
        IReadOnlyDictionary<string, Guid> identifiers,
        IDictionary<string, DictionaryValueIdentifier> conceptIdentifiers) =>
        new();

    public static DictionaryDependencyReadModel CountNullableDictionaryValues(
        IDictionary<string, int?> numbers,
        IReadOnlyDictionary<string, DictionaryValueStatus?> nullableStatuses,
        Dictionary<string, Guid?> nullableIdentifiers) =>
        new();
}
