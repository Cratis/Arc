// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_QueryRowNarrower;

public class when_narrowing_a_queryable_by_allowed_keys : Specification
{
    public record Vessel(string Id, string Name);

    public class VesselsOnThePlan : QueryRowNarrowing<Vessel, string>
    {
        protected override Expression<Func<Vessel, string>> Key => vessel => vessel.Id;

        protected override ValueTask<IEnumerable<string>> GetAllowedKeys(QueryContext context) => ValueTask.FromResult<IEnumerable<string>>(["v1", "v3"]);
    }

    ServiceProvider _services;
    QueryContext _context;
    IQueryable<Vessel> _vessels;
    object _narrowed;
    PagedResult _page;

    record PagedResult(int Total, List<string> Names);

    void Establish()
    {
        _services = new ServiceCollection().AddSingleton<IQueryRowNarrowing, VesselsOnThePlan>().BuildServiceProvider();
        _context = new("Fleet.Vessels", CorrelationId.New(), new Paging(0, 1, true), Sorting.None);
        _vessels = new[] { new Vessel("v1", "Alpha"), new Vessel("v2", "Beta"), new Vessel("v3", "Gamma"), new Vessel("v4", "Delta") }.AsQueryable();
    }

    async Task Because()
    {
        _narrowed = await QueryRowNarrower.Narrow("Fleet.Vessels", typeof(Vessel), _vessels, _context, _services);
        var result = new QueryableQueryRenderer().Execute((IQueryable)_narrowed, _context);
        _page = new(result.TotalItems, [.. ((IQueryable<Vessel>)result.Data).Select(vessel => vessel.Name)]);
    }

    [Fact] void should_count_only_the_allowed_rows() => _page.Total.ShouldEqual(2);
    [Fact] void should_page_the_allowed_rows() => _page.Names.ShouldEqual(["Alpha"]);
}
