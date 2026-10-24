// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Concepts;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.TestTypes.DictionaryParameters;

public record PartName(string Value) : ConceptAs<string>(Value);

[ReadModel]
public record DictionaryParameterReadModel
{
    public static DictionaryParameterReadModel Get(IDictionary<string, string> parts, IServiceProvider dependency) => new();
    public static DictionaryParameterReadModel GetOptional(IReadOnlyDictionary<string, string>? parts) => new();
    public static DictionaryParameterReadModel GetConcepts(Dictionary<string, PartName> parts) => new();
    public static DictionaryParameterReadModel GetComplex(IDictionary<string, ComplexValue> parts) => new();
    public static DictionaryParameterReadModel GetNumericKeys(IDictionary<int, string> parts) => new();
}

public record ComplexValue(string Name);
