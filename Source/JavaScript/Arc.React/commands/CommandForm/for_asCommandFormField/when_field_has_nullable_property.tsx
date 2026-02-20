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

describe('when field has nullable property', () => {
    let requiredAttribute: string | null = null;

    beforeEach(() => {
        const command = new TestCommand();
        const propertyDescriptor = command.propertyDescriptors.find(pd => pd.name === 'optionalField')!;

        const contextValue = {
            command: TestCommand,
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
            isValid: true,
            // eslint-disable-next-line @typescript-eslint/no-empty-function
            setFieldValidity: () => {},
            showTitles: true,
            showErrors: true,
            validateOn: 'blur' as const,
            validateAllFieldsOnChange: false,
            validateOnInit: false,
            autoServerValidate: false,
            autoServerValidateThrottle: 500,
            fieldContainerComponent: undefined,
            onFieldValidate: undefined,
            onFieldChange: undefined
        };

        const { container } = render(
            <CommandFormContext.Provider value={contextValue}>
                <TestField<TestCommand>
                    value={(c) => c.optionalField}
                    propertyDescriptor={propertyDescriptor}
                    currentValue=""
                    fieldName="optionalField"
                />
            </CommandFormContext.Provider>
        );

        const field = container.querySelector('[data-testid="test-field"]');
        requiredAttribute = field?.getAttribute('data-required') ?? null;
    });

    it('should set required to false', () => requiredAttribute!.should.equal('false'));
});
