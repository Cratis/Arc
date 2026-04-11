// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { inject } from 'tsyringe';
import { Constructor } from '@cratis/fundamentals';
import { WellKnownBindings } from './WellKnownBindings';
import { isUserDefinedClass } from './isUserDefinedClass';

/**
 * Metadata key used to store the props type on a view model constructor.
 */
export const propsTypeKey = '__propsType';

/**
 * Parameter decorator for injecting typed props into a view model constructor.
 * Wraps tsyringe's `@inject(WellKnownBindings.props)` and additionally stores the
 * parameter's constructor type on the view model class as `__propsType`.
 * @param target The view model class constructor.
 * @param propertyKey The method name (undefined for constructor parameters).
 * @param parameterIndex The zero-based index of the parameter in the constructor.
 */
export function props(target: object, propertyKey: string | symbol | undefined, parameterIndex: number): void {
    const paramTypes = Reflect.getMetadata('design:paramtypes', target) as Constructor[] | undefined;
    if (paramTypes && parameterIndex < paramTypes.length) {
        const paramType = paramTypes[parameterIndex];
        if (paramType && isUserDefinedClass(paramType)) {
            (target as Record<string, Constructor>)[propsTypeKey] = paramType;
        }
    }
    inject(WellKnownBindings.props)(target, propertyKey, parameterIndex);
}
