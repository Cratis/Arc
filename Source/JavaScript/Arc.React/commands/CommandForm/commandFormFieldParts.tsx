// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import type { CommandFormContextValue } from './CommandFormContext.js';
import type { CommandFormFieldProps } from './CommandFormField.js';
import type { CommandFormMarked } from './commandFormMarkers.js';

/** Extracts the property name from an accessor function such as `command => command.property`. */
export function getPropertyName<T>(accessor: (obj: T) => unknown): string {
    const fnStr = accessor.toString();
    const match = fnStr.match(/\.([a-zA-Z_$][a-zA-Z0-9_$]*)/);
    return match ? match[1] : '';
}

/** Warns that a field could not be bound to a command property. */
export const warnAboutUnboundField = (field: React.ReactElement<CommandFormFieldProps>): void => {
    const component = field.type as React.ComponentType<unknown> & {
        displayName?: string;
        commandFormFieldName?: string;
    };
    const componentName =
        component.commandFormFieldName ||
        component.displayName ||
        component.name ||
        'CommandFormField';
    console.warn(
        `CommandForm field '${componentName}' could not resolve a command property from its value accessor and remains unbound. ` +
            'Pass an accessor such as `command => command.property`.',
    );
};

/** Whether the field is required: an explicit prop wins, otherwise the descriptor decides. */
export const isFieldRequired = (
    fieldProps: CommandFormFieldProps,
    propertyDescriptor: Record<string, unknown> | undefined,
): boolean =>
    fieldProps.required ??
    (propertyDescriptor ? !(propertyDescriptor as { isOptional?: boolean }).isOptional : true);

/** Finds the property descriptor for a property on the command instance. */
export const findPropertyDescriptor = (
    commandInstance: unknown,
    propertyName: string,
): Record<string, unknown> | undefined => {
    const descriptors = (commandInstance as Record<string, unknown>)?.propertyDescriptors;
    return propertyName && descriptors
        ? (descriptors as Array<Record<string, unknown>>).find((pd) => pd.name === propertyName)
        : undefined;
};

const titleStyle: React.CSSProperties = {
    display: 'block',
    marginBottom: '0.5rem',
    fontWeight: 500,
    color: 'var(--color-text)',
};

const FieldTitle = ({
    title,
    fieldId,
    titleId,
    isGroup,
}: {
    title: string;
    fieldId: string;
    titleId: string;
    isGroup: boolean;
}) =>
    isGroup ? (
        <span id={titleId} style={titleStyle}>{title}</span>
    ) : (
        <label htmlFor={fieldId} style={titleStyle}>{title}</label>
    );

/** Renders the title (when shown) and decorated field, grouped with a role when the field is a group. */
export const FieldContent = ({
    fieldProps,
    fieldId,
    groupRole,
    showTitles,
    decoratedField,
}: {
    fieldProps: CommandFormFieldProps;
    fieldId: string;
    groupRole: CommandFormMarked['commandFormFieldGroupRole'];
    showTitles: boolean;
    decoratedField: React.ReactNode;
}) => {
    const titleId = `${fieldId}-title`;
    const title = showTitles && fieldProps.title && (
        <FieldTitle title={fieldProps.title} fieldId={fieldId} titleId={titleId} isGroup={!!groupRole} />
    );

    return groupRole && title ? (
        <div role={groupRole} aria-labelledby={titleId}>
            {title}
            {decoratedField}
        </div>
    ) : (
        <>
            {title}
            {decoratedField}
        </>
    );
};

/** Renders the validation error of a field, via the custom error display when one is provided. */
export const FieldError = ({
    context,
    errorMessage,
    propertyName,
}: {
    context: CommandFormContextValue<unknown>;
    errorMessage: string | undefined;
    propertyName: string;
}) => {
    const ErrorDisplay = context.errorDisplayComponent;
    const errors = errorMessage ? [errorMessage] : [];
    if (!context.showErrors || errors.length === 0) {
        return null;
    }

    if (ErrorDisplay) {
        return <ErrorDisplay errors={errors} fieldName={propertyName} />;
    }

    return (
        <small
            className={context.errorClassName || 'p-error'}
            style={{
                display: 'block',
                marginTop: '0.25rem',
                color: 'var(--color-error, #c00)',
                fontSize: '0.875rem',
            }}
        >
            {errorMessage}
        </small>
    );
};
