// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// A session fold did not reach its probe; retrying the command can acquire a complete read.
/// </summary>
public class ReadIncomplete : Exception, IValidationFailure
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReadIncomplete"/> class.
    /// </summary>
    /// <param name="model">The read model.</param>
    /// <param name="key">The requested key, intentionally excluded from both messages.</param>
    public ReadIncomplete(Type model, ReadModelKey key)
        : base($"{model.Name} could not be read completely. Nothing was written; submit it again.")
    {
        _ = key;
    }

    /// <inheritdoc/>
    public ValidationResult ValidationResult => ValidationResult.Error(Message, reason: ValidationResultReason.ConcurrencyViolation, reasonDetail: "ReadIncomplete");
}
