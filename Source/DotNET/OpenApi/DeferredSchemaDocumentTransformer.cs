// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Cratis.Json;
using Cratis.Serialization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace Cratis.Arc.OpenApi;

/// <summary>
/// Fills converter-handled component schemas after ASP.NET has resolved component references.
/// </summary>
/// <param name="options">The configured OpenAPI documents.</param>
public class DeferredSchemaDocumentTransformer(IOptionsMonitor<OpenApiOptions> options) : IOpenApiDocumentTransformer
{
    /// <inheritdoc/>
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var pending = DeferredSchemas.For(document);
        var filled = new HashSet<Type>();
        var nullability = new NullabilityInfoContext();
        for (var index = 0; index < pending.Count; index++)
        {
            var (type, original, jsonOptions) = pending[index];
            var id = options.Get(context.DocumentName).CreateSchemaReferenceId(jsonOptions.GetTypeInfo(type));
            if (id is not null && !filled.Add(type))
            {
                continue;
            }
            var schema = id is not null && document.Components?.Schemas?.TryGetValue(id, out var component) == true && component is OpenApiSchema resolved ? resolved : original;
            if (jsonOptions.Converters.FirstOrDefault(converter => converter.CanConvert(type)) is DerivedTypeJsonConverterFactory)
            {
                schema.Properties ??= new Dictionary<string, IOpenApiSchema>();
                var properties = type.GetInterfaces().Append(type)
                    .SelectMany(candidate => candidate.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    .Where(property => property.GetMethod is not null && property.GetIndexParameters().Length == 0 && property.GetCustomAttribute<JsonIgnoreAttribute>()?.Condition is not JsonIgnoreCondition.Always);
                foreach (var property in properties)
                {
                    var name = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? jsonOptions.PropertyNamingPolicy?.ConvertName(property.Name) ?? property.Name;
                    var propertySchema = await GetSchema(property.PropertyType, document, context, jsonOptions, cancellationToken);
                    schema.Properties[name] = nullability.Create(property).ReadState == NullabilityState.Nullable && propertySchema is OpenApiSchemaReference
                        ? new OpenApiSchema { OneOf = [new OpenApiSchema { Type = JsonSchemaType.Null }, propertySchema] }
                        : propertySchema;
                }
            }
            else if (jsonOptions.Converters.FirstOrDefault(converter => converter.CanConvert(type)) is ComplexKeyDictionaryJsonConverterFactory)
            {
                var valueType = type.GetInterfaces().Append(type)
                    .First(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                    .GetGenericArguments()[1];
                schema.AdditionalProperties = await GetSchema(valueType, document, context, jsonOptions, cancellationToken);
            }
            else if (jsonOptions.GetTypeInfo(type) is { Kind: JsonTypeInfoKind.Enumerable, ElementType: { } elementType })
            {
                schema.Items = await GetSchema(elementType, document, context, jsonOptions, cancellationToken);
            }
        }
    }

    async Task<IOpenApiSchema> GetSchema(Type type, OpenApiDocument document, OpenApiDocumentTransformerContext context, System.Text.Json.JsonSerializerOptions jsonOptions, CancellationToken cancellationToken)
    {
        var id = options.Get(context.DocumentName).CreateSchemaReferenceId(jsonOptions.GetTypeInfo(type));
        if (id is not null)
        {
            if (document.Components?.Schemas?.ContainsKey(id) != true)
            {
                var schema = await context.GetOrCreateSchemaAsync(type, null, cancellationToken);
                document.Components ??= new OpenApiComponents();
                document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();
                document.Components.Schemas.TryAdd(id, schema);
                if (schema.Properties is not null)
                {
                    foreach (var property in jsonOptions.GetTypeInfo(type).Properties)
                    {
                        var propertyType = property.PropertyType;
                        var propertyInfo = jsonOptions.GetTypeInfo(propertyType);
                        var childType = propertyInfo.Kind == JsonTypeInfoKind.Enumerable ? propertyInfo.ElementType ?? propertyType : propertyType;
                        if (schema.Properties.ContainsKey(property.Name) && options.Get(context.DocumentName).CreateSchemaReferenceId(jsonOptions.GetTypeInfo(childType)) is not null)
                        {
                            var propertySchema = await GetSchema(propertyType, document, context, jsonOptions, cancellationToken);
                            schema.Properties[property.Name] = property.IsGetNullable && propertySchema is OpenApiSchemaReference
                                ? new OpenApiSchema { OneOf = [new OpenApiSchema { Type = JsonSchemaType.Null }, propertySchema] }
                                : propertySchema;
                        }
                    }
                }
            }
            return new OpenApiSchemaReference(id, document);
        }

        if (jsonOptions.GetTypeInfo(type) is { Kind: JsonTypeInfoKind.Enumerable, ElementType: { } elementType })
        {
            return new OpenApiSchema { Type = JsonSchemaType.Array, Items = await GetSchema(elementType, document, context, jsonOptions, cancellationToken) };
        }

        return await context.GetOrCreateSchemaAsync(type, null, cancellationToken);
    }
}
