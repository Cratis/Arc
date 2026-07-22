// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_CommandExtensions;

/// <summary>
/// A model-bound command's Handle takes the value its Provide() resolved, not the command. When that value carries a
/// validated concept and the command itself has no client-projectable rule, the descriptor must still be empty - the
/// value's rules belong to the value, never to the command.
/// </summary>
public class when_converting_a_command_whose_handle_consumes_a_provided_value : Specification
{
    CommandDescriptor _result;

    void Because() => _result = typeof(TestTypes.Invitations.InviteContact).GetTypeInfo().ToCommandDescriptor(
        "/output",
        segmentsToSkip: 5,
        skipCommandNameInRoute: false,
        apiPrefix: "api",
        [typeof(TestTypes.Invitations.InviteContact).GetTypeInfo()]);

    [Fact] void should_not_carry_the_provided_values_rules() => _result.ValidationRules.ShouldBeEmpty();
    [Fact] void should_not_declare_it_has_validation_rules() => _result.HasValidationRules.ShouldBeFalse();
}
