// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using Cratis.Json;

namespace Cratis.Arc;

/// <summary>
/// Limits Fundamentals' complex-key dictionary converter to dictionaries keyed by concepts on plain minimal APIs.
/// </summary>
internal class ConceptKeyDictionaryJsonConverterFactory : ComplexKeyDictionaryJsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) =>
        base.CanConvert(typeToConvert) && typeToConvert.GetInterfaces().Append(typeToConvert)
            .Any(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IDictionary<,>) && candidate.GetGenericArguments()[0].IsConcept());
}
