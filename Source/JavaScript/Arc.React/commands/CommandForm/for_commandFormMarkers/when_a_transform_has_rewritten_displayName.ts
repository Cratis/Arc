// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    CommandFormColumnDisplayName,
    CommandFormFieldDisplayName,
    isCommandFormColumn,
    isCommandFormField,
    markAsCommandFormColumn,
    markAsCommandFormField
} from '../commandFormMarkers';

/**
 * A field used to be recognised only by `component.displayName === 'CommandFormField'`, so any transform that set
 * `displayName` unbound every field with no error and no warning - react-docgen-typescript does exactly that, by
 * default, through a documented Storybook option.
 */
describe('when a transform has rewritten displayName', () => {
    const field = markAsCommandFormField(function Field() { return null; });
    const column = markAsCommandFormColumn(function Column() { return null; });

    field.displayName = 'MyStorybookName';
    column.displayName = 'MyStorybookName';

    it('should still recognise the field', () => isCommandFormField(field).should.be.true);
    it('should still recognise the column', () => isCommandFormColumn(column).should.be.true);

    // The compatibility half, and why displayName is not on a deprecation path: a component from a package that
    // predates the marker, or one a consumer marked by hand, has only the string.
    it('should still recognise a component carrying only the field displayName', () =>
        isCommandFormField({ displayName: CommandFormFieldDisplayName }).should.be.true);
    it('should still recognise a component carrying only the column displayName', () =>
        isCommandFormColumn({ displayName: CommandFormColumnDisplayName }).should.be.true);

    it('should not recognise an unrelated component', () => isCommandFormField({ displayName: 'Something' }).should.be.false);
    it('should not mistake a column for a field', () => isCommandFormField(column).should.be.false);
    it('should not mistake a field for a column', () => isCommandFormColumn(field).should.be.false);
    it('should tolerate no component at all', () => isCommandFormField(undefined).should.be.false);
});
