---
title: Cratis Arc proxies — read slice location metadata
description: Read a command or query's module, feature and slice in Cratis Arc backend contexts and generated TypeScript proxies for diagnostics.
---

A slice location in Cratis Arc identifies the module, feature and slice of a command or query from its C# namespace. Read it from backend contexts or generated TypeScript proxies to attach that location to diagnostics without maintaining a separate mapping.

## How it is resolved

The location comes from the namespace of the type that owns the command or query: the command type for a model-bound command, the read model for a model-bound query, and the controller for controller actions.

1. When the namespace equals or is nested under a [namespace root](namespace-roots.md), use the segments below the longest matching root.
2. Without a matching root, remove the `Cratis.API` namespace prefix, then skip the leading segments set by [`CratisProxiesSegmentsToSkip`](basic.md#namespace-segment-skipping).
3. From the remaining segments, take the first as the module, the last as the slice and those in between as the feature, joined with a period.

With fewer than three segments, Arc fills the deepest parts first: one segment names the slice; two name the feature and slice. Unfilled parts are empty strings, never null. With no remaining segments, the location is empty. React components resolved from folders fill from the module down instead; see [slice context](../../../../frontend/react/slice-context.md#folders-map-from-the-module-down).

With the namespace root `MyApp.Features`:

| C# namespace | Module | Feature | Slice |
| --- | --- | --- | --- |
| `MyApp.Features.Billing.Invoices.Pay` | `Billing` | `Invoices` | `Pay` |
| `MyApp.Features.Billing.Invoices.Recurring.Pay` | `Billing` | `Invoices.Recurring` | `Pay` |
| `MyApp.Features.Invoices.Pay` | _(empty)_ | `Invoices` | `Pay` |
| `MyApp.Features.Pay` | _(empty)_ | _(empty)_ | `Pay` |
| `MyApp.Features` | _(empty)_ | _(empty)_ | _(empty)_ |

Proxy output folders use the same namespace-segment rules. The namespace root's `Folder` adds an output base folder, but is never part of the slice location.

## Configuration

Use your existing proxy namespace settings; slice location needs no separate mapping. For example, put these settings inside your project:

```xml
<PropertyGroup>
    <CratisProxiesSegmentsToSkip>1</CratisProxiesSegmentsToSkip>
</PropertyGroup>
<ItemGroup>
    <NamespaceRoot Include="features" Namespace="MyApp.Features" Folder="features" />
</ItemGroup>
```

During assembly-info generation, the Arc MSBuild targets emit these values as `[assembly: SliceNamespaceRoot("MyApp.Features")]` and `[assembly: SliceNamespaceSegmentsToSkip(1)]` attributes from `Cratis.Arc.Slices`. The runtime can then read the namespace settings used for proxy generation.

Arc reads the attributes from the assembly that declares the type. If that assembly declares neither, Arc falls back to the entry assembly, such as the web application generating a class library's proxies. If your project does not use those targets or sets `GenerateAssemblyInfo` to `false`, declare the attributes yourself.

## On the backend

`CommandContext.SliceLocation` and `QueryContext.SliceLocation` hold the location of the executing command or query. Filters receive the context directly; other components can read it from `ICommandContextAccessor.Current` or `IQueryContextManager.Current`.

A `CommandContext` carries the command type and resolves its location when you read the property. This also works for a context you construct yourself. A `QueryContext` carries only the query name, so Arc sets its location in the query pipeline or MVC action filter using the read model or controller type. If you construct a `QueryContext` yourself, set its location when you need one; otherwise it carries `SliceLocation.NotSet`.

To include the command's location in tracing, read it in a command filter:

```csharp
using System.Diagnostics;
using Cratis.Arc.Commands;

public class SliceLocationTracingFilter : ICommandFilter
{
    public Task<CommandResult> OnExecution(CommandContext context)
    {
        Activity.Current?.SetTag("arc.slice_location", context.SliceLocation.ToString());
        return Task.FromResult(CommandResult.Success(context.CorrelationId));
    }
}
```

For a command in `MyApp.Features.Billing.Invoices.Pay` with the root above, the filter adds `arc.slice_location` with the value `Billing/Invoices/Pay` when a trace activity is present.

`SliceLocation` in `Cratis.Arc.Slices` is a record containing the concepts `ModuleName`, `FeatureName` and `SliceName`. An empty part equals that concept's `NotSet` value. `ToString()` joins the populated parts with `/`; `SliceLocation.NotSet` has every part empty. Use `SliceLocation.For(type)` to resolve a location directly from a type outside a command or query context.

## In the frontend

Every generated command and query proxy declares its location as a static member, resolved when the proxy was generated:

```typescript
import { SliceLocation as ArcSliceLocation, PropertyDescriptor } from '@cratis/arc/reflection';

export class PayInvoice extends Command<IPayInvoice> implements IPayInvoice {
    static readonly sliceLocation: ArcSliceLocation = new ArcSliceLocation('Billing', 'Invoices', 'Pay');
    readonly route: string = '/api/billing/invoices/pay';
    // ...
}
```

The generated proxy imports Arc's type under the `ArcSliceLocation` alias, so a type of your own named `SliceLocation` can be used by the same proxy without a duplicate identifier.

Read the static member directly when you know the proxy class. For code that receives a class or instance, use the corresponding helper:

```typescript
import { SliceLocation } from '@cratis/arc/reflection';
import { PayInvoice } from './generated/Billing/Invoices/Pay';

const fromClass = PayInvoice.sliceLocation;
const fromType = SliceLocation.ofType(PayInvoice);
const fromInstance = SliceLocation.of(new PayInvoice());

console.log(fromClass.toString()); // Billing/Invoices/Pay
```

Both helpers read the class's static member and return `SliceLocation.empty` when it has no location metadata. `isEmpty` is true when all three parts are empty; `equals(other)` compares locations part by part.

Location metadata is static so it does not collide with instance data. A command property or query parameter named `SliceLocation` can still have its own generated `sliceLocation` accessor; use the class member or helpers to read the metadata.

If you need the location of the React component rendering a screen, use `useSliceContext()` instead. [Slice context](../../../../frontend/react/slice-context.md) explains that folder-based mapping.

## Upgrading

When regenerating proxies, align `@cratis/arc` with the Arc NuGet packages used for generation. The generated code imports `SliceLocation` from `@cratis/arc/reflection`; a package version without that export cannot compile the new proxies.

Earlier proxies without a static location member return `SliceLocation.empty` through the helpers. Regenerate them to add the metadata, then check the class member against the expected namespace mapping.
