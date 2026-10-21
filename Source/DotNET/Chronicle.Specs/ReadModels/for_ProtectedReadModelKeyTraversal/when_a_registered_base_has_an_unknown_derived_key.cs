// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ProtectedReadModelKeyTraversal;

public class when_a_registered_base_has_an_unknown_derived_key : given.a_traversal
{
    static when_a_registered_base_has_an_unknown_derived_key() => ProtectedReadModelKeyAccessors.Register<BaseKey>([new(nameof(BaseKey.Part), key => key.Part)]);
    Exception _error;
    void Establish() => _options.Strict = true;
    void Because() => _error = Catch.Exception(() => _traversal.IsValid(new DerivedKey("valid", "*"), "wire"));
    [Fact] void should_require_the_exact_runtime_descriptor() => _error.ShouldBeOfExactType<ProtectedReadModelKeyAccessorMissing>();
    record BaseKey(string Part);
    record DerivedKey(string Part, string Extra) : BaseKey(Part);
}
