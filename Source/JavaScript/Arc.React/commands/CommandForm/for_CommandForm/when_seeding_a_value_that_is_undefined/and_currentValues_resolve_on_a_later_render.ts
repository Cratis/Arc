// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, render, waitFor } from '@testing-library/react';
import { a_command_form_with_a_required_property, NAME_FROM_THE_LOOKUP } from './given/a_command_form_with_a_required_property';
import { given } from '../../../../given';

/**
 * The shape that actually reaches a consumer. currentValues comes from a query, so it is undefined
 * on the first render and answers on a later one, while initialValues is the same literal every
 * render - including the renders after the answer arrived. A fixture that only ever looks at the
 * first render never sees this: on the first render there is nothing for the seed to overwrite yet,
 * and the seed is only wrong once something has resolved underneath it.
 */
describe('when seeding a value that is undefined and currentValues resolve on a later render', given(a_command_form_with_a_required_property, context => {
    let result: ReturnType<typeof render>;

    beforeEach(async () => {
        result = context.renderForm({
            currentValues: undefined,
            initialValues: { name: undefined }
        });

        await act(async () => {
            result.rerender(context.formWith({
                currentValues: { name: NAME_FROM_THE_LOOKUP },
                initialValues: { name: undefined }
            }));
        });
    });

    afterEach(() => result.unmount());

    it('should apply the value the later render resolved', () => context.commandInstance!.name!.should.equal(NAME_FROM_THE_LOOKUP));

    it('should report the form as valid once the value has arrived', async () => {
        await waitFor(() => { expect(context.isValid).toBe(true); }, { timeout: 2000 });
    });
}));
