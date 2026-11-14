// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelParser.given;

public class a_document_with_an_unpopulated_field : Specification
{
    // joinedName intentionally has no projection assignment, exercising the real PLAY0284 diagnostic.
    protected const string Source =
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
        """;
}
