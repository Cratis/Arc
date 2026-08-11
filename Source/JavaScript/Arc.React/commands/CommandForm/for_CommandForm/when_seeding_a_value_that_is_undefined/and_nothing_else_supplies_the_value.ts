// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { waitFor } from '@testing-library/react';
import { a_command_form_with_a_required_property, NAME_FROM_THE_COMMAND_CLASS } from './given/a_command_form_with_a_required_property';
import { given } from '../../../../given';

/**
 * With nothing underneath it, a seed of undefined has nothing to overwrite - but it still decides
 * whether the form starts from the command class's own value or from nothing at all, and whether
 * change tracking records a baseline for the property. Seeding undefined supplies nothing, so this
 * has to be indistinguishable from passing no initialValues at all.
 */
describe('when seeding a value that is undefined and nothing else supplies it', given(a_command_form_with_a_required_property, context => {
    beforeEach(() => {
        context.renderForm({ initialValues: { name: undefined } });
    });

    it('should leave the value the command class declares', () => context.commandInstance!.name!.should.equal(NAME_FROM_THE_COMMAND_CLASS));

    it('should report the form as valid', async () => {
        await waitFor(() => { expect(context.isValid).toBe(true); }, { timeout: 2000 });
    });

    // Nothing was supplied, so nothing was recorded as the baseline to measure against, and the
    // value the class declares is a value the user has not committed yet. Seeding the property with
    // undefined instead recorded undefined as both the baseline and the value, which read as a
    // pristine form that was in fact holding nothing for a required property.
    it('should record no baseline for the property', () => context.commandInstance!.hasChanges.should.be.true);
}));
