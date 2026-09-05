// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.CodeFixVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandDataAnnotationsKeyAnalyzer, Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes.UseChronicleKeyAttributeCodeFixProvider>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_UseChronicleKeyAttributeCodeFixProvider;

/// <summary>
/// The attribute is written out in full rather than reached through a new using: the file already has one for the data
/// annotations namespace, and with both in scope a bare [Key] is ambiguous.
/// </summary>
public class when_rewriting_the_key_attribute
{
    const string Source = @"
using System;
using System.ComponentModel.DataAnnotations;
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    public record CustomerRenamed(string Name);

    [Command]
    public record RenameCustomer([{|#0:property: Key|}] Guid CustomerId, string NewName)
    {
        public CustomerRenamed Handle() => new(NewName);
    }
}";

    const string FixedSource = @"
using System;
using System.ComponentModel.DataAnnotations;
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    public record CustomerRenamed(string Name);

    [Command]
    public record RenameCustomer([property: Cratis.Chronicle.Keys.Key] Guid CustomerId, string NewName)
    {
        public CustomerRenamed Handle() => new(NewName);
    }
}";

    [Fact] async Task should_rewrite_to_the_chronicle_attribute() => await VerifyCS.VerifyCodeFixAsync(
        Source,
        FixedSource,
        new ExpectedDiagnostic("ARCCHR0008", DiagnosticSeverity.Warning, "RenameCustomer", "CustomerId"));
}
