// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as browser from './browser';
import * as messaging from './messaging';
import * as dialogs from './dialogs';
import { observer } from 'mobx-react';
export * from './Bindings';
export * from './MVVMContext';
export * from './withViewModel';
export * from './IViewModelDetached';
export * from './WellKnownBindings';
export * from './IHandleProps';
export * from './IHandleParams';
export * from './IHandleQueryParams';
export * from './params';
export * from './queryParams';
export * from './props';

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
