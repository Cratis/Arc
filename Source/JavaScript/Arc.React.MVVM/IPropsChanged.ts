// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** Opt-in notification after changed props commit. Not called on initial mount; does not replace IHandleProps. */
export interface IPropsChanged<TProps = object> {
    /** Receives the new props and previous committed props, using Arc deep equality. */
    propsChanged(nextProps: TProps, previousProps: TProps): void;
}
