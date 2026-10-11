// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.ProxyGenerator.Templates;
using Cratis.Concepts;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_CommandExtensions;

public class when_converting_a_command_with_a_generated_value : Specification
{
    CommandDescriptor _descriptor;

    void Because() => _descriptor = typeof(CreateCart).GetTypeInfo().ToCommandDescriptor(
        "/output", segmentsToSkip: 5, skipCommandNameInRoute: false, apiPrefix: "api", [typeof(CreateCart).GetTypeInfo()]);

    [Fact] void should_include_only_command_properties_in_the_request() => _descriptor.Properties.Select(property => property.Name).ShouldContainOnly(nameof(CreateCart.Name));
    [Fact] void should_not_include_the_generated_parameter_in_the_request() => _descriptor.Parameters.ShouldBeEmpty();

    public record CartId(Guid Value) : ConceptAs<Guid>(Value), IGeneratable<CartId>
    {
        public static CartId New() => new(GenerateValue.Uuid());
    }

    [Command]
    public record CreateCart(string Name)
    {
        public void Handle([GeneratedValue] CartId cartId) { }
    }
}
