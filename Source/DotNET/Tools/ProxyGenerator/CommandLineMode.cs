// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Owns comparison/report options and their stdout contract.
/// </summary>
/// <param name="arguments">Command-line arguments.</param>
internal sealed class CommandLineMode(string[] arguments)
{
    readonly string? _validationFormat = Value(arguments, "--validation-report");
    readonly string? _validationFile = Value(arguments, "--validation-report-file");

    /// <summary>
    /// Gets whether a non-mutating comparison was requested.
    /// </summary>
    public bool Check { get; } = arguments.Contains("--check", StringComparer.Ordinal);

    /// <summary>
    /// Gets whether validation projection reporting was requested.
    /// </summary>
    public bool ReportsValidation => _validationFormat is not null || _validationFile is not null;

    /// <summary>
    /// Validates the report option combination before generation starts.
    /// </summary>
    /// <param name="error">A safe usage error.</param>
    /// <returns>Whether options are valid.</returns>
    public bool TryValidate(out string error)
    {
        error = string.Empty;
        if (Check && ReportsValidation) error = "--check cannot be combined with --validation-report or --validation-report-file.";
        else if (_validationFormat is not null && !IsValidationFormat(_validationFormat)) error = "--validation-report must be text or json.";
        else if (Value(arguments, "--report-validation") is not null || Value(arguments, "--check-report") is not null) error = "Unknown report option. Use --validation-report=text|json.";
        else if (_validationFile is not null) return ValidationReportFile.TryValidate(_validationFile, arguments.ElementAtOrDefault(1), out error);
        return error.Length == 0;
    }

    /// <summary>
    /// Writes ordinary progress unless JSON stdout was requested.
    /// </summary>
    /// <param name="message">Progress message.</param>
    public void WriteLine(string message = "")
    {
        if (_validationFormat != "json") Console.WriteLine(message);
    }

    /// <summary>Writes an error or diagnostic to stderr.</summary>
    /// <param name="message">The already formatted diagnostic.</param>
    public void WriteError(string message) => Console.Error.WriteLine(message);

    /// <summary>
    /// Completes reporting after successful generation or comparison.
    /// </summary>
    /// <param name="output">Optional comparison sink.</param>
    /// <param name="validation">Optional validation report.</param>
    /// <returns>A task containing the clean or drift exit code.</returns>
    public async Task<int> Complete(CheckingGeneratedOutput? output, ValidationProjectionReport? validation)
    {
        if (output is not null)
        {
            await Console.Out.WriteLineAsync(output.Report.ToText());
            return output.Report.Ordered().Any() ? ProxyGeneratorExitCodes.Drift : ProxyGeneratorExitCodes.Clean;
        }
        if (validation is null) return ProxyGeneratorExitCodes.Clean;
        if (_validationFile is not null) await ValidationReportFile.Write(_validationFile, validation.ToJson());
        if (_validationFormat is not null) await Console.Out.WriteLineAsync(_validationFormat == "json" ? validation.ToJson() : validation.ToText());
        return ProxyGeneratorExitCodes.Clean;
    }

    static bool IsValidationFormat(string format) => format == "text" || format == "json";

    static string? Value(string[] arguments, string option)
    {
        var argument = arguments.LastOrDefault(_ => _ == option || _.StartsWith(option + "=", StringComparison.Ordinal));
        return argument is null ? null : argument == option ? string.Empty : argument[(option.Length + 1)..];
    }
}
