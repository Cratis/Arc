// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;

namespace Company.Library.Accounts.SignIn;

/// <summary>
/// Represents signing in to the fixture application.
/// </summary>
/// <param name="Name">The account name.</param>
[Command]
public record SignIn(string Name)
{
    /// <summary>
    /// Returns the accepted account name.
    /// </summary>
    /// <returns>The name.</returns>
    public string Handle() => Name;
}
