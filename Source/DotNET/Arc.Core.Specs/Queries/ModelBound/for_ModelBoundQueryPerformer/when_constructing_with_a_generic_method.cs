// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer;

/// <summary>
/// Discovery is not the only way a performer gets built, so the performer itself refuses a method it could never
/// invoke - at wire-up, where the offending method is named, rather than per request with a bare reflection message.
/// </summary>
public class when_constructing_with_a_generic_method : Specification
{
    MethodInfo _method;
    ModelBoundQueryPerformer _performer;
    Exception _exception;

    void Establish() => _method = typeof(ReadModelWithGenericHelper)
        .GetMethod(nameof(ReadModelWithGenericHelper.CountOf), BindingFlags.NonPublic | BindingFlags.Static)!;

    void Because() => _exception = Catch.Exception(() => _performer = new ModelBoundQueryPerformer(
        typeof(ReadModelWithGenericHelper),
        typeof(ReadModelWithGenericHelper).FullName!,
        _method,
        Substitute.For<IServiceProviderIsService>(),
        Substitute.For<IAuthorizationEvaluator>()));

    [Fact] void should_refuse_the_method() => _exception.ShouldBeOfExactType<QueryMethodCannotBeGeneric>();
    [Fact] void should_name_the_offending_method() => ((QueryMethodCannotBeGeneric)_exception).Method.ShouldEqual(_method);
}
