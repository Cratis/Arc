// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing_by_id;

public class and_the_document_is_deleted : given.an_observed_collection
{
    ISubject<ObservedDocument> _subject;
    EmissionSequence _emissions;
    ObservedDocument _target;
    ObservedDocument? _initialEmission;
    ObservedDocument? _afterDeleteEmission;

    void Establish()
    {
        _target = new ObservedDocument(Guid.NewGuid(), "Target");
        _documents = [_target];
    }

    async Task Because()
    {
        _subject = _collection.ObserveById(_target.Id);
        _emissions = RecordEmissions(_subject);
        _initialQueryGate.SetResult();

        _initialEmission = await _emissions.Next(TimeSpan.FromSeconds(5));

        PushChange(DeleteOf(_target.Id));
        _afterDeleteEmission = await _emissions.Next(TimeSpan.FromSeconds(5));
    }

    void Destroy() => _emissions?.Dispose();

    [Fact] void should_emit_the_document_initially() => _initialEmission.ShouldEqual(_target);
    [Fact] void should_emit_the_default_value_after_the_delete() => _afterDeleteEmission.ShouldBeNull();
}
