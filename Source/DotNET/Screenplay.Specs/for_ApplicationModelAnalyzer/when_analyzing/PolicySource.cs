// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ApplicationModelAnalyzer.when_analyzing;

/// <summary>
/// The source of an application whose commands name authorization policies rather than roles.
/// </summary>
public static class PolicySource
{
    /// <summary>
    /// The part of the authorization framework a registration is written against.
    /// </summary>
    public const string Framework = """
        using System;

        namespace Microsoft.AspNetCore.Authorization;

        public class AuthorizationPolicyBuilder
        {
            public AuthorizationPolicyBuilder RequireAuthenticatedUser() => this;

            public AuthorizationPolicyBuilder RequireRole(params string[] roles) => this;

            public AuthorizationPolicyBuilder RequireClaim(string claimType, params string[] allowedValues) => this;

            public AuthorizationPolicyBuilder RequireAssertion(Func<object, bool> handler) => this;
        }

        public class AuthorizationOptions
        {
            public void AddPolicy(string name, Action<AuthorizationPolicyBuilder> configurePolicy)
            {
            }
        }

        public class AuthorizationBuilder
        {
            public AuthorizationBuilder AddPolicy(string name, Action<AuthorizationPolicyBuilder> configurePolicy) => this;
        }
        """;

    /// <summary>
    /// Where the application says what each of its policies means.
    /// </summary>
    public const string Composition = """
        using Microsoft.AspNetCore.Authorization;

        namespace Library;

        public static class Composition
        {
            public static void Authorization(AuthorizationOptions options, AuthorizationBuilder builder)
            {
                options.AddPolicy("CanReserve", policy => policy.RequireRole("Librarian").RequireClaim("branch", "central"));
                options.AddPolicy("Trusted", policy => policy.RequireAssertion(caller => true));
                builder.AddPolicy("SeniorStaff", policy => policy.RequireRole("Librarian", "Archivist"));
            }
        }
        """;

    /// <summary>
    /// The slice whose commands name the policies.
    /// </summary>
    public const string Slice = """
        using Cratis.Arc.Authorization;
        using Cratis.Arc.Commands.ModelBound;

        namespace Library.Lending.Reserving;

        [Command]
        [Authorize(Policy = "CanReserve")]
        public record ReserveBook(string Isbn)
        {
            public void Handle()
            {
            }
        }

        [Command]
        [Authorize(Policy = "SeniorStaff", Roles = "Librarian")]
        public record ForceReturn(string Isbn)
        {
            public void Handle()
            {
            }
        }

        [Command]
        [Authorize(Policy = "Trusted")]
        public record OverrideLoan(string Isbn)
        {
            public void Handle()
            {
            }
        }

        [Command]
        [Authorize(Policy = "Unregistered")]
        public record AuditLoan(string Isbn)
        {
            public void Handle()
            {
            }
        }
        """;

    /// <summary>
    /// Gets every source file of the application, keyed by the path each one is compiled as.
    /// </summary>
    /// <returns>The source files.</returns>
    public static (string Path, string Text)[] All() =>
    [
        ("Library/Composition.cs", Composition),
        ("Library/Authorization.cs", Framework),
        ("Library/Lending/Reserving/Reserving.cs", Slice)
    ];
}
