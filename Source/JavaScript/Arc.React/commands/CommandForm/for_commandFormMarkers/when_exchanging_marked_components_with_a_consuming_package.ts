// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    CommandFormColumnDisplayName,
    CommandFormFieldDisplayName,
    CommandFormMarked,
    isCommandFormColumn,
    isCommandFormField,
    markAsCommandFormColumn,
    markAsCommandFormField
} from '../commandFormMarkers';

/**
 * The cross-package contract, and the only spec that can catch this package and a consuming one
 * drifting apart.
 *
 * `@cratis/components` reads the field marker written here, and writes the column marker read here.
 * Neither imports the other's helper — a consuming package declares this one as a version range, so
 * a named import would be a hard module-link error against any version in that range predating the
 * marker — so the contract is carried entirely by the property names below being identical on both
 * sides. Nothing about that is enforced by the compiler.
 *
 * That is not hypothetical. The two packages were briefly implemented with different shapes, one a
 * static property and the other a `Symbol`, and every spec in both repositories passed: both kept
 * the `displayName` fallback, so nothing threw, and the marker simply stopped crossing the
 * boundary. A field whose `displayName` a build transform had rewritten bound in a bare
 * `CommandForm` and silently unbound inside a `CommandDialog` — the exact failure the marker was
 * added to prevent, surviving the fix.
 *
 * The literals here are therefore deliberate rather than lazy: they restate what a consuming
 * package writes and reads, so this reds if either side moves.
 */
describe('when exchanging marked components with a consuming package', () => {
    const markedElsewhere = (marker: 'isCommandFormField' | 'isCommandFormColumn', displayName: string): CommandFormMarked => {
        const component = function Component() { return null; };
        return Object.assign(component, { [marker]: true, displayName }) as CommandFormMarked;
    };

    // A component another package marked, whose displayName a build transform then rewrote, so only
    // the marker is left to recognize it by.
    const field = markedElsewhere('isCommandFormField', CommandFormFieldDisplayName);
    const column = markedElsewhere('isCommandFormColumn', CommandFormColumnDisplayName);
    field.displayName = 'RenamedByABuildTransform';
    column.displayName = 'RenamedByABuildTransform';

    it('should recognize a field another package marked', () => isCommandFormField(field).should.be.true);
    it('should recognize a column another package marked', () => isCommandFormColumn(column).should.be.true);

    // The other half: what this package writes has to be what the consuming package reads. Asserted
    // as raw property access rather than through the helpers, because that is the half performed by
    // code this package does not run.
    const ownField = markAsCommandFormField(function Field() { return null; });
    const ownColumn = markAsCommandFormColumn(function Column() { return null; });

    it('should expose the field marker under the property name a consuming package reads', () =>
        (ownField as unknown as Record<string, unknown>).isCommandFormField!.should.equal(true));
    it('should expose the column marker under the property name a consuming package reads', () =>
        (ownColumn as unknown as Record<string, unknown>).isCommandFormColumn!.should.equal(true));

    it('should keep the legacy field displayName for a consumer predating the marker', () =>
        (ownField as unknown as Record<string, unknown>).displayName!.should.equal('CommandFormField'));
    it('should keep the legacy column displayName for a consumer predating the marker', () =>
        (ownColumn as unknown as Record<string, unknown>).displayName!.should.equal('CommandFormColumn'));
});
