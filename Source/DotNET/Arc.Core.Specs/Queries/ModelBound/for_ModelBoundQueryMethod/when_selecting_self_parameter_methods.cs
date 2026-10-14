// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryMethod;

public class when_selecting_self_parameter_methods : Specification
{
    MethodInfo[] _methods;

    void Because() => _methods = ModelBoundQueryMethod.GetMethods(typeof(given.SelfParameterModel), method => method.IsValidQueryFor(typeof(given.SelfParameterModel)));

    [Fact] void should_exclude_exact_self_parameters_in_every_position() => _methods.Select(_ => _.Name).ShouldContainOnly("Collection", "Other", "Service", "All");
    [Fact] void should_exclude_nullable_value_self_parameters() => typeof(given.SelfParameterValue).GetMethod("Fold")!.IsValidQueryFor(typeof(given.SelfParameterValue)).ShouldBeFalse();
    [Fact] void should_keep_value_queries_without_a_self_parameter() => typeof(given.SelfParameterValue).GetMethod("All")!.IsValidQueryFor(typeof(given.SelfParameterValue)).ShouldBeTrue();
}
