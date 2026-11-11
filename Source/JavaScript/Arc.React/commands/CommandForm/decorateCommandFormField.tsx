// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import type { CommandFormContextValue } from './CommandFormContext.js';
import type { CommandFormFieldProps } from './CommandFormField.js';
import { CommandFormFieldBinding } from './commandFormFieldBindingContext.js';

const leftAddonStyle: React.CSSProperties = {
    display: 'flex',
    alignItems: 'center',
    padding: '0.75rem',
    backgroundColor: 'var(--color-background-secondary)',
    border: '1px solid var(--color-border)',
    borderRight: 'none',
    borderRadius: 'var(--radius-md) 0 0 var(--radius-md)',
};

/** When there's an icon, set hasLeftAddon on the field to remove its left border-radius. */
function withLeftAddon(clonedField: React.ReactNode, hasIcon: boolean): React.ReactNode {
    if (!hasIcon) {
        return clonedField;
    }

    return React.cloneElement(clonedField as React.ReactElement, {
        hasLeftAddon: true,
        style: {
            ...((clonedField as React.ReactElement).props?.style || {}),
            flex: 1,
        },
    });
}

function decorateWithCustomDecorator(
    clonedField: React.ReactNode,
    fieldProps: CommandFormFieldProps,
    FieldDecorator: NonNullable<CommandFormContextValue<unknown>['fieldDecoratorComponent']>,
): React.ReactNode {
    return (
        <FieldDecorator icon={fieldProps.icon} description={fieldProps.description}>
            <CommandFormFieldBinding>
                {withLeftAddon(clonedField, !!fieldProps.icon)}
            </CommandFormFieldBinding>
        </FieldDecorator>
    );
}

function wrapWithDescription(
    wrappedField: React.ReactNode,
    description: string,
    TooltipWrapper: CommandFormContextValue<unknown>['tooltipComponent'],
): React.ReactNode {
    if (TooltipWrapper) {
        return <TooltipWrapper description={description}>{wrappedField}</TooltipWrapper>;
    }

    return (
        <div title={description} style={{ cursor: 'help' }}>
            {wrappedField}
        </div>
    );
}

function decorateWithDefaults(
    clonedField: React.ReactNode,
    fieldProps: CommandFormFieldProps,
    context: CommandFormContextValue<unknown>,
): React.ReactNode {
    // Default decoration: icon addon and tooltip
    const iconAddon = fieldProps.icon && (
        <span className={context.iconAddonClassName || 'p-inputgroup-addon'} style={leftAddonStyle}>
            {fieldProps.icon}
        </span>
    );

    const wrappedField = (
        <div style={{ display: 'flex', width: '100%', alignItems: 'stretch' }}>
            {iconAddon}
            <CommandFormFieldBinding>
                {withLeftAddon(clonedField, !!fieldProps.icon)}
            </CommandFormFieldBinding>
        </div>
    );

    return fieldProps.description
        ? wrapWithDescription(wrappedField, fieldProps.description, context.tooltipComponent)
        : wrappedField;
}

/**
 * Decorates a cloned command form field with its icon and description.
 *
 * Wraps only the cloned field in the private binding marker. Runtime-capable fields use this
 * framework-owned scope rather than public callback props to decide whether to self-wrap.
 * @param {React.ReactNode} clonedField The cloned field element.
 * @param {CommandFormFieldProps} fieldProps The props of the original field.
 * @param {CommandFormContextValue} context The command form context.
 * @returns {React.ReactNode} The decorated field.
 */
export function decorateCommandFormField(
    clonedField: React.ReactNode,
    fieldProps: CommandFormFieldProps,
    context: CommandFormContextValue<unknown>,
): React.ReactNode {
    if (!fieldProps.icon && !fieldProps.description) {
        return <CommandFormFieldBinding>{clonedField}</CommandFormFieldBinding>;
    }

    const FieldDecorator = context.fieldDecoratorComponent;
    return FieldDecorator
        ? decorateWithCustomDecorator(clonedField, fieldProps, FieldDecorator)
        : decorateWithDefaults(clonedField, fieldProps, context);
}
