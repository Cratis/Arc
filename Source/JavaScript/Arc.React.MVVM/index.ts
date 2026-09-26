// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as browser from './browser/index.js';
import * as messaging from './messaging/index.js';
import * as dialogs from './dialogs/index.js';
import { observer } from 'mobx-react';
export * from './Bindings.js';
export * from './MVVMContext.js';
export * from './withViewModel.js';
export * from './IViewModelDetached.js';
export * from './WellKnownBindings.js';
export * from './IHandleProps.js';
export * from './IHandleParams.js';
export * from './IHandleQueryParams.js';
export * from './params.js';
export * from './queryParams.js';
export * from './props.js';

/**
 * The sanctioned leaf-observer boundary for Cratis Arc MVVM.
 *
 * `withViewModel()` observes only the render of the component it wraps. A child component
 * that reads observable view model state directly — outside that boundary — will not
 * re-render when the observed state changes. Wrap such a child with `observer()` to give
 * it its own observer boundary.
 *
 * Always import `observer` from `@cratis/arc.react.mvvm`, never directly from `mobx-react`
 * or `mobx-react-lite`. This keeps the MobX binding an internal implementation detail and
 * lets Cratis Arc evolve it without breaking consumers.
 *
 * @example
 * ```tsx
 * import { observer } from '@cratis/arc.react.mvvm';
 *
 * // A presentational child that reads observable view model state directly.
 * const PartnerResults = observer(({ viewModel }: { viewModel: PartnerSearchViewModel }) => (
 *     <ul>
 *         {viewModel.filteredPartners.map(partner => <li key={partner.id}>{partner.name}</li>)}
 *     </ul>
 * ));
 * ```
 */
export { observer };

export {
    browser,
    messaging,
    dialogs
};
