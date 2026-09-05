// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Core.Generators.Integration.Specs.Testing;

/// <summary>
/// Represents SDK-host code-fix discovery and application.
/// </summary>
/// <param name="DiagnosticId">The diagnostic identifier passed to <c>dotnet format analyzers</c>.</param>
/// <param name="SourceWasRewritten">Whether the expected code fix rewrote the source.</param>
/// <param name="HostReportedFormattedFile">Whether the SDK host reported formatting a file.</param>
public sealed record CodeFixHostResult(
    string DiagnosticId,
    bool SourceWasRewritten,
    bool HostReportedFormattedFile);
