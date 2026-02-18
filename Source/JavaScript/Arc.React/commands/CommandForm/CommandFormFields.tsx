// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCommandFormContext } from './CommandForm';
import React from 'react';
import { Tooltip } from 'primereact/tooltip';
import type { CommandFormFieldProps } from './CommandFormField';
import type { ICommandResult } from '@cratis/arc/commands';

export interface ColumnInfo {
    fields: React.ReactElement<CommandFormFieldProps<unknown>>[];
}

export interface CommandFormFieldsProps {
    fields?: React.ReactElement<CommandFormFieldProps<unknown>>[];
    columns?: ColumnInfo[];
}

// Separate component for each field to prevent re-rendering all fields
const CommandFormFieldWrapper = ({ field, index }: { field: React.ReactElement<CommandFormFieldProps<unknown>>; index: number }) => {
    const context = useCommandFormContext<unknown>();
    const fieldProps = field.props as CommandFormFieldProps<unknown>;
    const propertyAccessor = fieldProps.value;

    // Get the property name from the accessor function
    const propertyName = propertyAccessor ? getPropertyName(propertyAccessor) : '';

    // Get the current value from the command instance
    // commandVersion ensures this component re-renders when values change
    const currentValue = React.useMemo(() => {
        return propertyName ? (context.commandInstance as Record<string, unknown>)?.[propertyName] : undefined;
    }, [context.commandInstance, propertyName, context.commandVersion]);

    // Get the error message for this field, if any
    const errorMessage = propertyName ? context.getFieldError(propertyName) : undefined;

    // Get the property descriptor for this field from the command instance
    const propertyDescriptor = propertyName && (context.commandInstance as Record<string, unknown>)?.propertyDescriptors
        ? ((context.commandInstance as Record<string, unknown>).propertyDescriptors as Array<Record<string, unknown>>).find((pd: Record<string, unknown>) => pd.name === propertyName)
        : undefined;

    // Clone the field element with the current value and onChange handler
    const clonedField = React.cloneElement(field as React.ReactElement, {
        ...fieldProps,
        currentValue,
        propertyDescriptor,
        fieldName: propertyName,
        onValueChange: (value: unknown) => {
            if (propertyName) {
                const oldValue = currentValue;

                // Update the command value
                context.setCommandValues({ [propertyName]: value } as Record<string, unknown>);

                // Call validate() on the command instance and store the result
                if (context.commandInstance && typeof (context.commandInstance as Record<string, unknown>).validate === 'function') {
                    const validationResult = ((context.commandInstance as Record<string, unknown>).validate as () => ICommandResult<unknown>)();
                    if (validationResult) {
                        context.setCommandResult(validationResult);
                    }
                }

                // Call custom field validator if provided
                if (context.onFieldValidate) {
                    const validationError = context.onFieldValidate(context.commandInstance as Record<string, unknown>, propertyName, oldValue, value);
                    context.setCustomFieldError(propertyName, validationError);
                }

                // Call field change callback if provided
                if (context.onFieldChange) {
                    context.onFieldChange(context.commandInstance as Record<string, unknown>, propertyName, oldValue, value);
                }
            }
            fieldProps.onChange?.(value as unknown);
        },
        required: fieldProps.required ?? true,
        invalid: !!errorMessage
    } as Record<string, unknown>);

    const tooltipId = fieldProps.description ? `tooltip-${propertyName}-${index}` : undefined;

    return (
        <div className="w-full">
            {fieldProps.title && (
                <label 
                    style={{ 
                        display: 'block', 
                        marginBottom: '0.5rem', 
                        fontWeight: 500,
                        color: 'var(--color-text)'
                    }}
                >
                    {fieldProps.title}
                </label>
            )}
            <div className="p-inputgroup w-full">
                {fieldProps.description && (
                    <Tooltip target={`.${tooltipId}`} content={fieldProps.description} />
                )}
                {fieldProps.icon && (
                    <span className={`p-inputgroup-addon ${tooltipId || ''}`}>
                        {fieldProps.icon}
                    </span>
                )}
                {clonedField}
            </div>
            {errorMessage && (
                <small className="p-error block mt-1">{errorMessage}</small>
            )}
        </div>
    );
};

export const CommandFormFields = (props: CommandFormFieldsProps) => {
    const { fields, columns } = props;

    // Render columns if provided
    if (columns && columns.length > 0) {
        return (
            <div className="card flex flex-column md:flex-row gap-3">
                {columns.map((column, columnIndex) => (
                    <div key={`column-${columnIndex}`} className="flex flex-column gap-3 flex-1">
                            {column.fields.map((field, index) => {
                                const fieldProps = field.props as CommandFormFieldProps<unknown>;
                                const propertyAccessor = fieldProps.value;
                                const propertyName = propertyAccessor ? getPropertyName(propertyAccessor) : `field-${columnIndex}-${index}`;

                                return (
                                    <CommandFormFieldWrapper
                                        key={propertyName}
                                        field={field}
                                        index={index}
                                    />
                                );
                            })}
                    </div>
                ))}
            </div>
        );
    }

    // Render fields (single column layout)
    return (
        <div className="flex flex-col gap-4 w-full">
            {(fields || []).map((field, index) => {
                const fieldProps = field.props as CommandFormFieldProps<unknown>;
                const propertyAccessor = fieldProps.value;
                const propertyName = propertyAccessor ? getPropertyName(propertyAccessor) : `field-${index}`;

                return (
                    <CommandFormFieldWrapper
                        key={propertyName}
                        field={field}
                        index={index}
                    />
                );
            })}
        </div>
    );
};

// Helper function to extract property name from accessor function
function getPropertyName<T>(accessor: (obj: T) => unknown): string {
    const fnStr = accessor.toString();
    const match = fnStr.match(/\.([a-zA-Z_$][a-zA-Z0-9_$]*)/);
    return match ? match[1] : '';
}
