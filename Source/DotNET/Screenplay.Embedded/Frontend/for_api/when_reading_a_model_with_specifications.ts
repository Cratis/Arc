// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// @vitest-environment node

import { describe, expect, it } from 'vitest';
import { toDocumentModel } from '../api';
import { modelFor } from '../Specs/given/api';

const id = (n: number) => `00000000-0000-4000-8000-${n.toString().padStart(12, '0')}`;

// The shape the embedded host writes a slice's specifications in - camel cased, every member present, null where
// nothing is stated - so the published reader is held to what the board is really handed.
const specification = {
    id: id(10),
    name: 'RegisteringReturnsIdentifiers — then returns { receiptId = "22222222-2222-2222-2222-222222222222" }',
    given: [{ id: id(11), name: 'ProjectRegistered', eventId: id(3), values: { name: 'Apollo' } }],
    when: { id: id(12), commandId: id(2), name: 'RegisterProject', values: { name: 'Apollo' } },
    thenEvents: [{ id: id(13), name: 'ProjectRegistered', eventId: id(3), values: { name: 'Apollo', pages: 120 } }],
    thenErrors: [{ id: id(14), name: 'A project needs a name' }],
    collapsed: false
};

const appendingSpecification = {
    id: id(20),
    name: 'ListingARegisteredAuthor',
    given: [],
    when: { id: id(21), commandId: null, name: 'append AuthorRegistered', values: { name: 'Ursula' } },
    thenEvents: [],
    thenErrors: [],
    collapsed: false
};

const slice = (specifications: unknown[]) => ({
    id: id(1),
    name: 'Register',
    sliceType: 0,
    status: 1,
    collapsed: false,
    sortOrder: 0,
    command: null,
    readModel: null,
    externalEvents: [],
    events: [],
    queries: [],
    actors: [],
    specifications,
    commentCount: 0
});

const documentWith = (specifications: unknown[]) => ({
    ...modelFor('Projects'),
    collections: [{
        id: id(4),
        position: { x: 0, y: 0 },
        actors: [],
        modules: [{ id: id(5), name: 'Projects', collapsed: false, sortOrder: 0, features: [{ id: id(6), name: 'Registration', subFeatures: [], slices: [slice(specifications)] }] }]
    }]
});

describe('when reading a model with specifications', () => {
    const { document } = toDocumentModel({ eventModel: documentWith([specification, appendingSpecification]), success: true, warnings: [] });
    const [running, appending] = document.collections[0].modules[0].features[0].slices[0].specifications;

    it('keeps every specification of the slice, in order', () => {
        expect([running.name, appending.name]).toEqual([specification.name, appendingSpecification.name]);
    });

    it('keeps the given events with their values', () => {
        expect(running.given.map(step => [step.name, step.eventId.toString(), step.values])).toEqual([['ProjectRegistered', id(3), { name: 'Apollo' }]]);
    });

    it('keeps the command the specification runs', () => {
        expect([running.when?.name, running.when?.commandId?.toString(), running.when?.values]).toEqual(['RegisterProject', id(2), { name: 'Apollo' }]);
    });

    it('keeps the expected events with their values', () => {
        expect(running.thenEvents.map(step => [step.name, step.values])).toEqual([['ProjectRegistered', { name: 'Apollo', pages: 120 }]]);
    });

    it('keeps the expected errors', () => {
        expect(running.thenErrors.map(error => error.name)).toEqual(['A project needs a name']);
    });

    it('keeps an action that runs no command without pointing it at one', () => {
        expect([appending.when?.name, appending.when?.commandId]).toEqual(['append AuthorRegistered', undefined]);
    });
});
