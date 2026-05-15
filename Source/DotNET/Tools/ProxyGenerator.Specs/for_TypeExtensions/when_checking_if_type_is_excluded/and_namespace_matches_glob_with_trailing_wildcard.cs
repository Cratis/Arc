// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_checking_if_type_is_excluded;

[Collection(AssemblyPackageMappingCollectionDefinition.Name)]
public class and_namespace_matches_glob_with_trailing_wildcard : Specification
{
    class TypeInMatchingNamespace;

    bool _result;

    void Establish()
    {
        var ns = typeof(TypeInMatchingNamespace).Namespace ?? string.Empty;
        TypeExtensions.SetExcludedTypes([], [$"{ns}*"]);
    }

    void Because() => _result = typeof(TypeInMatchingNamespace).IsExcluded();

    void Destroy() => TypeExtensions.SetExcludedTypes([], []);

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
