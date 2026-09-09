// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing_single;

public class and_another_document_is_deleted : given.an_observed_collection
{
    ISubject<ObservedDocument> _subject;
    EmissionSequence _emissions;
    ObservedDocument _target;
    ObservedDocument? _initialEmission;
    bool _emittedAgain;

    void Establish()
    {
        _target = new ObservedDocument(Guid.NewGuid(), "Target");
        _documents = [_target];
    }

    async Task Because()
    {
        _subject = _collection.ObserveSingle();
        _emissions = RecordEmissions(_subject);
        _initialQueryGate.SetResult();

        _initialEmission = await _emissions.Next(TimeSpan.FromSeconds(5));

        // An id that was never part of the tracked observed set — removing it produces no change and must
        // not trigger a re-emission.
        PushChange(DeleteOf(Guid.NewGuid()));

        try
        {
            await _emissions.Next(TimeSpan.FromMilliseconds(300));
            _emittedAgain = true;
        }
        catch (TimeoutException)
        {
            // Expected: nothing further was emitted within the window, which is the behavior under test.
            _emittedAgain = false;
        }
    }

    void Destroy() => _emissions?.Dispose();

    [Fact] void should_emit_the_document_initially() => _initialEmission.ShouldEqual(_target);
    [Fact] void should_not_emit_again() => _emittedAgain.ShouldBeFalse();
}
