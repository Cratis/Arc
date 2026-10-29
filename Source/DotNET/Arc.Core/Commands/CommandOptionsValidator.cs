// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Commands;

/// <summary>
/// Validates the caller-supplied command severity configuration on host startup.
/// </summary>
public class CommandOptionsValidator : IValidateOptions<ArcOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, ArcOptions options) =>
        options.Command.MaximumCallerAllowedSeverity is < ValidationResultSeverity.Unknown or > ValidationResultSeverity.Error
            ? ValidateOptionsResult.Fail("Cratis:Arc:Command:MaximumCallerAllowedSeverity must be between Unknown (0) and Error (3).")
            : ValidateOptionsResult.Success;
}
