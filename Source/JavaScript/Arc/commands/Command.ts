// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ICommand, PropertyChanged } from './ICommand';
import { CommandResult } from "./CommandResult";
import { CommandValidator } from './CommandValidator';
import { Constructor, JsonSerializer } from '@cratis/fundamentals';
import { Globals } from '../Globals';
import { joinPaths } from '../joinPaths';
import { UrlHelpers } from '../UrlHelpers';
import { GetHttpHeaders } from '../GetHttpHeaders';
import { PropertyDescriptor } from '../reflection/PropertyDescriptor';
import { ValidationResult } from '../validation/ValidationResult';
import { ValidationResultSeverity } from '../validation/ValidationResultSeverity';

type Callback = {
    callback: WeakRef<PropertyChanged>;
    thisArg: WeakRef<object>;
}

/**
 * Represents an implementation of {@link ICommand} that works with HTTP fetch.
 */
export abstract class Command<TCommandContent = object, TCommandResponse = object> implements ICommand<TCommandContent, TCommandResponse> {
    private _microservice: string;
    private _apiBasePath: string;
    private _origin: string;
    private _httpHeadersCallback: GetHttpHeaders;
    abstract readonly route: string;
    /* eslint-disable @typescript-eslint/no-explicit-any */
    readonly validation?: CommandValidator<any>;
    /* eslint-enable @typescript-eslint/no-explicit-any */
    abstract readonly propertyDescriptors: PropertyDescriptor[];
    abstract get requestParameters(): string[];

    private _initialValues: object = {};
    private _hasChanges = false;
    private _callbacks: Callback[] = [];

    /**
     * Initializes a new instance of the {@link Command<,>} class.
     * @param _responseType Type of response.
     * @param _isResponseTypeEnumerable Whether or not the response type is enumerable.
     */
    constructor(readonly _responseType: Constructor = Object, readonly _isResponseTypeEnumerable: boolean) {
        this._microservice = Globals.microservice ?? '';
        this._apiBasePath = Globals.apiBasePath ?? '';
        this._origin = Globals.origin ?? '';
        this._httpHeadersCallback = () => ({});
    }

    /** @inheritdoc */
    setMicroservice(microservice: string) {
        this._microservice = microservice;
    }

    /** @inheritdoc */
    setApiBasePath(apiBasePath: string): void {
        this._apiBasePath = apiBasePath;
    }

    /** @inheritdoc */
    setOrigin(origin: string): void {
        this._origin = origin;
    }

    /** @inheritdoc */
    setHttpHeadersCallback(callback: GetHttpHeaders): void {
        this._httpHeadersCallback = callback;
    }

    /** @inheritdoc */
    async execute(allowedSeverity?: ValidationResultSeverity, ignoreWarnings?: boolean): Promise<CommandResult<TCommandResponse>> {
        const clientValidationErrors = this.validation?.validate(this) || [];
        const filteredClientErrors = this.filterValidationResultsBySeverity(clientValidationErrors, allowedSeverity, ignoreWarnings);
        if (filteredClientErrors.length > 0) {
            return CommandResult.validationFailed(filteredClientErrors) as CommandResult<TCommandResponse>;
        }

        const validationErrors = this.validateRequiredProperties();
        const filteredRequiredErrors = this.filterValidationResultsBySeverity(validationErrors, allowedSeverity, ignoreWarnings);
        if (filteredRequiredErrors.length > 0) {
            return CommandResult.validationFailed(filteredRequiredErrors) as CommandResult<TCommandResponse>;
        }

        let actualRoute = this.route;

        if (this.requestParameters && this.requestParameters.length > 0) {
            const payload = this.buildPayload();
            const { route } = UrlHelpers.replaceRouteParameters(this.route, payload);
            actualRoute = route;
        }

        const result = await this.performRequest(actualRoute, 'Command not found at route', 'Error during server call', allowedSeverity, ignoreWarnings);
        this.setInitialValuesFromCurrentValues();
        return result;
    }

    /** @inheritdoc */
    async validate(): Promise<CommandResult<TCommandResponse>> {
        const clientValidationErrors = this.validation?.validate(this) || [];
        if (clientValidationErrors.length > 0) {
            return CommandResult.validationFailed(clientValidationErrors) as CommandResult<TCommandResponse>;
        }

        const validationErrors = this.validateRequiredProperties();
        if (validationErrors.length > 0) {
            return CommandResult.validationFailed(validationErrors) as CommandResult<TCommandResponse>;
        }

        let actualRoute = this.route;

        if (this.requestParameters && this.requestParameters.length > 0) {
            const payload = this.buildPayload();
            const { route } = UrlHelpers.replaceRouteParameters(this.route, payload);
            actualRoute = route;
        }

        actualRoute = `${actualRoute}/validate`;
        return this.performRequest(actualRoute, 'Command validation endpoint not found at route', 'Error during validation call');
    }

    private buildPayload(): object {
        const payload = {};
        this.propertyDescriptors.forEach(propertyDescriptor => {
            const property = propertyDescriptor.name;
            payload[property] = this[property];
        });
        return payload;
    }

