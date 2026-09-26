// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, screen } from '@testing-library/react';
import { CommandForm } from '../CommandForm';
import { asCommandFormField, type WrappedFieldProps } from '../asCommandFormField';
import { InputTextField } from '../fields/InputTextField';
import { TestCommand } from '../for_CommandForm/TestCommand';
import { a_command_form_fields_context } from './given/a_command_form_fields_context';
import { given } from '../../../given';

const CustomField = asCommandFormField<WrappedFieldProps<string>>(
    (props) => (
        <input id={props.id} value={props.value} onChange={props.onChange} onBlur={props.onBlur} />
    ),
    {
        defaultValue: '',
        extractValue: (event: unknown) => (event as React.ChangeEvent<HTMLInputElement>).target.value,
    },
);

describe('when a field has a title', given(a_command_form_fields_context, (context) => {
    it('should focus its input when the title is clicked', () => {
        render(
            <CommandForm command={TestCommand}>
                <InputTextField value={(command: TestCommand) => command.name} title='Name' />
            </CommandForm>,
            { wrapper: context.createWrapper() },
        );

        const label = screen.getByText('Name') as HTMLLabelElement;
        const input = screen.getByRole('textbox');
        // jsdom does not perform the browser's default label activation on click.
        label.addEventListener('click', () => label.control?.focus());
        label.click();
        (document.activeElement === input).should.equal(true);
    });

    it('should give the input the title as its accessible name', () => {
        render(
            <CommandForm command={TestCommand}>
                <InputTextField value={(command: TestCommand) => command.name} title='Name' />
            </CommandForm>,
            { wrapper: context.createWrapper() },
        );

        expect(screen.getByLabelText('Name')).toBe(screen.getByRole('textbox'));
    });

    it('should preserve a caller-provided id on the input and label', () => {
        render(
            <CommandForm command={TestCommand}>
                <InputTextField value={(command: TestCommand) => command.name} title='Name' id='profile-name' />
            </CommandForm>,
            { wrapper: context.createWrapper() },
        );

        (screen.getByLabelText('Name') as HTMLInputElement).id.should.equal('profile-name');
        (screen.getByText('Name') as HTMLLabelElement).htmlFor.should.equal('profile-name');
    });

    it('should pass the generated id through the custom field adapter', () => {
        render(
            <CommandForm command={TestCommand}>
                <CustomField value={(command: TestCommand) => command.name} title='Name' />
            </CommandForm>,
            { wrapper: context.createWrapper() },
        );

        const input = screen.getByLabelText('Name') as HTMLInputElement;
        input.id.should.not.equal('');
        (screen.getByText('Name') as HTMLLabelElement).htmlFor.should.equal(input.id);
    });
}));
