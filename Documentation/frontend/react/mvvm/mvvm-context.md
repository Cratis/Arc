# MVVM Context

The MVVM solution in Cratis Arc is based on top of [mobx](https://mobx.js.org).
In addition to being based on top of it, everything internal is expecting a certain behavior that
needs to be configured for mobx for everything to work.

This is were the `MVVM` context comes into play. The Arc exposes a component that
configures mobx and also ensures the necessary bindings for [tsyringe](./tsyringe.md) are configured.

All you need to do is include it in your application setup:

```tsx
import { MVVM } from '@cratis/arc.react.mvvm';

export const App = () => {
    return (
        <MVVM>
            {/* Your application */}
        </MVVM>
    );
};
```
