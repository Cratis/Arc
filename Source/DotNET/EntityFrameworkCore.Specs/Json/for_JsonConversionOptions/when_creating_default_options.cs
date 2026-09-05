// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Json;

namespace Cratis.Arc.EntityFrameworkCore.Json.for_JsonConversionOptions;

public class when_creating_default_options : Specification
{
    JsonConversionOptions _options;

    void Because() => _options = new JsonConversionOptions();

    [Fact] void should_not_write_indented() => _options.JsonSerializerOptions.WriteIndented.ShouldBeFalse();
    [Fact] void should_include_concept_as_converter() => _options.JsonSerializerOptions.Converters.ShouldContain(c => c is ConceptAsJsonConverterFactory);
    [Fact] void should_include_enumerable_concept_as_converter() => _options.JsonSerializerOptions.Converters.ShouldContain(c => c is EnumerableConceptAsJsonConverterFactory);
    [Fact] void should_include_enum_converter() => _options.JsonSerializerOptions.Converters.ShouldContain(c => c is EnumConverterFactory);
    [Fact] void should_include_date_only_converter() => _options.JsonSerializerOptions.Converters.ShouldContain(c => c is DateOnlyJsonConverter);
    [Fact] void should_include_time_only_converter() => _options.JsonSerializerOptions.Converters.ShouldContain(c => c is TimeOnlyJsonConverter);
    [Fact] void should_include_type_converter() => _options.JsonSerializerOptions.Converters.ShouldContain(c => c is TypeJsonConverter);
    [Fact] void should_include_uri_converter() => _options.JsonSerializerOptions.Converters.ShouldContain(c => c is UriJsonConverter);
    [Fact] void should_include_enumerable_model_with_id_converter() => _options.JsonSerializerOptions.Converters.ShouldContain(c => c is EnumerableModelWithIdToConceptOrPrimitiveEnumerableConverterFactory);
}
