// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// The source of a library application whose commands name authorization policies and govern their changes through
/// an aggregate root, written the way an Arc application really is written.
/// </summary>
public static class PolicyAndAggregateSource
{
    /// <summary>
    /// The part of the authorization framework a registration is written against.
    /// </summary>
    public const string Framework = """
        using System;

        namespace Microsoft.AspNetCore.Authorization;

        public class AuthorizationPolicyBuilder
        {
            public AuthorizationPolicyBuilder RequireRole(params string[] roles) => this;

            public AuthorizationPolicyBuilder RequireClaim(string claimType, params string[] allowedValues) => this;
        }

        public class AuthorizationOptions
        {
            public void AddPolicy(string name, Action<AuthorizationPolicyBuilder> configurePolicy)
            {
            }
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
            public static void Authorization(AuthorizationOptions options)
            {
                options.AddPolicy("CanReserve", policy => policy.RequireRole("Librarian").RequireClaim("branch", "central"));
                options.AddPolicy("SeniorStaff", policy => policy.RequireRole("Librarian", "Archivist"));
            }
        }
        """;

    /// <summary>
    /// The slice reserving a copy of a book through the aggregate root governing the reservation.
    /// </summary>
    public const string Reserving = """
        using System.Threading.Tasks;
        using Cratis.Arc.Authorization;
        using Cratis.Arc.Chronicle.Aggregates;
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using FluentValidation;

        namespace Library.Lending.Reserving;

        [EventType]
        public record BookReserved(string Isbn, string Member);

        public class Reservation : AggregateRoot
        {
            public Task Reserve(string isbn, string member) => Apply(new BookReserved(isbn, member));

            public void OnBookReserved(BookReserved @event)
            {
            }
        }

        /// <summary>
        /// Reserves a copy of a book for a member.
        /// </summary>
        [Command]
        [Authorize(Policy = "CanReserve")]
        public record ReserveBook(string Isbn, string MemberId)
        {
            public async Task Handle(Reservation reservation)
            {
                await reservation.Reserve(Isbn, MemberId);
                await reservation.Commit();
            }
        }

        public class ReserveBookValidator : CommandValidator<ReserveBook>
        {
            public ReserveBookValidator()
            {
                RuleFor(_ => _.Isbn).Length(10, 13).WithMessage("An ISBN is between 10 and 13 characters");
            }
        }
        """;

    /// <summary>
    /// The slice writing off a lost copy, which only senior staff may do.
    /// </summary>
    public const string WritingOff = """
        using Cratis.Arc.Authorization;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Lending.WritingOff;

        [EventType]
        public record CopyWrittenOff(string Isbn);

        [Command]
        [Authorize(Policy = "SeniorStaff", Roles = "Librarian")]
        public record WriteOffCopy(string Isbn)
        {
            public CopyWrittenOff Handle() => new(Isbn);
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
        ("Library/Lending/Reserving/Reserving.cs", Reserving),
        ("Library/Lending/WritingOff/WritingOff.cs", WritingOff)
    ];
}
