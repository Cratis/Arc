// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_TypeExtensionsModelBound;

public class when_generating_a_proxy_for_a_derived_collection : Specification
{
    QueryDescriptor[] _descriptors;

    void Because()
    {
        var readModel = typeof(QueryCandidateReadModel).GetTypeInfo();
        _descriptors = readModel.ToQueryDescriptors("/output", 0, false, "api", [readModel]).ToArray();
    }

    [Fact] void should_generate_a_proxy_for_the_derived_collection() => _descriptors.Any(_ => _.Name == nameof(QueryCandidateReadModel.Derived)).ShouldBeTrue();
    [Fact] void should_not_generate_a_proxy_for_a_string() => _descriptors.Any(_ => _.Name == nameof(QueryCandidateReadModel.WrongScalar)).ShouldBeFalse();
    [Fact] void should_not_generate_a_proxy_for_an_unrelated_collection() => _descriptors.Any(_ => _.Name == nameof(QueryCandidateReadModel.WrongCollection)).ShouldBeFalse();
}
