// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Commands.Filters.for_DataAnnotationValidationFilter.given;

public class a_data_annotation_validation_filter : Specification
{
    protected DataAnnotationValidationFilter _filter;
    protected CommandContext _context;
    protected CorrelationId _correlationId;

    void Establish()
    {
        _correlationId = CorrelationId.New();
        _filter = new DataAnnotationValidationFilter();
    }
}
