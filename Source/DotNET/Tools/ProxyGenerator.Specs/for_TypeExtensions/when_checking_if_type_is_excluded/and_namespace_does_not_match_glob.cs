// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_checking_if_type_is_excluded;

[Collection(AssemblyPackageMappingCollectionDefinition.Name)]
public class and_namespace_does_not_match_glob : Specification
{
    class TypeInNonMatchingNamespace
    {
        public string Name { get; set; } = string.Empty;
    }

    bool _result;

    void Establish() =>
        TypeExtensions.SetExcludedTypes([], ["Some.Completely.Different.Namespace*"]);

    void Because() => _result = typeof(TypeInNonMatchingNamespace).IsExcluded();

    void Destroy() => TypeExtensions.SetExcludedTypes([], []);

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}
