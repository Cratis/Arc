// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// A session fold did not reach its probe; retrying the command can acquire a complete read.
/// </summary>
/// <param name="model">The read model.</param>
/// <param name="key">The requested key.</param>
public class ReadIncomplete(Type model, ReadModelKey key)
    : Exception($"{model.Name} '{key}' could not be read completely. Nothing was written; submit it again."), IValidationFailure
{
    /// <inheritdoc/>
    public ValidationResult ValidationResult => ValidationResult.Error(Message, reason: ValidationResultReason.ConcurrencyViolation, reasonDetail: "ReadIncomplete");
}
