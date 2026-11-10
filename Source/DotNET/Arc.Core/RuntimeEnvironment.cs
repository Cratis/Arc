// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Execution;

/// <summary>
/// Represents helpers for getting details about the Runtime environment.
/// </summary>
public static class RuntimeEnvironment
{
    /// <summary>
    /// Gets whether or not we're running in development or not.
    /// </summary>
    public static readonly bool IsDevelopment;

    static RuntimeEnvironment()
    {
        IsDevelopment = IsDevelopmentIn(
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"));
    }

    /// <summary>
    /// Determines whether the environment names select the Development environment.
    /// </summary>
    /// <param name="aspNetCoreEnvironment">The value of <c>ASPNETCORE_ENVIRONMENT</c>.</param>
    /// <param name="dotNetEnvironment">The value of <c>DOTNET_ENVIRONMENT</c>.</param>
    /// <returns>True when Development is selected; <c>ASPNETCORE_ENVIRONMENT</c> wins when both are set, as it does for WebApplication.</returns>
    internal static bool IsDevelopmentIn(string? aspNetCoreEnvironment, string? dotNetEnvironment) =>
        (string.IsNullOrEmpty(aspNetCoreEnvironment) ? dotNetEnvironment : aspNetCoreEnvironment) == "Development";
}
