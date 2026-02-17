// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ICanBeConfigured } from '../ICanBeConfigured';
import { CommandResult } from './CommandResult';
import { PropertyDescriptor } from '../reflection/PropertyDescriptor';
import { ValidationResultSeverity } from '../validation/ValidationResultSeverity';

/**
 * Callback for when a property changes.
 */
export type PropertyChanged = (property: string) => void;

/**
 * Defines the base of a command.
 */
export interface ICommand<TCommandContent = object, TCommandResponse = object> extends ICanBeConfigured {
    /**
     * Gets the route information for the command.
     */
    readonly route: string;

    /**
     * Gets the property descriptors for the command.
     */
    readonly propertyDescriptors: PropertyDescriptor[];

    /**
     * Execute the {@link ICommand}.
     * @param [allowedSeverity] Optional maximum severity level to allow. Validation results with severity higher than this will cause the command to fail.
     * @param [ignoreWarnings] Optional flag to ignore warnings. When true, only errors will cause the command to fail.
     * @returns {CommandResult} for the execution.
     */
    execute(allowedSeverity?: ValidationResultSeverity, ignoreWarnings?: boolean): Promise<CommandResult<TCommandResponse>>;

    /**
     * Validate the {@link ICommand} without executing it.
     * @returns {CommandResult} for the validation containing authorization and validation status.
     * @remarks
     * This method performs authorization and validation checks on the server without executing the command handler.
     * Use this for pre-flight validation to provide early feedback to users.
     */
    validate(): Promise<CommandResult<TCommandResponse>>;

    /**
     * Clear the command properties and reset them to their default values. This will also clear the initial values.
     * This is used when the command is not needed anymore and should be cleared.
     */
    clear(): void;

    /**
     * Set the initial values for the command. This is used for tracking if there are changes to a command or not.
     * @param {*} values Values to set.
     */
    setInitialValues(values: TCommandContent): void;

    /**
     * Set the initial values for the command to be the current value of the properties.
     */
    setInitialValuesFromCurrentValues(): void;

    /**
     * Revert any changes on the command.
     */
    revertChanges(): void;

    /**
     * Gets whether or not there are changes to any properties.
     */
    readonly hasChanges: boolean;

    /**
     * Notify about a property that has had its value changed.
     * @param {string} property Name of property that changes.
     */
    propertyChanged(property: string): void;

    /**
     * Register callback that gets called when a property changes.
     * @param {PropertyChanged} callback Callback to register.
     * @param {*} thisArg The this arg to use when calling.
     */
    onPropertyChanged(callback: PropertyChanged, thisArg: object): void;
}
