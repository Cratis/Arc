// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.ModelBound;

namespace Cratis.Arc.ProxyGenerator.Specs.ModelBound.for_TypeExtensionsModelBound;

public class when_checking_if_type_with_internal_query_method_is_query : Specification
{
    bool _result;

    void Because() => _result = typeof(Scenarios.for_Queries.ModelBound.ReadModelWithInternalQuery).IsQuery();

    [Fact] void should_recognize_query_with_internal_method() => _result.ShouldBeTrue();
}
