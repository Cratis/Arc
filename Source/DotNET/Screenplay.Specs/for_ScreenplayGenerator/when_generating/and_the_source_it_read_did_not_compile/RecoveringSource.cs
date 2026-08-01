// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating.and_the_source_it_read_did_not_compile;

/// <summary>
/// Holds source that does not compile and yet gives up everything it declares, which is what a host handing over a
/// compilation assembled without the compile items a build generates really produces.
/// </summary>
/// <remarks>
/// The unresolved name sits in a helper class that declares no artifact, so the errors are real while the command and
/// the event are read from declarations the compiler accepted - the shape the whole recalibration turns on.
/// </remarks>
public static class RecoveringSource
{
    /// <summary>
    /// The slice, which compiles on its own.
    /// </summary>
    public const string Slice = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Events;

        namespace Library.Authors.Registration;

        [EventType]
        public record AuthorRegistered(string Name);

        [Command]
        public record RegisterAuthor(string Name)
        {
            public AuthorRegistered Handle() => new(Name);
        }
        """;

    /// <summary>
    /// A helper reaching for the strongly typed resource class a build would have generated.
    /// </summary>
    public const string Wording = """
        namespace Library.Authors;

        public static class Wording
        {
            public static string NameIsRequired() => AuthorsMessages.NameIsRequired;
        }
        """;

    /// <summary>
    /// Gets the source files, keyed by the path each one is compiled as.
    /// </summary>
    /// <returns>The source files.</returns>
    public static (string Path, string Text)[] Files() =>
    [
        ("Library/Authors/Registration/Registration.cs", Slice),
        ("Library/Authors/Wording.cs", Wording)
    ];
}
