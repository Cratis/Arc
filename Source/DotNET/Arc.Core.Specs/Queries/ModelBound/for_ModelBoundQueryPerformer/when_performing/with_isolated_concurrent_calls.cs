// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_performing;

/// <summary>
/// Smoke test for per-call argument isolation during concurrent execution.
/// </summary>
public class with_isolated_concurrent_calls : given.a_model_bound_query_performer
{
    public record TestReadModel(object Dependency, string Name, int Age)
    {
        public static TestReadModel Query(object dependency, string name, int age) => new(dependency, name, age);
    }

    readonly object[] _expectedDependencies = Enumerable.Range(0, 64).Select(_ => new object()).ToArray();
    TestReadModel[] _results;

    void Establish()
    {
        _serviceProviderIsService.IsService(typeof(object)).Returns(true);
        _serviceProviderIsService.IsService(typeof(string)).Returns(false);
        EstablishPerformer<TestReadModel>(nameof(TestReadModel.Query));
    }

    async Task Because()
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = Enumerable.Range(0, _expectedDependencies.Length).Select(index => Task.Run(async () =>
        {
            var context = _context with
            {
                Dependencies = [_expectedDependencies[index]],
                Arguments = new QueryArguments { ["name"] = $"Name {index}", ["age"] = index.ToString() }
            };
            await start.Task;
            return (TestReadModel)(await _performer.Perform(context))!;
        })).ToArray();

        start.SetResult();
        _results = await Task.WhenAll(calls);
    }

    [Fact] void should_bind_every_dependency_and_query_argument()
    {
        for (var index = 0; index < _results.Length; index++)
        {
            ReferenceEquals(_results[index].Dependency, _expectedDependencies[index]).ShouldBeTrue();
            _results[index].Name.ShouldEqual($"Name {index}");
            _results[index].Age.ShouldEqual(index);
        }
    }
}
