// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>Owns comparison/report options and their stdout contract.</summary>
/// <param name="arguments">Command-line arguments.</param>
internal sealed class CommandLineMode(string[] arguments)
{
    readonly string _checkFormat = Value(arguments, "--check-report") ?? "text";
    readonly string? _validationFormat = Value(arguments, "--report-validation");

    /// <summary>Gets whether a non-mutating comparison was requested.</summary>
    public bool Check { get; } = arguments.Contains("--check", StringComparer.Ordinal);

    /// <summary>Gets whether validation projection reporting was requested.</summary>
    public bool ReportsValidation => _validationFormat is not null;

    bool HasInvalidFormat => !IsFormat(_checkFormat) || (_validationFormat is not null && !IsFormat(_validationFormat));
    bool RequiresCheck => !Check && Value(arguments, "--check-report") is not null;
    bool HasMixedFormats => Check && ReportsValidation && _checkFormat != _validationFormat;

    /// <summary>Validates the report option combination.</summary>
    /// <param name="error">A safe usage error.</param>
    /// <returns>Whether options are valid.</returns>
    public bool TryValidate(out string error)
    {
        error = string.Empty;
        if (HasInvalidFormat) error = "Report formats must be text or json.";
        else if (RequiresCheck) error = "--check-report requires --check.";
        else if (HasMixedFormats) error = "Combined reports require matching --check-report and --report-validation formats.";
        return error.Length == 0;
    }

    /// <summary>Writes ordinary progress only in text generation mode.</summary>
    /// <param name="message">Progress message.</param>
    public void WriteLine(string message = "")
    {
        if (!Check && _validationFormat != "json") Console.WriteLine(message);
    }

    /// <summary>Completes reporting after successful generation or comparison.</summary>
    /// <param name="output">Optional comparison sink.</param>
    /// <param name="validation">Optional validation report.</param>
    /// <returns>The clean or drift exit code.</returns>
    public int Complete(CheckingGeneratedOutput? output, ValidationProjectionReport? validation)
    {
        if (output is null)
        {
            WriteValidation(validation);
            return ProxyGeneratorExitCodes.Clean;
        }
        WriteComparison(output.Report, validation);
        return output.Report.Ordered().Any() ? ProxyGeneratorExitCodes.Drift : ProxyGeneratorExitCodes.Clean;
    }

    void WriteComparison(DriftReport drift, ValidationProjectionReport? validation)
    {
        if (_checkFormat == "json")
        {
            Console.WriteLine((validation ?? new()).ToCombinedJson(drift));
            return;
        }
        Console.WriteLine(drift.ToText());
        WriteValidation(validation);
    }

    void WriteValidation(ValidationProjectionReport? validation)
    {
        if (validation is null) return;
        Console.WriteLine(_validationFormat == "json" ? validation.ToJson() : validation.ToText());
    }

    static bool IsFormat(string value) => value is "text" or "json";

    static string? Value(string[] arguments, string option)
    {
        var argument = arguments.LastOrDefault(_ => _ == option || _.StartsWith(option + "=", StringComparison.Ordinal));
        return argument is null ? null : argument == option ? "text" : argument[(option.Length + 1)..];
    }
}
