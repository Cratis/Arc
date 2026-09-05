// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.from_projects_in_separate_namespace_roots;

/// <summary>
/// The source of a solution holding two applications that share nothing, the way a samples or a monorepo solution
/// really is arranged.
/// </summary>
/// <remarks>
/// The layered source is two projects of one application, so every namespace in it begins with the same segment and
/// the question of what the modules are never arises. Here the two do not: each names itself, and a document that
/// gathered both under one module named after the solution would say the solution is the application and that the
/// two applications are features of it.
/// </remarks>
public static class SeparateRootsSource
{
    const string Ordering = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Ordering.Placing;

        /// <summary>
        /// An order was placed.
        /// </summary>
        [EventType]
        public record OrderPlaced(string Reference);

        /// <summary>
        /// Places an order.
        /// </summary>
        [Command]
        public record PlaceOrder(string Reference)
        {
            public OrderPlaced Handle() => new(Reference);
        }
        """;

    const string Onboarding = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Quickstart.Users.Onboarding;

        /// <summary>
        /// A user was onboarded.
        /// </summary>
        [EventType]
        public record UserOnboarded(string Name);

        /// <summary>
        /// Onboards a user.
        /// </summary>
        [Command]
        public record OnboardUser(string Name)
        {
            public UserOnboarded Handle() => new(Name);
        }
        """;

    /// <summary>
    /// Builds the project of the first application.
    /// </summary>
    /// <returns>The <see cref="Compilation"/>.</returns>
    public static Compilation LibraryProject() =>
        Analyzed.Project(
            "Library",
            [],
            ("Source/Library/Program.cs", "namespace Library;"),
            ("Source/Library/Ordering/Placing/Placing.cs", Ordering));

    /// <summary>
    /// Builds the project of the second application, which shares no namespace with the first.
    /// </summary>
    /// <returns>The <see cref="Compilation"/>.</returns>
    public static Compilation QuickstartProject() =>
        Analyzed.Project(
            "Quickstart",
            [],
            ("Source/Quickstart/Program.cs", "namespace Quickstart;"),
            ("Source/Quickstart/Users/Onboarding/Onboarding.cs", Onboarding));
}
