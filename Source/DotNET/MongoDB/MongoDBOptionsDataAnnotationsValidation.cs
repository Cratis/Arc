// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Validates the data annotations on MongoDB options without runtime reflection.
/// </summary>
[OptionsValidator]
internal partial class MongoDBOptionsDataAnnotationsValidation : IValidateOptions<MongoDBOptions>;
