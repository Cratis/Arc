// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it, vi } from 'vitest';
import {
    defaultPresentation,
    parseViewOptions,
    presentationStorageKey,
    readPresentation,
    toStoredViewOptions,
    writePresentation
} from '../presentation';

vi.mock('@cratis/event-models', () => import('../Specs/given/eventModelsStub'));

const storageWith = (entries: Record<string, string>): Storage => ({
    getItem: (key: string) => entries[key] ?? null,
    setItem: (key: string, value: string) => { entries[key] = value; },
    removeItem: (key: string) => { delete entries[key]; },
    clear: () => { for (const key of Object.keys(entries)) delete entries[key]; },
    key: (index: number) => Object.keys(entries)[index] ?? null,
    get length() { return Object.keys(entries).length; }
});

describe('when reading remembered view options', () => {
    it('shows what was last chosen', () => {
        const storage = storageWith({
            [presentationStorageKey]: JSON.stringify({ detailLevel: 'overview', showProperties: true, visualizationMode: 'fillLines' })
        });

        expect(readPresentation(storage)).toEqual({
            detailLevel: 'overview',
            visualizationMode: 'fillLines',
            detailsVisibility: { global: true, modules: {}, features: {}, slices: {} }
        });
    });

    it('falls back to the defaults for anything it does not recognize', () => {
        expect(parseViewOptions(JSON.stringify({ detailLevel: 'microscopic', showProperties: 'yes', visualizationMode: 42 })))
            .toEqual(toStoredViewOptions(defaultPresentation));
        expect(parseViewOptions('not json at all')).toEqual(toStoredViewOptions(defaultPresentation));
        expect(parseViewOptions('null')).toEqual(toStoredViewOptions(defaultPresentation));
        expect(parseViewOptions(null)).toEqual(toStoredViewOptions(defaultPresentation));
    });

    it('keeps the fields it does recognize when the rest is unusable', () => {
        expect(parseViewOptions(JSON.stringify({ detailLevel: 'overview', visualizationMode: 'nonsense' })))
            .toEqual({ detailLevel: 'overview', showProperties: false, visualizationMode: 'simplified' });
    });

    it('starts from the defaults when there is no storage at all', () => {
        expect(readPresentation(undefined)).toEqual(defaultPresentation);
    });

    it('starts from the defaults when storage cannot be read', () => {
        const denied = { ...storageWith({}), getItem: () => { throw new Error('denied'); } } as unknown as Storage;

        expect(readPresentation(denied)).toEqual(defaultPresentation);
    });
});

describe('when remembering view options', () => {
    it('writes only what the person chose', () => {
        const entries: Record<string, string> = {};

        writePresentation(storageWith(entries), { ...defaultPresentation, detailLevel: 'overview' });

        expect(JSON.parse(entries[presentationStorageKey])).toEqual({
            detailLevel: 'overview', showProperties: false, visualizationMode: 'simplified'
        });
    });

    it('reports a storage that refuses the write instead of pretending it kept it', () => {
        const full = { ...storageWith({}), setItem: () => { throw new Error('quota exceeded'); } } as unknown as Storage;

        expect(() => writePresentation(full, defaultPresentation)).toThrow('quota exceeded');
        expect(() => writePresentation(undefined, defaultPresentation)).toThrow(/no storage/);
    });
});
