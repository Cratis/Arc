// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

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
        if (!commandResult.IsAuthorized)
        {
            response.StatusCode = (int)HttpStatusCode.Forbidden;            // Forbidden: https://www.rfc-editor.org/rfc/rfc9110.html#name-403-forbidden
        }
        else if (!commandResult.IsValid)
        {
            response.StatusCode = (int)HttpStatusCode.BadRequest;           // Bad request: https://www.rfc-editor.org/rfc/rfc9110.html#name-400-bad-request
        }
        else if (commandResult.HasExceptions)
        {
            response.StatusCode = (int)HttpStatusCode.InternalServerError;  // Internal Server error: https://www.rfc-editor.org/rfc/rfc9110.html#name-500-internal-server-error
        }
    }
}
