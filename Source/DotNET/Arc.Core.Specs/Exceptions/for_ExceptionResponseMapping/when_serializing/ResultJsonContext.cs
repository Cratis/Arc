// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;

namespace Cratis.Arc.Exceptions.for_ExceptionResponseMapping.when_serializing;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CommandResult))]
[JsonSerializable(typeof(CommandResult<string>))]
[JsonSerializable(typeof(QueryResult))]
internal partial class ResultJsonContext : JsonSerializerContext;
