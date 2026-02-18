// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { asCommandFormField, WrappedFieldProps } from '../asCommandFormField';
import { CommandFormContext } from '../CommandFormContext';
import { TestCommand } from './TestCommand';

interface TestFieldProps extends WrappedFieldProps<string> {
    placeholder?: string;
}

const TestFieldComponent = (props: TestFieldProps) => (
    <input
        type="text"
        value={props.value}
        onChange={(e) => props.onChange(e.target.value)}
        placeholder={props.placeholder}
        data-testid="test-field"
        data-required={props.required}
        data-invalid={props.invalid}
    />
);

const TestField = asCommandFormField<TestFieldProps>(TestFieldComponent, {
    defaultValue: '',
    extractValue: (e) => typeof e === 'string' ? e : (e as { target: { value: string } }).target.value
});

describe('when field has no property descriptor', () => {
    let requiredAttribute: string | null = null;

    beforeEach(() => {
        const command = new TestCommand();

        const contextValue = {
            commandInstance: command,
            commandVersion: 0,
            // eslint-disable-next-line @typescript-eslint/no-empty-function
            setCommandValues: () => {},
            // eslint-disable-next-line @typescript-eslint/no-empty-function
            setCommandResult: () => {},
            // eslint-disable-next-line @typescript-eslint/no-empty-function
            setCustomFieldError: () => {},
            getFieldError: () => undefined,
            customFieldErrors: {},
            showTitles: true,
            showErrors: true,
            fieldContainerComponent: undefined,
            onFieldValidate: undefined,
            onFieldChange: undefined
        };

        const { container } = render(
            <CommandFormContext.Provider value={contextValue}>
                <TestField<TestCommand>
                    value={(c) => c.requiredField}
                    currentValue=""
                    fieldName="requiredField"
                />
            </CommandFormContext.Provider>
        );

        const field = container.querySelector('[data-testid="test-field"]');
        requiredAttribute = field?.getAttribute('data-required') ?? null;
    });

    it('should default required to true', () => requiredAttribute!.should.equal('true'));
});
