// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { asCommandFormField, WrappedFieldProps } from '../../asCommandFormField.js';
import { CommandFormContext } from '../../CommandFormContext.js';
import { TestCommand } from '../TestCommand.js';

const ErrorsField = asCommandFormField<WrappedFieldProps<string>>(
    (props) => <input data-testid="test-field" data-errors={JSON.stringify(props.errors)} data-invalid={props.invalid} />,
    { defaultValue: '' },
);

/**
 * Renders a bound field inside a command form context and returns the errors the field component received.
 * @param serverError What the form's getFieldError yields for the field.
 * @param customFieldErrors The custom field errors held by the form.
 */
export function renderFieldWithErrors(serverError: string | undefined, customFieldErrors: Record<string, string>) {
    const command = new TestCommand();
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
        getFieldError: () => serverError,
        customFieldErrors,
        isValid: true,
        isAuthorized: true,
        isExecuting: false,
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
        onFieldChange: undefined,
        // eslint-disable-next-line @typescript-eslint/no-empty-function
        markUserInteracted: () => {},
        beginSilentValidation: () => 0,
        setSilentValidationResult: () => true,
    };

    const { container } = render(
        <CommandFormContext.Provider value={contextValue}>
            <ErrorsField<TestCommand> value={(c) => c.requiredField} currentValue="" fieldName="requiredField" />
        </CommandFormContext.Provider>,
    );

    const field = container.querySelector('[data-testid="test-field"]')!;
    return JSON.parse(field.getAttribute('data-errors')!) as string[];
}