    private validateRequiredProperties(): ValidationResult[] {
        const validationErrors: ValidationResult[] = [];
        this.propertyDescriptors.forEach(propertyDescriptor => {
            if (!propertyDescriptor.isOptional && !this.requestParameters.includes(propertyDescriptor.name)) {
                const value = this[propertyDescriptor.name];
                if (value === undefined || value === null || value === '') {
                    validationErrors.push(new ValidationResult(
                        ValidationResultSeverity.Error,
                        `${propertyDescriptor.name} is required`,
                        [propertyDescriptor.name],
                        null
                    ));
                }
            }
        });
        return validationErrors;
    }

    private filterValidationResultsBySeverity(validationResults: ValidationResult[], allowedSeverity?: ValidationResultSeverity, ignoreWarnings?: boolean): ValidationResult[] {
        if (ignoreWarnings === true) {
            return validationResults.filter(result => result.severity === ValidationResultSeverity.Error);
        }
        if (allowedSeverity === undefined) {
            return validationResults.filter(result => result.severity === ValidationResultSeverity.Error);
        }
        return validationResults.filter(result => result.severity > allowedSeverity);
    }

    private buildHeaders(allowedSeverity?: ValidationResultSeverity, ignoreWarnings?: boolean): HeadersInit {
        const customHeaders = this._httpHeadersCallback?.() ?? {};
        const headers = {
            ...customHeaders,
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        };

        if (this._microservice?.length > 0) {
            headers[Globals.microserviceHttpHeader] = this._microservice;
        }

        if (allowedSeverity !== undefined) {
            headers['X-Allowed-Severity'] = allowedSeverity.toString();
        }

        if (ignoreWarnings === true) {
            headers['X-Ignore-Warnings'] = 'true';
        }

        return headers;
    }

    private async performRequest(
        route: string,
        notFoundMessage: string,
        errorMessage: string,
        allowedSeverity?: ValidationResultSeverity,
        ignoreWarnings?: boolean
    ): Promise<CommandResult<TCommandResponse>> {
        const payload = this.buildPayload();
        const headers = this.buildHeaders(allowedSeverity, ignoreWarnings);
        const actualRoute = joinPaths(this._apiBasePath, route);
        const url = UrlHelpers.createUrlFrom(this._origin, this._apiBasePath, actualRoute);

        try {
            const response = await fetch(url, {
                method: 'POST',
                headers,
                body: JsonSerializer.serialize(payload)
            });

            if (response.status === 404) {
                return CommandResult.failed([`${notFoundMessage} '${actualRoute}'`]) as CommandResult<TCommandResponse>;
            }

            const result = await response.json();
            return new CommandResult(result, this._responseType, this._isResponseTypeEnumerable);
        } catch (ex) {
            return CommandResult.failed([`${errorMessage}: ${ex}`]) as CommandResult<TCommandResponse>;
        }
    }

    /** @inheritdoc */
    clear(): void {
        this.propertyDescriptors.forEach(propertyDescriptor => {
            const property = propertyDescriptor.name;
            this[property] = undefined;
        });
        this._initialValues = {};
        this._hasChanges = false;
    }

    /** @inheritdoc */
    setInitialValues(values: TCommandContent) {
        this.propertyDescriptors.forEach(propertyDescriptor => {
            const property = propertyDescriptor.name;
            if (Object.prototype.hasOwnProperty.call(values, property)) {
                this._initialValues[property] = values[property];
                this[property] = values[property];
            }
        });
        this.updateHasChanges();
    }

    /** @inheritdoc */
    setInitialValuesFromCurrentValues() {
        this.propertyDescriptors.forEach(propertyDescriptor => {
            const property = propertyDescriptor.name;
            if (this[property]) {
                this._initialValues[property] = this[property];
            }
        });
        this.updateHasChanges();
    }

    /** @inheritdoc */
    revertChanges(): void {
        this.propertyDescriptors.forEach(propertyDescriptor => {
            const property = propertyDescriptor.name;
            this[property] = this._initialValues[property];
        });
    }

    /** @inheritdoc */
    get hasChanges() {
        return this._hasChanges;
    }

    /** @inheritdoc */
    propertyChanged(property: string) {
        this.updateHasChanges();

        this._callbacks.forEach(callbackContainer => {
            const callback = callbackContainer.callback.deref();
            const thisArg = callbackContainer.thisArg.deref();
            if (callback && thisArg) {
                callback.call(thisArg, property);
            } else {
                this._callbacks = this._callbacks.filter(_ => _.callback !== callbackContainer.callback);
            }
        });
    }

    /** @inheritdoc */
    onPropertyChanged(callback: PropertyChanged, thisArg: object) {
        this._callbacks.push({
            callback: new WeakRef(callback),
            thisArg: new WeakRef(thisArg)
        });
    }

    private updateHasChanges() {
        this._hasChanges = this.propertyDescriptors.some(propertyDescriptor => {
            const property = propertyDescriptor.name;
            return this[property] !== this._initialValues[property];
        });
    }
}
