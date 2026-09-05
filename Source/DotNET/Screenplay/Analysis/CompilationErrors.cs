// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Reports source that did not compile, at the severity what was recovered from it earns.
/// </summary>
/// <remarks>
/// A compilation that does not build still yields symbols, and analyzing them can produce a document that looks like
/// an answer while describing an application that does not exist - which is why this is reported at all. What it is
/// not is a single outcome. A host that hands over a compilation assembled without the compile items a build
/// generates leaves every reference to a strongly typed resource class unresolved, and hundreds of errors then sit
/// in source that declares no artifact at all while every command, event and reactor is read exactly as written.
/// Calling that an error says something untrue about thousands of correct lines and makes the host discard them.
/// <para>
/// So the severity follows one number: how many artifacts were recovered from a declaration no compilation error
/// sits inside. When that number is zero - because nothing was recovered, or because every declaration something was
/// recovered from is one the compiler could not make sense of - nothing in the document is worth trusting and this is
/// an error, which is what makes a host exit non zero. Otherwise it is a warning stating how much was recovered
/// anyway, because the artifacts read from source the compiler accepted are described exactly as that source states
/// them and a document holding them is worth having.
/// </para>
/// <para>
/// A count is used rather than a proportion deliberately. Any threshold - a tenth, a half - would make the same
/// recovery pass for a large application and fail for a small one, and no number is defensible. Zero is the only one
/// that means recovery was prevented rather than merely dented.
/// </para>
/// </remarks>
public static class CompilationErrors
{
    /// <summary>
    /// Reports the errors a compilation carries, if it carries any.
    /// </summary>
    /// <param name="compilation">The compilation that was analyzed.</param>
    /// <param name="recovered">What was recovered from it, and where each of it was written.</param>
    /// <param name="diagnostics">The diagnostics to report to.</param>
    /// <returns>True when the source did not compile, whatever was recovered from it.</returns>
    public static bool Report(Compilation compilation, RecoveredArtifacts recovered, ScreenplayDiagnostics diagnostics)
    {
        var errors = compilation.GetDiagnostics().Where(_ => _.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Count == 0)
        {
            return false;
        }

        var first = errors[0].GetMessage(CultureInfo.InvariantCulture);
        var accepted = recovered.RecoveredFromAcceptedSource(errors);

        if (accepted == 0)
        {
            diagnostics.Error(
                ScreenplayDiagnosticCodes.SourceDidNotCompile,
                PreventedRecovery(errors.Count, first, recovered.Count),
                compilation.AssemblyName);
        }
        else
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.SourceDidNotCompile,
                SurvivedRecovery(errors.Count, first, recovered.Count, accepted),
                compilation.AssemblyName);
        }

        return true;
    }

    /// <summary>
    /// Says that the errors left nothing behind worth trusting.
    /// </summary>
    /// <param name="errors">The number of errors the compiler reported.</param>
    /// <param name="first">The first thing the compiler said.</param>
    /// <param name="recovered">The number of artifacts recovered in all.</param>
    /// <returns>The message.</returns>
    static string PreventedRecovery(int errors, string first, int recovered) =>
        recovered == 0
            ? $"The source did not compile - {errors} error(s), the first being '{first}'. Nothing at all was recovered from it, so nothing in the document describes the application reliably"
            : $"The source did not compile - {errors} error(s), the first being '{first}'. {recovered} artifact(s) were recovered, and every declaration they were read from is one an error sits inside, so nothing recovered describes the application reliably";

    /// <summary>
    /// Says how much came through the errors intact.
    /// </summary>
    /// <param name="errors">The number of errors the compiler reported.</param>
    /// <param name="first">The first thing the compiler said.</param>
    /// <param name="recovered">The number of artifacts recovered in all.</param>
    /// <param name="accepted">The number of them read from a declaration no error sits inside.</param>
    /// <returns>The message.</returns>
    static string SurvivedRecovery(int errors, string first, int recovered, int accepted) =>
        $"The source did not compile - {errors} error(s), the first being '{first}'. {recovered} artifact(s) were recovered anyway, {accepted} of them from a declaration no error sits inside, so the document describes those exactly as the source states them - a missing type named like 'SomethingMessages' or a designer class usually means the compilation was handed over without the compile items a build generates";
}
