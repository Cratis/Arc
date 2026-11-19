// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The binding table of <see cref="JsonSerializerOptions"/>, in the metadata order of System.Text.Json 10, which is the
/// version every target framework resolves. The type is get-only on <see cref="ArcOptions"/>, so the existing instance is
/// bound and never created.
/// </summary>
internal static class JsonSerializerOptionsBinding
{
    /// <summary>
    /// Gets how <see cref="JsonSerializerOptions"/> is bound.
    /// </summary>
    internal static readonly ObjectType Bound = new(typeof(JsonSerializerOptions), static () => new JsonSerializerOptions(), Members);

    static Member[] Members() =>
    [
        Member.Of<JsonSerializerOptions, IList<JsonConverter>>("Converters", new ListType<JsonConverter>(new AbstractType(typeof(JsonConverter))), static _ => _.Converters),
        Member.Of<JsonSerializerOptions, IJsonTypeInfoResolver?>("TypeInfoResolver", new AbstractType(typeof(IJsonTypeInfoResolver)), static _ => _.TypeInfoResolver, static (o, v) => o.TypeInfoResolver = v),
        Member.Of<JsonSerializerOptions, IList<IJsonTypeInfoResolver>>("TypeInfoResolverChain", new ListType<IJsonTypeInfoResolver>(new AbstractType(typeof(IJsonTypeInfoResolver))), static _ => _.TypeInfoResolverChain),
        Member.Of<JsonSerializerOptions, bool>("AllowOutOfOrderMetadataProperties", Leaves.Bool, static _ => _.AllowOutOfOrderMetadataProperties, static (o, v) => o.AllowOutOfOrderMetadataProperties = v),
        Member.Of<JsonSerializerOptions, bool>("AllowTrailingCommas", Leaves.Bool, static _ => _.AllowTrailingCommas, static (o, v) => o.AllowTrailingCommas = v),
        Member.Of<JsonSerializerOptions, int>("DefaultBufferSize", Leaves.Int, static _ => _.DefaultBufferSize, static (o, v) => o.DefaultBufferSize = v),
        Member.Of<JsonSerializerOptions, JavaScriptEncoder?>("Encoder", new AbstractType(typeof(JavaScriptEncoder)), static _ => _.Encoder, static (o, v) => o.Encoder = v),
        Member.Of<JsonSerializerOptions, JsonNamingPolicy?>("DictionaryKeyPolicy", new AbstractType(typeof(JsonNamingPolicy)), static _ => _.DictionaryKeyPolicy, static (o, v) => o.DictionaryKeyPolicy = v),
#pragma warning disable CS0618, SYSLIB0020 // The obsolete setting is still bound from configuration, as the reflection binder does.
        Member.Of<JsonSerializerOptions, bool>("IgnoreNullValues", Leaves.Bool, static _ => _.IgnoreNullValues, static (o, v) => o.IgnoreNullValues = v),
#pragma warning restore CS0618, SYSLIB0020
        Member.Of<JsonSerializerOptions, JsonIgnoreCondition>("DefaultIgnoreCondition", Leaves.JsonIgnoreCondition, static _ => _.DefaultIgnoreCondition, static (o, v) => o.DefaultIgnoreCondition = v),
        Member.Of<JsonSerializerOptions, JsonNumberHandling>("NumberHandling", Leaves.JsonNumberHandling, static _ => _.NumberHandling, static (o, v) => o.NumberHandling = v),
        Member.Of<JsonSerializerOptions, JsonObjectCreationHandling>("PreferredObjectCreationHandling", Leaves.JsonObjectCreationHandling, static _ => _.PreferredObjectCreationHandling, static (o, v) => o.PreferredObjectCreationHandling = v),
        Member.Of<JsonSerializerOptions, bool>("IgnoreReadOnlyProperties", Leaves.Bool, static _ => _.IgnoreReadOnlyProperties, static (o, v) => o.IgnoreReadOnlyProperties = v),
        Member.Of<JsonSerializerOptions, bool>("IgnoreReadOnlyFields", Leaves.Bool, static _ => _.IgnoreReadOnlyFields, static (o, v) => o.IgnoreReadOnlyFields = v),
        Member.Of<JsonSerializerOptions, bool>("IncludeFields", Leaves.Bool, static _ => _.IncludeFields, static (o, v) => o.IncludeFields = v),
        Member.Of<JsonSerializerOptions, int>("MaxDepth", Leaves.Int, static _ => _.MaxDepth, static (o, v) => o.MaxDepth = v),
        Member.Of<JsonSerializerOptions, JsonNamingPolicy?>("PropertyNamingPolicy", new AbstractType(typeof(JsonNamingPolicy)), static _ => _.PropertyNamingPolicy, static (o, v) => o.PropertyNamingPolicy = v),
        Member.Of<JsonSerializerOptions, bool>("PropertyNameCaseInsensitive", Leaves.Bool, static _ => _.PropertyNameCaseInsensitive, static (o, v) => o.PropertyNameCaseInsensitive = v),
        Member.Of<JsonSerializerOptions, JsonCommentHandling>("ReadCommentHandling", Leaves.JsonCommentHandling, static _ => _.ReadCommentHandling, static (o, v) => o.ReadCommentHandling = v),
        Member.Of<JsonSerializerOptions, JsonUnknownTypeHandling>("UnknownTypeHandling", Leaves.JsonUnknownTypeHandling, static _ => _.UnknownTypeHandling, static (o, v) => o.UnknownTypeHandling = v),
        Member.Of<JsonSerializerOptions, JsonUnmappedMemberHandling>("UnmappedMemberHandling", Leaves.JsonUnmappedMemberHandling, static _ => _.UnmappedMemberHandling, static (o, v) => o.UnmappedMemberHandling = v),
        Member.Of<JsonSerializerOptions, bool>("WriteIndented", Leaves.Bool, static _ => _.WriteIndented, static (o, v) => o.WriteIndented = v),
        Member.Of<JsonSerializerOptions, char>("IndentCharacter", Leaves.Char, static _ => _.IndentCharacter, static (o, v) => o.IndentCharacter = v),
        Member.Of<JsonSerializerOptions, int>("IndentSize", Leaves.Int, static _ => _.IndentSize, static (o, v) => o.IndentSize = v),
        Member.Of<JsonSerializerOptions, ReferenceHandler?>("ReferenceHandler", new AbstractType(typeof(ReferenceHandler)), static _ => _.ReferenceHandler, static (o, v) => o.ReferenceHandler = v),
        Member.Of<JsonSerializerOptions, string>("NewLine", Leaves.String, static _ => _.NewLine, static (o, v) => o.NewLine = v),
        Member.Of<JsonSerializerOptions, bool>("RespectNullableAnnotations", Leaves.Bool, static _ => _.RespectNullableAnnotations, static (o, v) => o.RespectNullableAnnotations = v),
        Member.Of<JsonSerializerOptions, bool>("RespectRequiredConstructorParameters", Leaves.Bool, static _ => _.RespectRequiredConstructorParameters, static (o, v) => o.RespectRequiredConstructorParameters = v),
        Member.Of<JsonSerializerOptions, bool>("AllowDuplicateProperties", Leaves.Bool, static _ => _.AllowDuplicateProperties, static (o, v) => o.AllowDuplicateProperties = v),
#if NET11_0_OR_GREATER
        Member.Of<JsonSerializerOptions, IList<JsonTypeClassifierFactory>>("TypeClassifiers", new ListType<JsonTypeClassifierFactory>(new AbstractType(typeof(JsonTypeClassifierFactory))), static _ => _.TypeClassifiers),
        Member.Of<JsonSerializerOptions, bool>("InferClosedTypePolymorphism", Leaves.Bool, static _ => _.InferClosedTypePolymorphism, static (o, v) => o.InferClosedTypePolymorphism = v),
#endif
        Member.Of<JsonSerializerOptions, bool>("IsReadOnly", Leaves.Bool, static _ => _.IsReadOnly)
    ];
}
