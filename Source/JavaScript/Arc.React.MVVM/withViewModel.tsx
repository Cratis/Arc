// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { container, DependencyContainer } from 'tsyringe';
import { Constructor } from '@cratis/fundamentals';
import { FunctionComponent, ReactElement, useContext, useEffect, useRef, useState } from 'react';
import { Observer } from 'mobx-react';
import { makeAutoObservable } from 'mobx';
import { useParams, useSearchParams } from 'react-router-dom';
import {
    DialogMediator,
    DialogMediatorHandler,
    Dialogs,
    IDialogMediatorHandler,
    IDialogs,
    useDialogMediator
} from './dialogs';
import { IViewModelDetached } from './IViewModelDetached';
import { ArcContext } from '@cratis/arc.react';
import { WellKnownBindings } from "./WellKnownBindings";
import { deepEqual } from '@cratis/arc';
import { IHandleParams } from 'IHandleParams';
import { IHandleQueryParams } from 'IHandleQueryParams';
import { IHandleProps } from 'IHandleProps';
import { ObservableQueryFor, QueryFor } from '@cratis/arc/queries';
import { Command } from '@cratis/arc/commands';
import { ICanBeConfigured } from '@cratis/arc/ICanBeConfigured';
import { DialogComponentsContext, DialogContextContent, IDialogComponents, useDialogContext } from '@cratis/arc.react/dialogs';
import { ICommandScope, useCommandScope } from '@cratis/arc.react/commands';

interface IViewModel extends IViewModelDetached {
    __childContainer: DependencyContainer;
}

function disposeViewModel(viewModel: IViewModel) {
    const vmWithDetach = (viewModel as IViewModelDetached);
    if (typeof (vmWithDetach.detached) == 'function') {
        vmWithDetach.detached();
    }

    if (viewModel.__childContainer) {
        const container = viewModel.__childContainer as DependencyContainer;
        container.dispose();
    }
}

function handleProps(viewModel: IViewModel, params: object) {
    const vmWithHandleParams = (viewModel as unknown as IHandleProps);
    if (typeof (vmWithHandleParams.handleProps) == 'function') {
        vmWithHandleParams.handleProps(params);
    }
}

function handleParams(viewModel: IViewModel, params: object) {
    const vmWithHandleParams = (viewModel as unknown as IHandleParams);
    if (typeof (vmWithHandleParams.handleParams) == 'function') {
        vmWithHandleParams.handleParams(params);
    }
}

function handleQueryParams(viewModel: IViewModel, queryParams: object) {
    const vmWithHandleParams = (viewModel as unknown as IHandleQueryParams);
    if (typeof (vmWithHandleParams.handleQueryParams) == 'function') {
        vmWithHandleParams.handleQueryParams(queryParams);
    }
}

/**
 * Represents the view context that is passed to the view.
 */
export interface IViewContext<T, TProps = object> {
    viewModel: T,
    props: TProps,
}

/**
 * Use a view model with a component.
 * @param {Constructor} viewModelType View model type to use.
 * @param {FunctionComponent} targetComponent The target component to render.
 * @returns 
 */
export function withViewModel<TViewModel extends object, TProps extends object = object>(
    viewModelType: Constructor<TViewModel>,
    targetComponent: FunctionComponent<IViewContext<TViewModel, TProps>>) {

    const renderComponent = (props: TProps) => {
        const applicationContext = useContext(ArcContext);
        const dialogComponentsContext = useContext<IDialogComponents>(DialogComponentsContext);
        const commandScope = useCommandScope();
        const params = useParams();
        const [currentProps, setCurrentProps] = useState(props);
        const [previousParams, setPreviousParams] = useState(params);
        const [queryParams] = useSearchParams();
        const [previousQueryParams, setPreviousQueryParams] = useState(queryParams);
        const queryParamsObject = Object.fromEntries(queryParams.entries());
        const dialogMediatorContext = useRef<IDialogMediatorHandler | null>(null);
        const currentViewModel = useRef<TViewModel | null>(null);
        const [, setInitialRender] = useState(true);
        const parentDialogMediator = useDialogMediator();
        const dialogContext = useDialogContext();

        useEffect(() => {
            if (currentViewModel.current !== null) {
                return () => {
                    disposeViewModel(currentViewModel.current as IViewModel);
                };
            }

            dialogMediatorContext.current = new DialogMediatorHandler(parentDialogMediator);

            const child = container.createChildContainer();
            child.registerInstance(WellKnownBindings.props, props);
            child.registerInstance(WellKnownBindings.params, params);
            child.registerInstance(WellKnownBindings.queryParams, queryParamsObject);
            child.registerInstance(DialogContextContent, dialogContext as unknown);
            child.registerInstance(ICommandScope as Constructor<ICommandScope>, commandScope);

            const originalResolve = child.resolve;

            child.resolve = function <T>(type: Constructor<T>) {
                // eslint-disable-next-line prefer-rest-params
                const instance = originalResolve.apply(child, arguments as never);

                if (type.prototype instanceof ObservableQueryFor ||
                    type.prototype instanceof QueryFor ||
                    type.prototype instanceof Command) {
                    const query = instance as ICanBeConfigured;
                    query.setMicroservice(applicationContext.microservice);
                    query.setApiBasePath(applicationContext.apiBasePath ?? '');
                    query.setOrigin(applicationContext.origin ?? '');
                }

                return instance;
            } as never;

            const dialogService = new Dialogs(dialogMediatorContext.current!, dialogComponentsContext);
            child.registerInstance<IDialogs>(IDialogs as Constructor<IDialogs>, dialogService);
            const viewModel = child.resolve<TViewModel>(viewModelType) as IViewModel;
            makeAutoObservable(viewModel);
            viewModel.__childContainer = child;
            currentViewModel.current = viewModel as TViewModel;

            setInitialRender(false);
            handleProps(viewModel, props);
            handleParams(viewModel, params);
            handleQueryParams(viewModel, queryParamsObject);

            return () => {
                if (applicationContext.development === false) {
                    disposeViewModel(viewModel);
                }
            };
        }, []);

        if (currentViewModel.current === null) return null;

        if (!deepEqual(currentProps, props)) {
            setCurrentProps(props);
            handleProps(currentViewModel.current as IViewModel, props);
        }

        if (!deepEqual(params, previousParams)) {
            setPreviousParams(params);
            handleParams(currentViewModel.current as IViewModel, params);
        }

        if (!deepEqual(queryParams, previousQueryParams)) {
            setPreviousQueryParams(queryParams);
            handleQueryParams(currentViewModel.current as IViewModel, queryParams);
        }

        const component = () => targetComponent({ viewModel: currentViewModel.current!, props }) as ReactElement<object, string>;

        return (
            <DialogMediator handler={dialogMediatorContext.current!}>
                <Observer>
                    {component}
                </Observer>
            </DialogMediator>
        );
    };

    return renderComponent;
}
