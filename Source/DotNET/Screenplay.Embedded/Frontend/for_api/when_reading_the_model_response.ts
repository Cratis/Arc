// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { toDocumentModel } from '../api';
import { modelFor } from '../Specs/given/api';

describe('when reading the model response', () => {
    it('reads the document with the published reader and keeps warnings beside it', () => {
        const { document, warnings } = toDocumentModel({ ...modelFor('Acme.Orders'), warnings: ['Screens are not shown'] });

        expect(document.name).toBe('Acme.Orders');
        expect(document.collections).toEqual([]);
        expect(warnings).toEqual(['Screens are not shown']);
    });

    it('reads the backend compiler visitor result', () => {
        const { document, warnings } = toDocumentModel({ eventModel: modelFor('Acme.Orders'), success: true, errors: [], warnings: ['Multiple commands'] });

        expect(document.name).toBe('Acme.Orders');
        expect(warnings).toEqual(['Multiple commands']);
    });

    it('surfaces a compiler rejection instead of presenting an empty model', () => {
        expect(() => toDocumentModel({ eventModel: modelFor('Acme.Orders'), success: false, errors: ['Unknown event'], warnings: [] })).toThrow('Unknown event');
    });

    it('formats the actual backend warning objects with their location', () => {
        const { warnings } = toDocumentModel({ eventModel: modelFor('Acme.Orders'), success: true, warnings: [
            { code: 'EM001', message: 'Screens are not shown', path: 'orders.play', line: 12, column: 3 }
        ] });
        expect(warnings).toEqual(['orders.play:12:3: EM001: Screens are not shown']);
    });

    it('surfaces the actual backend compiler diagnostic objects', () => {
        expect(() => toDocumentModel({ success: false, errors: [
            { code: 'SP001', message: 'Unknown event', line: null, column: null, path: null }
        ] })).toThrow('SP001: Unknown event');
    });

    it('refuses a payload the board cannot draw', () => {
        expect(() => toDocumentModel({ modules: [], features: [] })).toThrow();
    });
});
