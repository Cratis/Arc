// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;

namespace TestApps.Features.AuthenticationQueries;

/// <summary>
/// Represents a sample read model used to verify anonymous and authenticated query behavior.
/// </summary>
/// <param name="Message">The message payload including the last update timestamp.</param>
[ReadModel]
[Authorize]
public record AuthenticationQueryItem(string Message)
{
    static readonly BehaviorSubject<AuthenticationQueryItem> _anonymous =
        new(new AuthenticationQueryItem($"Anonymous stream — {DateTimeOffset.UtcNow:HH:mm:ss}"));

    static readonly BehaviorSubject<AuthenticationQueryItem> _authenticated =
        new(new AuthenticationQueryItem($"Authenticated stream — {DateTimeOffset.UtcNow:HH:mm:ss}"));

    static AuthenticationQueryItem()
    {
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
                var now = DateTimeOffset.UtcNow;

                _anonymous.OnNext(new AuthenticationQueryItem($"Anonymous stream — {now:HH:mm:ss}"));
                _authenticated.OnNext(new AuthenticationQueryItem($"Authenticated stream — {now:HH:mm:ss}"));
            }
        });
    }

    /// <summary>
    /// Gets the anonymous query stream used for login and pre-auth screens.
    /// </summary>
    /// <returns>An observable stream that allows anonymous access.</returns>
    [AllowAnonymous]
    public static ISubject<AuthenticationQueryItem> Anonymous() => _anonymous;

    /// <summary>
    /// Gets the authenticated query stream.
    /// </summary>
    /// <returns>An observable stream that requires authentication.</returns>
    public static ISubject<AuthenticationQueryItem> Authenticated() => _authenticated;
}
