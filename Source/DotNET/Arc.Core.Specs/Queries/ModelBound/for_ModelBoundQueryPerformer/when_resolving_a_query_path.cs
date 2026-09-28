// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer;

public class when_resolving_a_query_path : given.a_model_bound_query_performer
{
    void Because() => EstablishPerformer<Queries>(nameof(Queries.Get));

    [Fact] void should_choose_the_real_method_path_over_other_attributes_and_the_type_path() =>
        _performer.CustomRoute.ShouldEqual("/queries/get");

    [Unrelated.Path("/wrong-type")]
    [Path("/queries")]
    public static class Queries
    {
        [Unrelated.Path("/wrong-method")]
        [Path("/queries/get")]
        public static int Get() => 1;
    }

    public static class Unrelated
    {
        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
        public sealed class PathAttribute(string path) : Attribute
        {
            public string Path { get; } = path;
        }
    }
}
