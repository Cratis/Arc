// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Applies generated data annotation validation only to the default MongoDB options.
/// </summary>
internal sealed class DefaultMongoDBOptionsValidation : IValidateOptions<MongoDBOptions>
{
    static readonly MongoDBOptionsDataAnnotationsValidation _validator = new();

    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, MongoDBOptions options) =>
        name is not null && name != Options.DefaultName
            ? ValidateOptionsResult.Skip
            : _validator.Validate(name, options);
}
