// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Props for rendering safe, form-level exception feedback without raw command diagnostics.
 */
export interface ExceptionDisplayProps {
    /** The caller-provided safe message, or the default unexpected-error message. */
    message: string;
}
