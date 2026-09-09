// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing_single;

/// <summary>
/// The document is updated so it no longer matches the filter — it still exists, but has left the observed
/// result set. This is the sibling case to a hard delete: the "belongs" re-check <c>HandleChange</c> performs
/// for an update/replace reports the document as no longer matching.
/// </summary>
public class and_the_observed_document_leaves_the_filter : given.an_observed_collection
{
    ISubject<ObservedDocument> _subject;
    EmissionSequence _emissions;
    ObservedDocument _target;
    ObservedDocument _renamed;
    ObservedDocument? _initialEmission;
    ObservedDocument? _afterUpdateEmission;

    void Establish()
    {
        _target = new ObservedDocument(Guid.NewGuid(), "Target");
        _renamed = _target with { Name = "Renamed" };
        _documents = [_target];
        StubUpdateBelongsCheck(belongs: false);
    }

    async Task Because()
    {
        _subject = _collection.ObserveSingle(d => d.Name == "Target");
        _emissions = RecordEmissions(_subject);
        _initialQueryGate.SetResult();

        _initialEmission = await _emissions.Next(TimeSpan.FromSeconds(5));

        PushChange(UpdateOf(_renamed));
        _afterUpdateEmission = await _emissions.Next(TimeSpan.FromSeconds(5));
    }

    void Destroy() => _emissions?.Dispose();

    [Fact] void should_emit_the_document_initially() => _initialEmission.ShouldEqual(_target);
    [Fact] void should_emit_the_default_value_after_leaving_the_filter() => _afterUpdateEmission.ShouldBeNull();
}
