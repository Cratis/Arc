// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.ReadModels.for_SelfReducingReadModel;

public class when_recognizing_an_inherited_reduction : Specification
{
    Type[] _recognized;

    void Because() => _recognized = new[] { typeof(DerivedModel), typeof(BaseModel) }
        .Where(SelfReducingReadModel.IsSelfReducing).ToArray();

    [Fact] void should_recognize_the_model_with_an_inherited_static_reduction() => _recognized.ShouldContainOnly(typeof(DerivedModel));

    [EventType] public record InheritedSelfModelChanged;

    record DerivedModel : BaseModel;

    record BaseModel
    {
        public static DerivedModel? Reduce(InheritedSelfModelChanged @event, DerivedModel? current) => current;
    }
}
