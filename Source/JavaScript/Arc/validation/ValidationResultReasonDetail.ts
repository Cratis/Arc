// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Holds well-known finer identities within a validation result's reason.
 */
export const ValidationResultReasonDetail = {
    /**
     * The client found a missing required command property. Used with `rule`.
     * The message is framework-authored; map it to your own user-facing copy.
     */
    Required: 'Required'
} as const;
