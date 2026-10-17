// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Core.Generators.Integration.Specs.Testing;

/// <summary>
/// A package consumer that exercises generated concept metadata through the command validation filter.
/// </summary>
internal static class DataAnnotationConsumer
{
    /// <summary>
    /// The output produced only after both validation outcomes match expectations.
    /// </summary>
    internal const string Success = "concept annotations validated";

    /// <summary>
    /// The executable consumer source compiled against the packed Arc.Core package.
    /// </summary>
    internal const string Source = """
        using System;
        using System.ComponentModel.DataAnnotations;
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.Filters;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Concepts;
        using Cratis.Execution;

        var filter = new DataAnnotationValidationFilter();
        var invalid = new Submit(new Label("too long"), new Count(6));
        var rejected = await filter.OnExecution(new CommandContext(CorrelationId.New(), typeof(Submit), invalid, [], new()));
        var valid = new Submit(new Label("ok"), new Count(3));
        var accepted = await filter.OnExecution(new CommandContext(CorrelationId.New(), typeof(Submit), valid, [], new()));
        if (rejected.IsValid || !accepted.IsValid)
        {
            return 1;
        }

        Console.WriteLine("concept annotations validated");
        return 0;

        public record Label(string Value) : ConceptAs<string>(Value);
        public record Count(int Value) : ConceptAs<int>(Value);
        [Command]
        public partial record Submit([property: Required, StringLength(3)] Label Name, [property: Range(1, 5)] Count Copies)
        {
            public void Handle() { }
        }
        """;
}
