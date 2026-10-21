// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.ModelBound;

namespace Cratis.Arc.ProxyGenerator.Specs.ModelBound.for_TypeExtensionsModelBound;

public class when_checking_if_internal_type_is_query : Specification
{
    bool _result;

    void Because() => _result = typeof(Scenarios.for_Queries.ModelBound.InternalReadModel).IsQuery();

    [Fact] void should_recognize_a_public_query_on_an_internal_type() => _result.ShouldBeTrue();
}
