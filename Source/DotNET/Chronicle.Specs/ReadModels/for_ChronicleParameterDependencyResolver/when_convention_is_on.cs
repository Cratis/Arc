// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_convention_is_on : given.a_parameter_lookup
{
    object _result;
    void Establish() => Configure(new Convention(new("from")));
    void Because() => _result = CommandReadModelLookup.Resolve(_context, typeof(Convention).GetMethod(nameof(Convention.Provide))!.GetParameters()[0], ReadProtectionMode.On, ProtectedKeyTraversalForSpecs.Create(), out _);
    [Fact] void should_choose_the_property_of_the_model_key_type() => _result.ToString().ShouldEqual("from");
}
