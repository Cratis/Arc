// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Queries.for_QueryArgumentsModels.given;

public class query_arguments_models : Specification
{
    protected QueryArgumentsModels _models;
    protected IQueryPerformer _performer;

    void Establish()
    {
        _models = new QueryArgumentsModels(NullLogger<QueryArgumentsModels>.Instance);
        _performer = Substitute.For<IQueryPerformer>();
        _performer.ReadModelType.Returns(typeof(SearchReadModel));
        _performer.FullyQualifiedName.Returns(new FullyQualifiedQueryName($"{Guid.NewGuid()}"));
    }

    /// <summary>
    /// Declares the query's name and parameters, which together decide which arguments model type applies.
    /// </summary>
    /// <param name="name">The query name, matching the method name on the read model.</param>
    /// <param name="parameters">The parameters the query exposes.</param>
    protected void ForQuery(string name, params QueryParameter[] parameters)
    {
        _performer.Name.Returns(new QueryName(name));
        _performer.Parameters.Returns(new QueryParameters(parameters));
    }

    /// <summary>
    /// Builds a <see cref="QueryArguments"/> from name/value pairs.
    /// </summary>
    /// <param name="arguments">The argument name/value pairs.</param>
    /// <returns>The <see cref="QueryArguments"/>.</returns>
    protected static QueryArguments ArgumentsOf(params (string Name, object Value)[] arguments)
    {
        var queryArguments = new QueryArguments();
        foreach (var (name, value) in arguments)
        {
            queryArguments[name] = value;
        }

        return queryArguments;
    }
}
