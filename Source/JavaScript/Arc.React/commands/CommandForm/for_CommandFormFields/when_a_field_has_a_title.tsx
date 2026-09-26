// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import { vi } from 'vitest';
import { CommandForm } from '../CommandForm';
import { asCommandFormField, type WrappedFieldProps } from '../asCommandFormField';
import { InputTextField } from '../fields/InputTextField';
import { RadioGroupField } from '../fields/RadioGroupField';
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

const CustomGroupField = asCommandFormField<WrappedFieldProps<string>>(
    (props) => (
        <div id={props.id}>
            {['First', 'Second'].map((option) => (
                <label key={option}>
                    <input type='checkbox' checked={props.value === option} onChange={() => props.onChange(option)} />
                    {option}
                </label>
            ))}
        </div>
    ),
    { defaultValue: '', groupRole: 'group' },
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

    it('should name a radio group without selecting an option when its title is clicked', () => {
        const onFieldChange = vi.fn();
        render(
            <CommandForm command={TestCommand} onFieldChange={onFieldChange}>
                <RadioGroupField
                    value={(command: TestCommand) => command.name}
                    title='Choice'
                    id='choice-group'
                    options={[
                        { value: 'First', label: 'First' },
                        { value: 'Second', label: 'Second' },
                    ]}
                />
            </CommandForm>,
            { wrapper: context.createWrapper() },
        );

        const group = screen.getByRole('radiogroup', { name: 'Choice' });
        const radios = screen.getAllByRole('radio') as HTMLInputElement[];
        expect(group.querySelector('#choice-group')).not.toBeNull();
        radios[0].id.should.equal('');
        fireEvent.click(radios[1]);
        radios[1].checked.should.equal(true);
        expect(onFieldChange).toHaveBeenCalledTimes(1);

        const onRadioChange = vi.fn();
        radios.forEach((radio) => radio.addEventListener('change', onRadioChange));
        fireEvent.click(screen.getByText('Choice'));
        radios[1].checked.should.equal(true);
        radios[0].checked.should.equal(false);
        expect(onRadioChange).not.toHaveBeenCalled();
        expect(onFieldChange).toHaveBeenCalledTimes(1);
    });

    it('should name a custom multi-input field as a group', () => {
        render(
            <CommandForm command={TestCommand}>
                <CustomGroupField value={(command: TestCommand) => command.name} title='Options' />
            </CommandForm>,
            { wrapper: context.createWrapper() },
        );

        expect(screen.getByRole('group', { name: 'Options' })).not.toBeNull();
        screen.getAllByRole('checkbox').length.should.equal(2);
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

describe('when a radio group has no rendered title', given(a_command_form_fields_context, (context) => {
    const renderRadioGroup = (title?: string, showTitles = true) => render(
        <CommandForm command={TestCommand} showTitles={showTitles}>
            <RadioGroupField
                value={(command: TestCommand) => command.name}
                title={title}
                options={[
                    { value: 'First', label: 'First' },
                    { value: 'Second', label: 'Second' },
                ]}
            />
        </CommandForm>,
        { wrapper: context.createWrapper() },
    );

    it('should not render an unnamed radiogroup when no title was supplied', () => {
        renderRadioGroup();

        expect(screen.queryByRole('radiogroup')).toBeNull();
        screen.getAllByRole('radio').length.should.equal(2);
    });

    it('should not render an unnamed radiogroup when titles are hidden', () => {
        renderRadioGroup('Choice', false);

        expect(screen.queryByRole('radiogroup')).toBeNull();
        screen.getAllByRole('radio').length.should.equal(2);
    });
}));
