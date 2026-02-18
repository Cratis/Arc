// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { CommandForm } from '../CommandForm';
import { CommandFormField } from '../CommandFormField';
import { asCommandFormField } from '../asCommandFormField';
import { TestCommand } from '../for_CommandForm/TestCommand';
import { a_command_form_fields_context } from './given/a_command_form_fields_context';
import { given } from '../../../given';

const SimpleTextField = asCommandFormField<{ value: string; onChange: (value: unknown) => void; invalid: boolean; required: boolean; errors: string[] }>(
    (props) => {
        return React.createElement('input', {
            type: 'text',
            value: props.value,
            onChange: props.onChange,
            'data-testid': `text-input-${props.value || 'empty'}`
        });
    },
    {
        defaultValue: '',
        extractValue: (e: unknown) => (e as React.ChangeEvent<HTMLInputElement>).target.value
    }
);

describe("when rendering with fields array", given(a_command_form_fields_context, context => {
    let container: HTMLElement;

    beforeEach(() => {
        const result = render(
            React.createElement(
                CommandForm,
                { command: TestCommand },
                React.createElement(CommandFormField, {
                    value: (c: TestCommand) => c.name,
                    title: 'Name'
                }, React.createElement(SimpleTextField)),
                React.createElement(CommandFormField, {
                    value: (c: TestCommand) => c.email,
                    title: 'Email'
                }, React.createElement(SimpleTextField))
            ),
            { wrapper: context.createWrapper() }
        );
        container = result.container;
    });

    it("should render all fields", () => {
        const inputs = container.querySelectorAll('input');
        inputs.should.have.lengthOf(2);
    });

    it("should render fields in single column layout", () => {
        const fieldsContainer = container.querySelector('[style*="flex-direction: column"]');
        fieldsContainer.should.not.be.null;
    });
}));
