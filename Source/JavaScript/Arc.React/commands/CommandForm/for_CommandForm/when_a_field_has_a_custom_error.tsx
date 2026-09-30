// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, render } from '@testing-library/react';
import { CommandForm } from '../CommandForm.js';
import { useCommandFormContext, type CommandFormContextValue } from '../CommandFormContext.js';
import { asCommandFormField, type WrappedFieldProps } from '../asCommandFormField.js';
import { a_command_form_context } from './given/a_command_form_context.js';
import { TestCommand } from './TestCommand.js';

const ErrorsField = asCommandFormField<WrappedFieldProps<string>>(
    (props) => <output data-testid="errors" data-errors={JSON.stringify(props.errors)} data-invalid={String(props.invalid)} />,
    { defaultValue: '' },
);

describe('when a field has a custom error', () => {
    let context: CommandFormContextValue<TestCommand>;
    let errors: string[];
    let invalid: string | null;

    const Probe = () => {
        context = useCommandFormContext<TestCommand>();
        return null;
    };

    beforeEach(async () => {
        const { getByTestId } = render(
            <CommandForm command={TestCommand}>
                <Probe />
                <ErrorsField<TestCommand> value={(c) => c.name} />
            </CommandForm>,
            { wrapper: new a_command_form_context().createWrapper() },
        );
        await act(() => Promise.resolve());
        await act(async () => { context.setCustomFieldError('name', 'Name is required.'); });

        const field = getByTestId('errors');
        errors = JSON.parse(field.getAttribute('data-errors')!);
        invalid = field.getAttribute('data-invalid');
    });

    it('should hand the field the message exactly once', () => errors.should.deep.equal(['Name is required.']));
    it('should mark the field invalid', () => invalid!.should.equal('true'));
});
