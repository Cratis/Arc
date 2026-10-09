// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a table a screen shows, read from a Cratis Components data table bound to a query of its slice.
/// </summary>
/// <param name="ReadModel">The read model the rows are, as the query the table is bound to returns it.</param>
/// <param name="Columns">The columns the table shows, in the order the component declares them.</param>
/// <remarks>
/// The table is named after the read model rather than after anything in the component, because the query it is
/// bound to is the one part of it the model can vouch for. The columns are the fields the component names, and a
/// column whose field is not written as text is left out rather than guessed.
/// </remarks>
public record ScreenTableModel(string ReadModel, IEnumerable<ScreenColumnModel> Columns);
