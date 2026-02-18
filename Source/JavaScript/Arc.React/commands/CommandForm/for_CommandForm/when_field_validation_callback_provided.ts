// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, fireEvent } from '@testing-library/react';
import { CommandForm } from '../CommandForm';
// eslint-disable-next-line @typescript-eslint/no-unused-vars
import { CommandFormField } from '../CommandFormField';
import { asCommandFormField } from '../asCommandFormField';
import { TestCommand } from './TestCommand';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

const SimpleTextField = asCommandFormField<{ value: string; onChange: (value: unknown) => void; invalid: boolean; required: boolean; errors: string[] }>(
    (props) => {
        return React.createElement('input', {
            type: 'text',
            value: props.value,
            onChange: props.onChange,
            'data-testid': 'text-input'
        });
    },
    {
        defaultValue: '',
        extractValue: (e: unknown) => (e as React.ChangeEvent<HTMLInputElement>).target.value
    }
);

describe("when field validation callback provided", given(a_command_form_context, context => {
    let validationCallCount = 0;
    let lastValidatedFieldName = '';
    let lastNewValue: unknown;

    beforeEach(() => {
        validationCallCount = 0;
        lastValidatedFieldName = '';
        lastNewValue = undefined;

        const result = render(
            React.createElement(
                CommandForm,
                {
                    command: TestCommand,
                    onFieldValidate: (cmd, fieldName, _oldValue, newValue) => {
                        validationCallCount++;
                        lastValidatedFieldName = fieldName;
                        lastNewValue = newValue;
                        if (newValue === 'invalid') {
                            return 'This value is not allowed';
                        }
                        return undefined;
                    }
                },
                React.createElement(SimpleTextField, {
                    value: (c: TestCommand) => c.name,
                    title: 'Name'
                })
            ),
            { wrapper: context.createWrapper() }
        );

        const input = result.getByTestId('text-input') as HTMLInputElement;
        fireEvent.change(input, { target: { value: 'test-value' } });
    });

    it("should call validation callback", () => {
        validationCallCount.should.equal(1);
    });

    it("should pass field name to validator", () => {
        lastValidatedFieldName.should.equal('name');
    });

    it("should pass new value to validator", () => {
        lastNewValue.should.equal('test-value');
    });
}));
