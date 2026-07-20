// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Execution;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Queries.Filters.for_FluentValidationFilter.given;

public class a_fluent_validation_filter : Specification
{
    protected IDiscoverableValidators _discoverableValidators;
    protected IQueryPerformerProviders _queryPerformerProviders;
    protected IQueryPerformer _performer;
    protected IQueryArgumentsModels _queryArgumentsModels;
    protected FluentValidationFilter _filter;
    protected CorrelationId _correlationId;

    void Establish()
    {
        _correlationId = CorrelationId.New();
        _discoverableValidators = Substitute.For<IDiscoverableValidators>();
        _queryPerformerProviders = Substitute.For<IQueryPerformerProviders>();
        _performer = Substitute.For<IQueryPerformer>();
        _performer.Parameters.Returns(new QueryParameters());
        _queryPerformerProviders
            .TryGetPerformersFor(Arg.Any<FullyQualifiedQueryName>(), out Arg.Any<IQueryPerformer>())
            .Returns(x =>
            {
                x[1] = _performer;
                return true;
            });

        _queryArgumentsModels = Substitute.For<IQueryArgumentsModels>();
        _filter = new FluentValidationFilter(
            _queryPerformerProviders,
            _queryArgumentsModels,
            new ModelGraphValidator(_discoverableValidators, NullLogger<ModelGraphValidator>.Instance));
    }

    /// <summary>
    /// Declares the parameters the query performer exposes, mirroring the signature of the query method under test.
    /// </summary>
    /// <param name="parameters">The <see cref="QueryParameter"/> collection to expose.</param>
    protected void WithParameters(params QueryParameter[] parameters) =>
        _performer.Parameters.Returns(new QueryParameters(parameters));

    /// <summary>
    /// Registers a validator for a type so the graph traversal discovers it, mirroring how
    /// <see cref="IDiscoverableValidators"/> resolves a convention-discovered validator at runtime.
    /// </summary>
    /// <param name="type">The model <see cref="Type"/> the validator applies to.</param>
    /// <param name="validator">The <see cref="IValidator"/> to return for it.</param>
    protected void WithValidatorFor(Type type, IValidator validator) =>
        _discoverableValidators.TryGet(type, out Arg.Any<IValidator>())
            .Returns(x =>
            {
                x[1] = validator;
                return true;
            });

    /// <summary>
    /// Declares that the query's arguments are modelled as a whole by the supplied instance, as they are when a
    /// validator is declared against the query's flat argument shape.
    /// </summary>
    /// <param name="model">The arguments model instance to validate.</param>
    protected void WithArgumentsModel(object model) =>
        _queryArgumentsModels.TryCreateFor(Arg.Any<IQueryPerformer>(), Arg.Any<QueryArguments>(), out Arg.Any<object>())
            .Returns(x =>
            {
                x[2] = model;
                return true;
            });

    /// <summary>
    /// Builds a <see cref="QueryContext"/> carrying the supplied arguments.
    /// </summary>
    /// <param name="arguments">The argument name/value pairs the client supplied.</param>
    /// <returns>A <see cref="QueryContext"/>.</returns>
    protected QueryContext ContextWith(params (string Name, object Value)[] arguments)
    {
        var queryArguments = new QueryArguments();
        foreach (var (name, value) in arguments)
        {
            queryArguments[name] = value;
        }

        return new QueryContext("SomeQuery", _correlationId, Paging.NotPaged, Sorting.None, queryArguments);
    }
}
