// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Calendar;

/// <summary>
/// Represents an implementation of <see cref="IValidateOptions{TOptions}"/> that refuses an application time zone the
/// host cannot resolve.
/// </summary>
/// <remarks>
/// It throws <see cref="TimeZoneIsNotAnIanaZone"/> while the host starts, like the tenancy validator, rather than returning a failed
/// result that would be collected into a generic options exception.
/// </remarks>
public class CalendarOptionsValidator : IValidateOptions<ArcOptions>
{
    /// <summary>
    /// Validates the calendar configuration.
    /// </summary>
    /// <param name="name">The name of the options instance being validated.</param>
    /// <param name="options">The <see cref="ArcOptions"/> to validate.</param>
    /// <returns>The <see cref="ValidateOptionsResult"/> of the validation.</returns>
    /// <exception cref="TimeZoneIsNotAnIanaZone">Thrown when the configured time zone cannot be used.</exception>
    public ValidateOptionsResult Validate(string? name, ArcOptions options)
    {
        ApplicationTimeZones.Resolve(options.Calendar.TimeZone);
        return ValidateOptionsResult.Success;
    }
}
