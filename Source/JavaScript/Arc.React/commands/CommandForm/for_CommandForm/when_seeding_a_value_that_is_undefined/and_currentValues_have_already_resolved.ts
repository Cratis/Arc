// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { waitFor } from '@testing-library/react';
import { a_command_form_with_a_required_property, NAME_FROM_THE_LOOKUP } from './given/a_command_form_with_a_required_property';
import { given } from '../../../../given';

/**
 * initialValues is written out by the caller as one object literal covering every property of the
 * command, and the ones it has nothing to say about are left as undefined. Object spread copies keys
 * regardless of what they hold, so that undefined lands on top of the value currentValues resolved
 * and replaces it with nothing.
 */
describe('when seeding a value that is undefined and currentValues have already resolved', given(a_command_form_with_a_required_property, context => {
    beforeEach(() => {
        context.renderForm({
            currentValues: { name: NAME_FROM_THE_LOOKUP },
            initialValues: { name: undefined }
        });
    });

    it('should leave the resolved value in place', () => context.commandInstance!.name!.should.equal(NAME_FROM_THE_LOOKUP));

    // The consequence of losing it: the property is required, so the form greys submit out over a
    // value the caller can see on screen.
    it('should report the form as valid', async () => {
        await waitFor(() => { expect(context.isValid).toBe(true); }, { timeout: 2000 });
    });
}));
