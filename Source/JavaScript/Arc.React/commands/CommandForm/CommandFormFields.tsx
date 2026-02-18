// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCommandFormContext } from './CommandFormContext';
import React from 'react';
import type { CommandFormFieldProps } from './CommandFormField';
import type { ICommandResult } from '@cratis/arc/commands';

export interface ColumnInfo {
    fields: React.ReactElement<CommandFormFieldProps<unknown>>[];
}

export interface CommandFormFieldsProps {
    fields?: React.ReactElement<CommandFormFieldProps<unknown>>[];
    columns?: ColumnInfo[];
    orderedChildren?: Array<{ type: 'field' | 'other', content: React.ReactNode, index: number }>;
}

// Separate component for each field to prevent re-rendering all fields
const CommandFormFieldWrapper = ({ field }: { field: React.ReactElement<CommandFormFieldProps<unknown>> }) => {
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

    const FieldContainer = context.fieldContainerComponent;

    const fieldContent = (
        <>
            {context.showTitles && fieldProps.title && (
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
            <div style={{ display: 'flex', width: '100%' }}>
                {fieldProps.icon && (
                    <span
                        title={fieldProps.description}
                        style={{
                            cursor: fieldProps.description ? 'help' : 'default',
                            display: 'flex',
                            alignItems: 'center',
                            padding: '0.5rem',
                            backgroundColor: 'var(--color-background-secondary)',
                            border: '1px solid var(--color-border)',
                            borderRight: 'none',
                            borderRadius: 'var(--radius-md) 0 0 var(--radius-md)'
                        }}
                    >
                        {fieldProps.icon}
                    </span>
                )}
                {clonedField}
            </div>
            {context.showErrors && errorMessage && (
                <small style={{ display: 'block', marginTop: '0.25rem', color: 'var(--color-error, #c00)', fontSize: '0.875rem' }}>{errorMessage}</small>
            )}
        </>
    );

    if (FieldContainer) {
        return (
            <FieldContainer
                title={fieldProps.title}
                errorMessage={errorMessage}
            >
                {fieldContent}
            </FieldContainer>
        );
    }

    return (
        <div className="w-full" style={{ marginBottom: '1rem' }}>
            {fieldContent}
        </div>
    );
};

CommandFormFieldWrapper.displayName = 'CommandFormFieldWrapper';

export const CommandFormFields = (props: CommandFormFieldsProps) => {
    const { fields, columns, orderedChildren } = props;

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
                                />
                            );
                        })}
                    </div>
                ))}
            </div>
        );
    }

    // If we have ordered children, use them to render in the correct order
    if (orderedChildren && orderedChildren.length > 0) {
        return (
            <div style={{ display: 'flex', flexDirection: 'column', width: '100%' }}>
                {orderedChildren.map((item) => {
                    if (item.type === 'field') {
                        const field = item.content as React.ReactElement<CommandFormFieldProps<unknown>>;
                        const fieldProps = field.props as CommandFormFieldProps<unknown>;
                        const propertyAccessor = fieldProps.value;
                        const propertyName = propertyAccessor ? getPropertyName(propertyAccessor) : `field-${item.index}`;

                        return (
                            <CommandFormFieldWrapper
                                key={propertyName}
                                field={field}
                            />
                        );
                    } else {
                        return (
                            <React.Fragment key={`other-${item.index}`}>
                                {item.content}
                            </React.Fragment>
                        );
                    }
                })}
            </div>
        );
    }

    // Fallback: Render fields only (single column layout)
    return (
        <div style={{ display: 'flex', flexDirection: 'column', width: '100%' }}>
            {(fields || []).map((field, index) => {
                const fieldProps = field.props as CommandFormFieldProps<unknown>;
                const propertyAccessor = fieldProps.value;
                const propertyName = propertyAccessor ? getPropertyName(propertyAccessor) : `field-${index}`;

                return (
                    <CommandFormFieldWrapper
                        key={propertyName}
                        field={field}
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

CommandFormFields.displayName = 'CommandFormFields';
