# Using a View Model

Every React functional component can have a view model. This is accomplished using the `withViewModel()` method.

```tsx
import { withViewModel } from '@cratis/arc.react.mvvm';

export const Counter = withViewModel(CounterViewModel, ({viewModel}) => {
    return (
        <>
            Counter is : {viewModel.counter}
            <button onClick={() => viewModel.increaseCounter()}>Increase counter</button>
        </>
    )
});
```

The component uses the `withViewModel` and passes the type `CounterViewModel` to be created for the view.
As arguments you can then have the `viewModel`, this instance survives re-renders and can be stateful.

```ts
export class CounterViewModel {
    counter: number;

    increaseCounter() {
        this.counter++;
    }
}
```

The `viewModel` is automatically observable, which means that all properties on it will notify the view
if there are any changes to them. This means that the `increaseCounter()` method can just go ahead and
increase the counter and the view will automatically re-render.

## Observer boundaries

`withViewModel()` observes the render of the component it wraps — and only that render. The view model
is built on [mobx](https://mobx.js.org) (see [MVVM context](./mvvm-context.md)), so MobX tracks which
observable view model properties a render reads and re-renders that component when they change.

This has an important consequence: a **child** component that reads observable view model state
*directly* sits outside the parent's observer boundary. MobX never tracks that read, so the child does
not re-render when the state changes — even though the value on screen is stale.

```tsx
import { withViewModel } from '@cratis/arc.react.mvvm';

// The child reads `viewModel.counter` directly — but it has no observer boundary of its own,
// so it will NOT re-render when the counter changes.
const CounterValue = ({ viewModel }: { viewModel: CounterViewModel }) => (
    <span>Counter is : {viewModel.counter}</span>
);

export const Counter = withViewModel(CounterViewModel, ({ viewModel }) => {
    return (
        <>
            <CounterValue viewModel={viewModel} />
            <button onClick={() => viewModel.increaseCounter()}>Increase counter</button>
        </>
    );
});
```

### Prefer passing plain props down

The cleanest fix is to keep observable reads inside the wrapped component and pass plain values down to
presentational children. The parent is already observed, so reading `viewModel.counter` there is tracked,
and the child re-renders because its `value` prop changed like any other React prop:

```tsx
import { withViewModel } from '@cratis/arc.react.mvvm';

// Presentational child — receives a plain value, knows nothing about the view model.
const CounterValue = ({ value }: { value: number }) => (
    <span>Counter is : {value}</span>
);

export const Counter = withViewModel(CounterViewModel, ({ viewModel }) => {
    return (
        <>
            <CounterValue value={viewModel.counter} />
            <button onClick={() => viewModel.increaseCounter()}>Increase counter</button>
        </>
    );
});
```

This keeps reactivity entirely within the wrapped component and leaves children as simple, predictable
functions of their props.

### Wrap a leaf that must read view model state directly

When a child genuinely needs to read observable view model state directly — for example a list that
filters a large collection the view model owns — give just that leaf its own observer boundary with
`observer()`. Import it from `@cratis/arc.react.mvvm`:

```tsx
import { observer, withViewModel } from '@cratis/arc.react.mvvm';

// The leaf reads observable state directly and is wrapped in `observer()`, so it has its
// own boundary and re-renders when `filteredPartners` changes.
const PartnerResults = observer(({ viewModel }: { viewModel: PartnerSearchViewModel }) => (
    <ul>
        {viewModel.filteredPartners.map(partner => <li key={partner.id}>{partner.name}</li>)}
    </ul>
));

export const PartnerSearch = withViewModel(PartnerSearchViewModel, ({ viewModel }) => {
    return (
        <>
            <input
                value={viewModel.partnerSearch}
                onChange={event => viewModel.setPartnerSearch(event.target.value)} />
            <PartnerResults viewModel={viewModel} />
        </>
    );
});
```

> Note: Always import `observer` from `@cratis/arc.react.mvvm`, never directly from `mobx-react` or
> `mobx-react-lite`. Going through Cratis Arc keeps the MobX binding an internal detail that can evolve
> without breaking your code, and the
> [Cratis Arc ESLint plugin](https://github.com/Cratis/Arc/blob/main/Source/JavaScript/Arc.ESLint/README.md)
> flags direct imports for you.

Reach for `observer()` only on the specific leaf that reads observable state directly. Do not blanket-wrap
every component — that adds render-tracking overhead everywhere and obscures which components actually
depend on view model state. Prefer the plain-props pattern above, and drop down to a leaf `observer()`
only when a child must read view model observables itself.

## Props

Components can have props associated with them. The `withViewModel` supports specifying props type and
ability to take the props in as a parameter on the render function:

```tsx
import { withViewModel } from '@cratis/arc.react.mvvm';

export interface CounterProps {
    initialValue: number;
}

export const Counter = withViewModel<CounterViewModel, CounterProps>(CounterViewModel, ({viewModel, props}) => {
    return (
        <>
            Counter is : {viewModel.counter}
            <button onClick={() => viewModel.increaseCounter()}>Increase counter</button>
        </>
    )
});
```

The `withViewModel` has 2 generic arguments that can be passed:

- ViewModel type
- Props type

When you don't have a props type, you don't need to specify the `ViewModel` type arguments, as TypeScript will
automatically infer its type from the first argument that specifies the type to use.

> Note: The reason we provide the type of view model to use as a parameter to the `withViewModel` and not just rely
> on the `ViewModel` type argument, is that we need a proper token / type to be used to be able to create an instance
> of it. Generic information is only available at compile-time in TypeScript and when transpiled to JavaScript
> this information is gone.

### Injecting Props as dependency

If you're component only gets loaded once and the props typically don't change without the component being unloaded
and then loaded again, you can simply inject it as part of the constructor.

#### Using `@props` decorator (recommended)

The `@props` decorator wraps tsyringe's `@inject` and also captures the parameter type:

```ts
import { props } from '@cratis/arc.react.mvvm';
import { injectable } from 'tsyringe';

@injectable()
export class MyViewModel {
    constructor(@props componentProps: Props) {
    }
}
```

#### Using `@inject` manually

You can also use tsyringe's `@inject` directly with `WellKnownBindings.props`:

```ts
import { WellKnownBindings } from '@cratis/arc.react.mvvm';
import { inject, injectable } from 'tsyringe';

@injectable()
export class MyViewModel {
    constructor(@inject(WellKnownBindings.props) props: Props) {
    }
}
```

### Handling Props

For the scenario were the props are changing by a consumer of your component, you need to implement the `IHandleProps<>`.
It is a generic interface, but the generic argument is optional and is defaulted to `object` if not specified.

```ts
import { IHandleProps } from '@cratis/arc.react.mvvm';

export class MyViewModel implements IHandleProps<Props>  {
    handleProps(props: Props): void {
        // Do things based on props
    }
}
```

> Note: The `handleProps` method will be called both on initial load and for any subsequent changes.

## Params

Params defined as part of routes using `react-router` can easily be accessed by a view model in a couple of ways.

### Injecting Params as dependency

If you're component only gets loaded once and the parameters can't change without the component being unloaded
and then loaded again, you can simply inject it as part of the constructor.

#### Using `@params` decorator (recommended)

The `@params` decorator wraps tsyringe's `@inject` and also captures the parameter type. When a typed class is
provided, `withViewModel` will use `JsonSerializer` to deserialize the string-based URL parameters into the
correct types. Properties on the params class must be annotated with `@field` from `@cratis/fundamentals` so
the serializer knows how to convert each value — for example a `number` property will be converted from the
URL string `"42"` to the integer `42`. Without `@field`, the property values remain as strings:

```ts
import { field } from '@cratis/fundamentals';
import { params } from '@cratis/arc.react.mvvm';
import { injectable } from 'tsyringe';

class RouteParams {
    @field id!: string;
    @field count!: number;
}

@injectable()
export class MyViewModel {
    constructor(@params routeParams: RouteParams) {
        // routeParams.count is a proper number, not the URL string "42"
    }
}
```

#### Using `@inject` manually

You can also use tsyringe's `@inject` directly with `WellKnownBindings.params`. In this case the parameters
are injected as raw strings as they appear in the URL:

```ts
import { WellKnownBindings } from '@cratis/arc.react.mvvm';
import { inject, injectable } from 'tsyringe';

@injectable()
export class MyViewModel {
    constructor(@inject(WellKnownBindings.params) params: Params) {
    }
}
```

The downside of this approach is that if a param changes and the component is not unloaded, you won't get the
change.

### Handling Params

For the scenario were the params are changing while the component is not unloaded, implementing the `IHandleParams<>`
interface is a better option. It is a generic interface, but the generic argument is optional and is defaulted to
`object` if not specified.

```ts
import { IHandleParams } from '@cratis/arc.react.mvvm';

export class MyViewModel implements IHandleParams<Params>  {
    handleParams(params: Params): void {
        // Do things based on params
    }
}
```

> Note: The `handleParams` method will be called both on initial load and for any subsequent changes.

## QueryParams

QueryParams defined as part of routes using `react-router` can easily be accessed by a view model in a couple of ways.

### Injecting Query Params as dependency

If you're component only gets loaded once and the query parameters can't change without the component being unloaded
and then loaded again, you can simply inject it as part of the constructor.

#### Using `@queryParams` decorator (recommended)

The `@queryParams` decorator wraps tsyringe's `@inject` and also captures the parameter type. When a typed class is
provided, `withViewModel` will use `JsonSerializer` to deserialize the string-based URL query parameters into the
correct types. Properties on the query params class must be annotated with `@field` from `@cratis/fundamentals` so
the serializer knows how to convert each value — for example a `number` property will be converted from the
URL string `"1"` to the integer `1`. Without `@field`, the property values remain as strings:

```ts
import { field } from '@cratis/fundamentals';
import { queryParams } from '@cratis/arc.react.mvvm';
import { injectable } from 'tsyringe';

class SearchParams {
    @field filter!: string;
    @field page!: number;
}

@injectable()
export class MyViewModel {
    constructor(@queryParams searchParams: SearchParams) {
        // searchParams.page is a proper number, not the URL string "1"
    }
}
```

#### Using `@inject` manually

You can also use tsyringe's `@inject` directly with `WellKnownBindings.queryParams`. In this case the parameters
are injected as raw strings as they appear in the URL:

```ts
import { WellKnownBindings } from '@cratis/arc.react.mvvm';
import { inject, injectable } from 'tsyringe';

@injectable()
export class MyViewModel {
    constructor(@inject(WellKnownBindings.queryParams) queryParams: Params) {
    }
}
```

The downside of this approach is that if a query param changes and the component is not unloaded, you won't get the
change.

### Handling Query Params

For the scenario were the query params are changing while the component is not unloaded, implementing the `IHandleParams<>`
interface is a better option. It is a generic interface, but the generic argument is optional and is defaulted to
`object` if not specified.

```ts
import { IHandleQueryParams } from '@cratis/arc.react.mvvm';

export class MyViewModel implements IHandleQueryParams<Params>  {
    handleQueryParams(queryParams: QueryParams): void {
        // Do things based on query params
    }
}
```

> Note: The `handleQueryParams` method will be called both on initial load and for any subsequent changes.

## View Model lifecycle

### Detaches

You can get notified when a view model is detached from its view, typically as a consequence of the view being removed from the DOM.
This is achieved by implementing the `IViewModelDetached` interface on your view model:

```ts
import { IViewModelDetached } from '@cratis/arc.react.mvvm';

export class MyViewModel implements IViewModelDetached {
    detached() {
        // Clean up...
    }
}
```
