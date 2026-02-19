// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createContext, useContext } from 'react';
import type { Constructor } from '@cratis/fundamentals';
import type { SetCommandValues } from '../useCommand';
import type { ICommandResult } from '@cratis/arc/commands';

export type BeforeExecuteCallback<TCommand> = (values: TCommand) => TCommand;

export interface FieldContainerProps {
    title?: string;
    errorMessage?: string;
    children: React.ReactNode;
}

export interface FieldDecoratorProps {
    icon?: React.ReactElement;
    description?: string;
    children: React.ReactNode;
}

export interface ErrorDisplayProps {
    errors: string[];
    fieldName?: string;
}

export interface TooltipWrapperProps {
    description: string;
    children: React.ReactNode;
}

export interface CommandFormContextValue<TCommand> {
    command: Constructor<TCommand>;
    commandInstance: TCommand;
    commandVersion: number;
    setCommandValues: SetCommandValues<TCommand>;
    commandResult?: ICommandResult<unknown>;
    setCommandResult: (result: ICommandResult<unknown>) => void;
    getFieldError: (propertyName: string) => string | undefined;
    isValid: boolean;
    setFieldValidity: (fieldName: string, isValid: boolean) => void;
    onFieldValidate?: (command: TCommand, fieldName: string, oldValue: unknown, newValue: unknown) => string | undefined;
    onFieldChange?: (command: TCommand, fieldName: string, oldValue: unknown, newValue: unknown) => void;
    onBeforeExecute?: BeforeExecuteCallback<TCommand>;
    customFieldErrors: Record<string, string>;
    setCustomFieldError: (fieldName: string, error: string | undefined) => void;
    showTitles: boolean;
    showErrors: boolean;
    fieldContainerComponent?: React.ComponentType<FieldContainerProps>;
    fieldDecoratorComponent?: React.ComponentType<FieldDecoratorProps>;
    errorDisplayComponent?: React.ComponentType<ErrorDisplayProps>;
    tooltipComponent?: React.ComponentType<TooltipWrapperProps>;
    errorClassName?: string;
    iconAddonClassName?: string;
}

export const CommandFormContext = createContext<CommandFormContextValue<unknown> | undefined>(undefined);

export const useCommandFormContext = <TCommand,>() => {
    const context = useContext(CommandFormContext);
    if (!context) {
        throw new Error('useCommandFormContext must be used within a CommandForm');
    }
    return context as CommandFormContextValue<TCommand>;
};
