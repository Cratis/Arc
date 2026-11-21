// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** Opt-in notification after changed props commit. Not called on initial mount; does not replace IHandleProps. */
export interface IPropsChanged<TProps = object> {
    /** Receives new props and the previous shallow committed snapshot; compares own enumerable keys with Object.is. */
    propsChanged(nextProps: TProps, previousProps: TProps): void;
}
