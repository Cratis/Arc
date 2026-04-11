// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { inject } from 'tsyringe';
import { Constructor } from '@cratis/fundamentals';
import { WellKnownBindings } from './WellKnownBindings';
import { isUserDefinedClass } from './isUserDefinedClass';

/**
 * Metadata key used to store the query params type on a view model constructor.
 */
export const queryParamsTypeKey = '__queryParamsType';

/**
 * Parameter decorator for injecting typed query params into a view model constructor.
 * Wraps tsyringe's `@inject(WellKnownBindings.queryParams)` and additionally stores the
 * parameter's constructor type on the view model class as `__queryParamsType` so that
 * `withViewModel` can deserialize the string-based URL query params into the correct types.
 * @param target The view model class constructor.
 * @param propertyKey The method name (undefined for constructor parameters).
 * @param parameterIndex The zero-based index of the parameter in the constructor.
 */
export function queryParams(target: object, propertyKey: string | symbol | undefined, parameterIndex: number): void {
    const paramTypes = Reflect.getMetadata('design:paramtypes', target) as Constructor[] | undefined;
    if (paramTypes && parameterIndex < paramTypes.length) {
        const paramType = paramTypes[parameterIndex];
        if (paramType && isUserDefinedClass(paramType)) {
            (target as Record<string, Constructor>)[queryParamsTypeKey] = paramType;
        }
    }
    inject(WellKnownBindings.queryParams)(target, propertyKey, parameterIndex);
}
