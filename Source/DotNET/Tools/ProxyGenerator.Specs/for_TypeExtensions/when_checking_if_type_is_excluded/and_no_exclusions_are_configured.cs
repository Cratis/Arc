// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_checking_if_type_is_excluded;

[Collection(AssemblyPackageMappingCollectionDefinition.Name)]
public class and_no_exclusions_are_configured : Specification
{
    class AnyType
    {
        public string Name { get; set; } = string.Empty;
    }

    bool _result;

    void Establish() => TypeExtensions.SetExcludedTypes([], []);

    void Because() => _result = typeof(AnyType).IsExcluded();

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}
