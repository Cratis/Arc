// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Exceptions;
using Cratis.Arc.Http;

namespace Cratis.Arc.Commands;

/// <summary>
/// Provides extension methods for handling command results and Http responses.
/// </summary>
public static class CommandHttpResponseExtensions
{
    /// <summary>
    /// Sets the proper status code on the HTTP response based on the state of the <see cref="CommandResult"/>.
    /// </summary>
    /// <param name="response"><see cref="HttpResponse"/> to set status code on.</param>
    /// <param name="commandResult"><see cref="CommandResult"/> to evaluate.</param>
    public static void SetResponseStatusCode(this HttpResponse response, CommandResult commandResult)
    {
        var mapping = ExceptionResponseMapping.ResponseFor(commandResult);
        if (mapping is not null && response.HasStarted) return;
        if (HasHttpFailure(commandResult))
        {
            response.StatusCode = (int)EndpointRouteHelper.GetStatusCode(commandResult);
        }
        if (mapping is not null)
        {
            foreach (var header in mapping.Headers) response.Headers[header.Key] = header.Value;
        }
    }

    static bool HasHttpFailure(CommandResult result) => !result.IsAuthorized || !result.IsValid || result.IsRetryable || result.HasExceptions;
}
