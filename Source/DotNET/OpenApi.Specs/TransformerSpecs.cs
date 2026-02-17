// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.OpenApi;

public class when_adding_concepts_to_open_api : Specification
{
    ServiceCollection _services;
    bool _exceptionThrown;

    void Establish()
    {
        _services = new ServiceCollection();
        _exceptionThrown = false;
    }

    void Because()
    {
        try
        {
            _services.AddOpenApi(options =>
            {
                options.AddConcepts();
            });
        }
        catch
        {
            _exceptionThrown = true;
        }
    }

    [Fact] void should_not_throw_exception() => _exceptionThrown.ShouldBeFalse();
}

public class when_creating_concept_schema_transformer : Specification
{
    ConceptSchemaTransformer _transformer;
    bool _created;

    void Because()
    {
        _transformer = new ConceptSchemaTransformer();
        _created = _transformer is not null;
    }

    [Fact] void should_create_transformer() => _created.ShouldBeTrue();
}

public class when_creating_enum_schema_transformer : Specification
{
    EnumSchemaTransformer _transformer;
    bool _created;

    void Because()
    {
        _transformer = new EnumSchemaTransformer();
        _created = _transformer is not null;
    }

    [Fact] void should_create_transformer() => _created.ShouldBeTrue();
}

public class when_creating_command_result_operation_transformer : Specification
{
    CommandResultOperationTransformer _transformer;
    bool _created;

    void Because()
    {
        _transformer = new CommandResultOperationTransformer();
        _created = _transformer is not null;
    }

    [Fact] void should_create_transformer() => _created.ShouldBeTrue();
}

public class when_creating_query_result_operation_transformer : Specification
{
    QueryResultOperationTransformer _transformer;
    bool _created;

    void Because()
    {
        _transformer = new QueryResultOperationTransformer();
        _created = _transformer is not null;
    }

    [Fact] void should_create_transformer() => _created.ShouldBeTrue();
}

public class when_creating_from_request_operation_transformer : Specification
{
    FromRequestOperationTransformer _transformer;
    bool _created;

    void Because()
    {
        _transformer = new FromRequestOperationTransformer();
        _created = _transformer is not null;
    }

    [Fact] void should_create_transformer() => _created.ShouldBeTrue();
}

public class when_creating_from_request_schema_transformer : Specification
{
    FromRequestSchemaTransformer _transformer;
    bool _created;

    void Because()
    {
        _transformer = new FromRequestSchemaTransformer();
        _created = _transformer is not null;
    }

    [Fact] void should_create_transformer() => _created.ShouldBeTrue();
}
