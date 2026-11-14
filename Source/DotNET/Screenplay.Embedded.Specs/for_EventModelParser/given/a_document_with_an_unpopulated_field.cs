// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelParser.given;

[SuppressMessage("Usage", "MA0136:Raw String contains an implicit end of line character", Justification = "The authored document is explicitly normalized to LF before it is parsed.")]
public class a_document_with_an_unpopulated_field : Specification
{
    /// <summary>
    /// Gets the authored document whose unassigned joinedName field exercises the real PLAY0284 diagnostic.
    /// </summary>
    protected static string Source { get; } =
        """
        type Row
          rowId Uuid
          joinedName String
        module Records
          feature Entries
            slice StateView Browse
              event RowAdded
                rowId Uuid
              readmodel EntryView
                rows Row[]
              projection Entries => EntryView
                children rows identified by rowId
                  no automap
                  from RowAdded
                    rowId = rowId
        """.ReplaceLineEndings("\n");
}
