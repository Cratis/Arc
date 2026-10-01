// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface StatusProps {
    kind: 'loading' | 'error' | 'empty';
    message: string;
}

/** The one way this application says loading, failed or nothing-here. */
export const Status = ({ kind, message }: StatusProps) => (
    <p className={`status status-${kind}`} role={kind === 'error' ? 'alert' : 'status'}>
        {message}
    </p>
);
