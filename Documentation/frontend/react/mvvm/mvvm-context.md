---
title: MVVM Context
description: Set up the <MVVM> component that configures MobX and Tsyringe bindings, and place the router that view models need.
---

The MVVM solution in Cratis Arc is based on top of [mobx](https://mobx.js.org).
In addition to being based on top of it, everything internal is expecting a certain behavior that
needs to be configured for mobx for everything to work.

This is were the `MVVM` context comes into play. The Arc exposes a component that
configures mobx and also ensures the necessary bindings for [tsyringe](./tsyringe.md) are configured.

Include it in your application setup. Components created with `withViewModel()` also read route and query parameters through React Router's `useParams` and `useSearchParams`, so they must render inside a router. `react-router-dom` 7 is a peer dependency of `@cratis/arc.react.mvvm`, and `<MVVM>` does not provide a router itself:

```tsx
import { BrowserRouter } from 'react-router-dom';
import { MVVM } from '@cratis/arc.react.mvvm';

export const App = () => {
    return (
        <MVVM>
            <BrowserRouter>
                {/* Your application, including every withViewModel() component */}
            </BrowserRouter>
        </MVVM>
    );
};
```
