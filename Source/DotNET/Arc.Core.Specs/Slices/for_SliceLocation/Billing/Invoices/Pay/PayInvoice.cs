// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Slices;

// Assembly-wide on purpose: the resolver reads namespace settings from the assembly a type lives in, exactly as the
// MSBuild properties emit them into a consuming assembly. Only namespaces under this root are affected; every other
// type in this spec assembly still resolves with no root and no segments skipped.
[assembly: SliceNamespaceRoot("Cratis.Arc.Slices.for_SliceLocation")]

namespace Cratis.Arc.Slices.for_SliceLocation.Billing.Invoices.Pay;

public record PayInvoice;
