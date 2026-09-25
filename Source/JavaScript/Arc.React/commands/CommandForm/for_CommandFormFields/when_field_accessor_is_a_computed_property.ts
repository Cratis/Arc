// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, fireEvent } from '@testing-library/react';
import { CommandForm } from '../CommandForm';
// eslint-disable-next-line @typescript-eslint/no-unused-vars
import { CommandFormField } from '../CommandFormField';
import { asCommandFormField } from '../asCommandFormField';
import { TestCommand } from '../for_CommandForm/TestCommand';
import { a_command_form_fields_context } from './given/a_command_form_fields_context';
import { given } from '../../../given';

const SimpleTextField = asCommandFormField<{ value: string; onChange: (value: unknown) => void; invalid: boolean; required: boolean; errors: string[]; title?: string }>(
    (props) => {
        return React.createElement('input', {
            type: 'text',
            value: props.value,
            onChange: props.onChange,
            'data-testid': `computed-${(props.title ?? '').toLowerCase()}`
        });
    },
    {
        defaultValue: '',
        extractValue: (e: unknown) => (e as React.ChangeEvent<HTMLInputElement>).target.value
    }
);

// A generated form builds its accessors from runtime property descriptors, so every accessor has
// the same source text and inference alone cannot tell the fields apart.
const computedAccessor = (propertyName: string) =>
    (instance: TestCommand) => (instance as unknown as Record<string, unknown>)[propertyName];

describe("when field accessor is a computed property", given(a_command_form_fields_context, context => {
    let container: HTMLElement;

    beforeEach(() => {
        const result = render(
            React.createElement(
                CommandForm,
                { command: TestCommand },
                React.createElement(SimpleTextField, {
                    value: computedAccessor('name'),
                    fieldName: 'name',
                    title: 'Name'
                }),
                React.createElement(SimpleTextField, {
                    value: computedAccessor('email'),
                    fieldName: 'email',
                    title: 'Email'
                })
            ),
            { wrapper: context.createWrapper() }
        );
        container = result.container;
    });

    it('should bind each field to its own declared property', () => {
        expect(container.querySelector('[data-testid="computed-name"]')).not.toBeNull();
        expect(container.querySelector('[data-testid="computed-email"]')).not.toBeNull();
    });

    it('should keep the other field untouched when one is edited', () => {
        const name = container.querySelector('[data-testid="computed-name"]') as HTMLInputElement;
        fireEvent.change(name, { target: { value: 'Ada' } });

        const email = container.querySelector('[data-testid="computed-email"]') as HTMLInputElement;
        expect((container.querySelector('[data-testid="computed-name"]') as HTMLInputElement).value).toBe('Ada');
        expect(email.value).toBe('');
    });
}));
