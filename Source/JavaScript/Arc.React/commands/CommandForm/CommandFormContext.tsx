// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createContext, useContext } from 'react';
import type { Constructor } from '@cratis/fundamentals';
import type { SetCommandValues } from '../useCommand.js';
import type { ICommandResult } from '@cratis/arc/commands';
import type { CommandFormValidationChannel, CommandFormValidationState } from './commandFormValidationState.js';

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

export interface FieldValidationInfo {
    isValid: boolean;
    errors: string[];
}

/**
 * The observable state of a command form, as a parent outside the form sees it.
 */
export interface CommandFormState {
    /**
     * Whether at least one execution of the command is in flight. Overlapping executions are counted,
     * so this stays true until the last one has settled.
     */
    isExecuting: boolean;

    /**
     * Whether the form currently passes silent validation and has no nonempty custom field errors.
     */
    isValid: boolean;

    /**
     * Whether the current identity holds at least one of the roles the command requires.
     */
    isAuthorized: boolean;

    /**
     * What validation has concluded, per channel, and how fresh that is. Built-in forms always supply it;
     * treat an absent value as unknown, which is what a custom or mocked state has to say.
     * A current, valid verdict is a preflight: it never guarantees the command will execute successfully.
     */
    validation?: CommandFormValidationState;
}

/**
 * The imperative surface a parent reaches through the `formRef` prop. The state members are getters
 * reading live values, so a handle captured once never goes stale - but they are not reactive. Use the
 * `onStateChange` prop to re-render on a change.
 */
export interface CommandFormHandle extends CommandFormState {
    /**
     * Executes the command the same way submitting the form does. Nonempty custom field errors
     * return a native validation failure before transformation or command execution.
     */
    execute(): Promise<ICommandResult<unknown>>;
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
    isAuthorized: boolean;

    /** See {@link CommandFormState.validation}. */
    validation?: CommandFormValidationState;

    /**
     * Whether at least one execution of the command is in flight. Executions are counted rather than
     * flagged, so overlapping submissions keep this true until the last one has settled.
     */
    isExecuting: boolean;
    /**
     * Claims a token for a silent validation that is about to be issued. Several validations are
     * legitimately in flight at once, so hand the token back to {@link setSilentValidationResult} and a
     * result that a later one has already overtaken is discarded instead of overwriting it.
     * @param channel The channel the validation is expected to run on; it is what `validation` reports
     * as pending meanwhile. Defaults to the client.
     */
    beginSilentValidation: (channel?: CommandFormValidationChannel) => number;

    /**
     * Applies a silent validation result, which is what drives {@link isValid}.
     * @param result The result to apply.
     * @param issue The token claimed from {@link beginSilentValidation} before the validation was issued.
     * Omitting it applies the result unconditionally and marks every validation still in flight as stale.
     * @returns True when the result was applied, false when a later one had already been.
     */
    setSilentValidationResult: (result: ICommandResult<unknown>, issue?: number) => boolean;

    /**
     * Reports that the validation issued under a token threw, so its channel records an `error` outcome
     * instead of staying pending. Optional: a custom or mocked context may not supply it.
     * @param issue The token claimed from {@link beginSilentValidation}.
     */
    failSilentValidation?: (issue: number) => void;
    onFieldValidate?: (command: TCommand, fieldName: string, oldValue: unknown, newValue: unknown) => string | undefined;
    onFieldChange?: (command: TCommand, fieldName: string, oldValue: unknown, newValue: unknown, validationInfo?: FieldValidationInfo) => void;
    onBeforeExecute?: BeforeExecuteCallback<TCommand>;
    onExecute?: () => Promise<ICommandResult<unknown>>;
    customFieldErrors: Record<string, string>;
    /**
     * Sets a form-local validation error, immediately blocking submission, onExecute and formRef.execute.
     * Undefined or an empty string clears it, matching field error display. A blocked attempt reports
     * a native validation failure through commandResult, its return value, onFailed and onValidationFailure.
     * Editing errors updates the displayed custom failure; clearing the last restores the retained
     * native result (or undefined). A subsequent setCommandResult replaces the displayed result.
     * Native field feedback and silent validation remain separate and are not discarded when custom
     * errors clear. Raw command.execute is unaffected.
     */
    setCustomFieldError: (fieldName: string, error: string | undefined) => void;
    showTitles: boolean;
    showErrors: boolean;
    validateOn: 'blur' | 'change' | 'both';
    validateAllFieldsOnChange: boolean;
    validateOnInit: boolean;
    autoServerValidate: boolean;
    autoServerValidateThrottle: number;
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

/**
 * Whether the command form this is used within currently has an execution in flight. Intended for
 * anything inside the form that has to reflect execution - a submit button that disables itself, a
 * spinner - without threading state down by hand.
 * @returns True while at least one execution is in flight.
 */
export const useIsCommandExecuting = (): boolean => useCommandFormContext().isExecuting;
