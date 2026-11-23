// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Cratis.Arc.Exceptions.for_ExceptionResponseMapping.when_resolving;

public class TheDiscoveredMapper : IExceptionResponseMapper
{
    public bool TryMap(Exception exception, [NotNullWhen(true)] out ExceptionResponse? response)
    {
        response = exception is TheMappedFailure ? new ExceptionResponse(HttpStatusCode.Conflict, "discovered_failure") : null;
        return response is not null;
    }
}
