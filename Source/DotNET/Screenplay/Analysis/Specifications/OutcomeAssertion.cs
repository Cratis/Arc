// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// One call in an assertion body that may say what followed.
/// </summary>
/// <param name="Invocation">The call.</param>
/// <param name="Method">The method being called.</param>
/// <param name="Appended">The event type the assertion names, when it asserts an appended event.</param>
/// <param name="Rejection">Whether the assertion says the command was rejected.</param>
internal sealed record OutcomeAssertion(InvocationExpressionSyntax Invocation, IMethodSymbol Method, ITypeSymbol? Appended, bool Rejection);
