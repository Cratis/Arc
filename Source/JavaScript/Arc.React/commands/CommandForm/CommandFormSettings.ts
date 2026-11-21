// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type React from 'react';
import type { FieldContainerProps, FieldDecoratorProps, ErrorDisplayProps, TooltipWrapperProps } from './CommandFormContext.js';
import type { ExceptionDisplayProps } from './ExceptionDisplayProps.js';
import type { ValidationSummaryProps } from './ValidationSummary.js';

/** Optional presentation and validation defaults shared by forms in a provider scope. */
export interface CommandFormSettings {
    showTitles?: boolean;
    /** Whether to display field errors and form-level exception feedback. Defaults to true. */
    showErrors?: boolean;
    /**
     * Safe, user-facing exception text. Defaults to 'An unexpected error occurred. Please try again.'
     * Supply localized text as needed; an explicitly empty string is preserved.
     * Never pass raw exception diagnostics as this message.
     */
    exceptionMessage?: string;
    /** Replaces unmatched validation feedback. A visible replacement must provide role='alert'; return null to suppress it. */
    validationSummaryComponent?: React.ComponentType<ValidationSummaryProps>;
    /**
     * Replaces the default exception panel and receives only the safe message.
     * Not rendered when showErrors is false; independent of the field errorDisplayComponent.
     */
    exceptionDisplayComponent?: React.ComponentType<ExceptionDisplayProps>;
    validateOn?: 'blur' | 'change' | 'both';
    validateAllFieldsOnChange?: boolean;
    validateOnInit?: boolean;
    /**
     * Whether silent/eager validation (on init, on change, on blur) should be performed against the
     * server. Defaults to false — silent validation only runs client-side rules and never contacts the
     * server. Executing the command still performs full server-side validation regardless of this flag;
     * enable this only to surface server-only rules (e.g. uniqueness checks) before submission.
     */
    autoServerValidate?: boolean;
    autoServerValidateThrottle?: number;
    fieldContainerComponent?: React.ComponentType<FieldContainerProps>;
    fieldDecoratorComponent?: React.ComponentType<FieldDecoratorProps>;
    errorDisplayComponent?: React.ComponentType<ErrorDisplayProps>;
    tooltipComponent?: React.ComponentType<TooltipWrapperProps>;
    errorClassName?: string;
    iconAddonClassName?: string;
}
