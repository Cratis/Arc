// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Arc.Validation;
using FluentValidation;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

/// <summary>
/// Stands in for a generated resource accessor: the value depends on ambient state, which is the whole reason the
/// message is declared as a factory instead of a literal.
/// </summary>
public static class AmbientMessages
{
    /// <summary>
    /// The message the build machine resolves, and the one that must never be baked into a generated artifact.
    /// </summary>
    public const string NeutralCulture = "A value is required";

    /// <summary>
    /// The message a request in a different culture must be able to resolve for itself.
    /// </summary>
    public const string NorwegianCulture = "Feltet ma fylles ut";

    /// <summary>
    /// Gets the message for whichever culture is current when it is asked - which at generation time is the build
    /// machine's, and at request time is the user's.
    /// </summary>
    public static string ForCurrentCulture =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "nb" ? NorwegianCulture : NeutralCulture;
}

/// <summary>
/// Carries one rule messaged eagerly and one messaged through a factory, so a spec can tell a fix that stops
/// resolving factories apart from one that simply stopped projecting messages.
/// </summary>
public class TypeWithDeferredMessage
{
    /// <summary>
    /// Gets or sets a property whose rule takes its message from a context-free literal.
    /// </summary>
    public string EagerlyMessaged { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a property whose rule defers its message to request time.
    /// </summary>
    public string DeferredMessaged { get; set; } = string.Empty;
}

/// <summary>
/// Validator for <see cref="TypeWithDeferredMessage"/>.
/// </summary>
public class TypeWithDeferredMessageValidator : BaseValidator<TypeWithDeferredMessage>
{
    /// <summary>
    /// The literal message, which is context-free and may be projected.
    /// </summary>
    public const string EagerMessage = "This one is a literal";

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeWithDeferredMessageValidator"/> class.
    /// </summary>
    public TypeWithDeferredMessageValidator()
    {
        RuleFor(x => x.EagerlyMessaged).NotEmpty().WithMessage(EagerMessage);
        RuleFor(x => x.DeferredMessaged).NotEmpty().WithMessage(_ => AmbientMessages.ForCurrentCulture);
    }
}
