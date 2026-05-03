// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_checking_if_type_is_excluded;

[Collection(AssemblyPackageMappingCollectionDefinition.Name)]
public class and_type_name_matches_exactly : Specification
{
    class TypeToExclude
    {
        public string Name { get; set; } = string.Empty;
    }

    bool _result;

    void Establish() =>
        TypeExtensions.SetExcludedTypes([typeof(TypeToExclude).FullName!], []);

    void Because() => _result = typeof(TypeToExclude).IsExcluded();

    void Destroy() => TypeExtensions.SetExcludedTypes([], []);

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
