// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { waitFor } from '@testing-library/react';
import { CommandFormField } from '../../CommandFormField';
import { a_command_form_with_a_required_property, NAME_FROM_THE_LOOKUP, RequiredNameCommand } from './given/a_command_form_with_a_required_property';
import { given } from '../../../../given';

/**
 * A field's currentValue prop is the other half of the seed layer, and a field that has nothing to
 * seed with declares undefined the same way a caller does. It is skipped where it is extracted
 * rather than where the layers are merged, so this is a separate guard from the initialValues one.
 */
describe('when seeding a value that is undefined and a field declares no current value', given(a_command_form_with_a_required_property, context => {
    beforeEach(() => {
        context.renderForm(
            { currentValues: { name: NAME_FROM_THE_LOOKUP } },
            React.createElement(CommandFormField, {
                value: (command: RequiredNameCommand) => command.name,
                currentValue: undefined,
                title: 'Name'
            }));
    });

    it('should leave the resolved value in place', () => context.commandInstance!.name!.should.equal(NAME_FROM_THE_LOOKUP));

    it('should report the form as valid', async () => {
        await waitFor(() => { expect(context.isValid).toBe(true); }, { timeout: 2000 });
    });
}));
