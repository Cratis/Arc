// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

/// <summary>
/// Verifies discovery of chained parameters despite provided-value alternatives.
/// </summary>
public class when_discovering_injected_chained_parameters : given.a_chained_lookup
{
    ParameterInfo[] _parameters;
    void Because() => _parameters = InjectedHandleParameters.Of(typeof(Providing).GetMethod("Handle")!, typeof(Providing).GetMethod("Provide")).ToArray();
    [Fact] void should_not_treat_any_explicit_chain_argument_as_provided() => _parameters.Select(parameter => parameter.Name).ShouldEqual(["last", "middle", "first"]);
}
