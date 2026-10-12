// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Proves command-context destinations, including direct generated-identity wrappers.
/// </summary>
/// <param name="model">The model resolving the handler body.</param>
/// <param name="body">The handler or aggregate behavior body.</param>
/// <param name="sources">The proven generated mapping sources.</param>
internal class ReturnedEventDestinations(SemanticModel model, SyntaxNode body, AuthoringSources? sources)
{
    /// <summary>
    /// Determines whether an event construction uses the command's own destination.
    /// </summary>
    /// <param name="creation">The event construction.</param>
    /// <param name="aggregate">Whether the body belongs to a reached aggregate.</param>
    /// <param name="generatedIdentity">Whether a generated command identity was proven.</param>
    /// <returns>Whether the event reaches the command context.</returns>
    public bool Read(BaseObjectCreationExpressionSyntax creation, bool aggregate, bool generatedIdentity)
    {
        if (ProductionDestinations.ThroughCommandContext(creation, body, model, aggregate, generatedIdentity))
        {
            return true;
        }

        return generatedIdentity && ThroughGeneratedWrapper(creation);
    }

    bool ThroughGeneratedWrapper(BaseObjectCreationExpressionSyntax creation)
    {
        if (creation.Ancestors().OfType<BaseObjectCreationExpressionSyntax>().FirstOrDefault() is not { ArgumentList.Arguments: [var destination, var payload] } wrapper)
        {
            return false;
        }
        if (!model.GetTypeInfo(wrapper).Type.Is("Cratis.Chronicle.EventSequences.EventForEventSourceId"))
        {
            return false;
        }
        if (sources?.ReadPath(destination.Expression, model) is null || MappingSourceReader.Unwrap(payload.Expression) != creation)
        {
            return false;
        }

        return IsDirectReturn(wrapper);
    }

    bool IsDirectReturn(BaseObjectCreationExpressionSyntax wrapper) => wrapper.Parent switch
    {
        ReturnStatementSyntax statement => ReferenceEquals(wrapper, body) || ReferenceEquals(statement.Parent, body),
        _ => ReferenceEquals(wrapper, body)
    };
}
