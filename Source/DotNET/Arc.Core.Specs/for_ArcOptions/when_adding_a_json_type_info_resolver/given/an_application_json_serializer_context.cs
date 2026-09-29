// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Arc.for_JsonSerializerOptionsConfiguration.given;

namespace Cratis.Arc.for_ArcOptions.when_adding_a_json_type_info_resolver.given;

/// <summary>
/// The source-generated context an application would ship for the types it returns from its queries and commands.
/// </summary>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(OrderReadModel))]
[JsonSerializable(typeof(OrderReadModel[]))]
[JsonSerializable(typeof(OrderNumber))]
[JsonSerializable(typeof(OrderLine))]
public partial class an_application_json_serializer_context : JsonSerializerContext;
