// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Props for the CommandFormField marker component.
 *
 * The value accessor uses a method declaration for bivariant type checking,
 * allowing `(c: MyCommand) => c.name` to be passed via React.createElement
 * without explicit generic arguments.
 * For full type safety in JSX, supply the command type explicitly:
 *
 *   <CommandFormField<MyCommand> value={c => c.name} />
 */
export interface CommandFormFieldProps<TCommand = unknown> {
    icon?: React.ReactElement;
    /** Accessor function that selects a property on the command, e.g. c => c.name */
    value?(instance: TCommand): unknown;
    /** Current value for the property (injected by CommandFormFields) */
    currentValue?: unknown;
    /** Called when the field value changes (injected by CommandFormFields) */
    onValueChange?: (value: unknown) => void;
    onChange?: (value: unknown) => void;
    required?: boolean;
    title?: string;
    description?: string;
    propertyDescriptor?: unknown;
    fieldName?: string;
}

export const CommandFormField = <TCommand = unknown,>(_props: CommandFormFieldProps<TCommand>) => {
    void _props;
    return <></>;
};

CommandFormField.displayName = 'CommandFormField';
