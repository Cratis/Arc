// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Generators.Specs.for_ModelGraphWalkerGenerator;

public class when_filtering_reflection_for_composite_keys : Specification
{
    const string Models = """
        namespace Models;
        public class Shape
        {
            public string Visible { get; } = "valid";
            public string Hidden { private get; set; } = "*";
            public string this[int index] => "*";
        }
        """;

    string[] _ordinary;
    string[] _keys;

    void Because()
    {
        var (_, _, compilation) = ModelGraphWalkerCompilation.Compile("PrivateGetterShape", Models, false);
        var type = ModelGraphWalkerCompilation.Load(compilation).GetType("Models.Shape")!;
        _ordinary = ModelGraphValidator.GetWalkablePropertiesThroughReflection(type).Select(member => member.Name).ToArray();
        _keys = ModelGraphValidator.GetWalkablePropertiesThroughReflection(type, publicGettersOnly: true).Select(member => member.Name).ToArray();
    }

    [Fact] void should_preserve_ordinary_validation_getter_visibility() => _ordinary.ShouldContain("hidden");
    [Fact] void should_only_include_public_getters_for_keys() => _keys.ShouldContainOnly("visible");
}
