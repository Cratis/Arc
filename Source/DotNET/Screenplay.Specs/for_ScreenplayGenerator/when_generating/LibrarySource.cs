// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// The source of a small library application, written the way an Arc application really is written.
/// </summary>
/// <remarks>
/// Every discovery path the end to end specification exercises is here rather than in a fixture built by hand, so
/// that what is asserted is what the generator does to source rather than what it does to a model someone wrote.
/// </remarks>
public static class LibrarySource
{
    /// <summary>
    /// The slice registering an author.
    /// </summary>
    public const string AuthorRegistration = """
        using Cratis.Arc.Authorization;
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle;
        using Cratis.Chronicle.Compliance.GDPR;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Events.Constraints;
        using Cratis.Concepts;
        using FluentValidation;

        namespace Library.Authors.Registration;

        [PII("The name of a person")]
        public record AuthorName(string Value) : ConceptAs<string>(Value);

        /// <summary>
        /// An author was registered in the library.
        /// </summary>
        [EventType]
        [Tag("audit")]
        public record AuthorRegistered([property: Unique("UniqueAuthorName")] AuthorName Name);

        /// <summary>
        /// Registers a new author.
        /// </summary>
        [Command]
        [Roles("Librarian")]
        public record RegisterAuthor(AuthorName Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }

        public class RegisterAuthorValidator : CommandValidator<RegisterAuthor>
        {
            public RegisterAuthorValidator()
            {
                RuleFor(_ => _.Name).NotEmpty().WithMessage("An author must have a name");
                RuleFor(_ => _.Name).MaximumLength(200);
            }
        }
        """;

    /// <summary>
    /// The slice listing the registered authors.
    /// </summary>
    public const string AuthorListing = """
        using System.Collections.Generic;
        using Cratis.Arc.Queries.ModelBound;
        using Cratis.Chronicle.Projections;
        using Library.Authors.Registration;

        namespace Library.Authors.Listing;

        [ReadModel]
        public record Author
        {
            public string Id { get; init; } = string.Empty;

            public AuthorName Name { get; init; } = new(string.Empty);

            public static IEnumerable<Author> AllAuthors() => [];

            public static Author AuthorById(string id) => new();
        }

        public class AuthorProjection : IProjectionFor<Author>
        {
            public void Define(IProjectionBuilderFor<Author> builder) => builder
                .AutoMap()
                .From<AuthorRegistered>(_ => _
                    .Set(m => m.Id).ToEventSourceId()
                    .Set(m => m.Name).To(e => e.Name));
        }
        """;

    /// <summary>
    /// The slice reserving a copy of a book.
    /// </summary>
    public const string Reserving = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;
        using Cratis.Concepts;

        namespace Library.Lending.Reserving;

        public record Isbn(string Value) : ConceptAs<string>(Value);

        [EventType]
        public record BookReserved(Isbn Isbn);

        [EventType]
        public record ReservationRefused(Isbn Isbn);

        /// <summary>
        /// Reserves a copy of a book for a member.
        /// </summary>
        [Command]
        public record ReserveBook(Isbn Isbn, bool InStock)
        {
            public object Handle()
            {
                if (InStock)
                {
                    return new BookReserved(Isbn);
                }

                return new ReservationRefused(Isbn);
            }
        }
        """;

    /// <summary>
    /// The slice notifying a member that their reservation is ready.
    /// </summary>
    public const string Notifications = """
        using System.Threading.Tasks;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Reactors;
        using Library.Lending.Reserving;

        namespace Library.Lending.Notifications;

        public class ReservationNotifier : IReactor
        {
            public Task BookReserved(BookReserved @event, EventContext context) => Task.CompletedTask;
        }
        """;
}
