// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Types;

/// <summary>
/// Represents a record an artifact carries that no <c>type</c> declaration could be written for.
/// </summary>
/// <param name="Type">The full name of the record.</param>
/// <param name="Reason">Why the shape could not be declared, written to follow "because".</param>
/// <remarks>
/// A shape being left out is worth far more to a reader than the fact of it: a record carrying nothing the document
/// can name and a record whose name a concept already took are two different gaps, and only one of them is closed by
/// renaming something.
/// </remarks>
public record UndeclarableShape(string Type, string Reason);
