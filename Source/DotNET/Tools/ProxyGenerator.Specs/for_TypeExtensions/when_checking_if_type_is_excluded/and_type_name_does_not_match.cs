// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_checking_if_type_is_excluded;

[Collection(AssemblyPackageMappingCollectionDefinition.Name)]
public class and_type_name_does_not_match : Specification
{
    class SomeType
    {
        public string Name { get; set; } = string.Empty;
    }

    bool _result;

    void Establish() =>
        TypeExtensions.SetExcludedTypes(["SomeOtherNamespace.SomeOtherType"], []);

    void Because() => _result = typeof(SomeType).IsExcluded();

    void Destroy() => TypeExtensions.SetExcludedTypes([], []);

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}
