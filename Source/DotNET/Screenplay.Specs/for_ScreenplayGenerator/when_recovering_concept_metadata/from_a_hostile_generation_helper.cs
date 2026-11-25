// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_recovering_concept_metadata;

public class from_a_hostile_generation_helper : a_generated_document
{
    [Theory]
    [InlineData("System.IO.File.ReadAllText(\"/etc/hosts\") + context.Tenant")]
    [InlineData("Settings.Prefix + context.Tenant")]
    public void should_leave_the_factory_in_code(string helperBody)
    {
        var source = $$"""
            using System;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Chronicle.Events;
            using Cratis.Concepts;
            namespace Library.Common;

            public static class Settings
            {
                public static string Prefix = "x";
            }

            public record OrderId(Guid Value) : ConceptAs<Guid>(Value);

            public record TenantOrderId(string Value) : ConceptAs<string>(Value), IGeneratableInContext<TenantOrderId>
            {
                public static TenantOrderId New(GenerationContext context) => new(Helper(context));
                static string Helper(GenerationContext context)
                {
                    return {{helperBody}};
                }
                public static implicit operator string(TenantOrderId value) => value.Value;
                public static implicit operator TenantOrderId(string value) => new(value);
            }

            [EventType] public record OrderPlaced(TenantOrderId Order);

            [Command] public record PlaceOrder([Cratis.Chronicle.Keys.Key] OrderId Id)
            {
                public OrderPlaced Handle([GeneratedValue("order")] TenantOrderId order) => new(order);
            }
            """;
        Analyzed.ErrorsIn((Analyzed.SlicePath, source)).ShouldBeEmpty();
        Generate((Analyzed.SlicePath, source));
        Result.Model.Concepts.Single(concept => concept.Name == "TenantOrderId").Generation.ShouldBeNull();
        Result.Source.ShouldNotContain("generate rule");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable).ShouldBeTrue();
    }
}
