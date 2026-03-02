// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeScriptContentCombiner.when_combining;

public class with_empty_collection : Specification
{
    string _result = null!;

    void Because() => _result = TypeScriptContentCombiner.Combine([]);

    [Fact] void should_return_empty_string() => _result.ShouldEqual(string.Empty);
}
