// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, render, waitFor } from '@testing-library/react';
import { a_command_form_with_a_required_property, NAME_FROM_THE_LOOKUP } from './given/a_command_form_with_a_required_property';
import { given } from '../../../../given';

/**
 * The boundary of the rule the specs beside this one pin, and the reason it is a rule about seeding
 * rather than a rule about undefined.
 *
 * currentValues is not a seed - it is what the form is currently bound to - so it carries presence
 * semantics: a key it holds is written whatever it holds, and an explicitly present undefined
 * clears the property exactly as null does. Only a key that is absent altogether is left alone.
 * Making the seed rule uniform would take this away, and taking it away is a breaking change to a
 * released contract.
 */
describe('when the undefined comes from currentValues instead', given(a_command_form_with_a_required_property, context => {
    let result: ReturnType<typeof render>;

    beforeEach(async () => {
        result = context.renderForm({ currentValues: { name: NAME_FROM_THE_LOOKUP }, validateOnInit: true });

        await waitFor(() => { expect(context.isValid).toBe(true); }, { timeout: 2000 });

        await act(async () => {
            result.rerender(context.formWith({ currentValues: { name: undefined }, validateOnInit: true }));
        });
    });

    afterEach(() => result.unmount());

    it('should clear the property', () => expect(context.commandInstance!.name).toBeUndefined());

    it('should reject the command for the missing required value', async () => {
        await waitFor(() => { context.validationMessages.should.contain('name is required'); }, { timeout: 2000 });
    });
}));
