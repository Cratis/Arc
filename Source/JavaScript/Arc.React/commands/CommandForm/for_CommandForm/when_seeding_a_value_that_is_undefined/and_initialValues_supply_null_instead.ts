// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { waitFor } from '@testing-library/react';
import { a_command_form_with_a_required_property, NAME_FROM_THE_LOOKUP } from './given/a_command_form_with_a_required_property';
import { given } from '../../../../given';

/**
 * null is a value, not an absence. A seed that skipped it as well would read as the same rule and
 * behave as a different one - it would make it impossible to seed a form with a deliberately empty
 * property over something the lookup resolved.
 */
describe('when seeding a value that is null and currentValues have already resolved', given(a_command_form_with_a_required_property, context => {
    beforeEach(() => {
        context.renderForm({
            currentValues: { name: NAME_FROM_THE_LOOKUP },
            initialValues: { name: null as unknown as string },
            validateOnInit: true
        });
    });

    it('should seed the property with null', () => expect(context.commandInstance!.name).toBeNull());

    // Asserted through the messages a completed run produced rather than through isValid, which
    // reads false before anything has run at all and would hold whether the seed landed or not.
    it('should reject the command for the missing required value', async () => {
        await waitFor(() => { context.validationMessages.should.contain('name is required'); }, { timeout: 2000 });
    });
}));
