// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_recovering_rendered_specifications.given;

/// <summary>States the captured model independently of the C# reader.</summary>
public static class ExpectedEnumCommandDocument
{
    /// <summary>Creates the document for one of the three captured specifications.</summary>
    /// <param name="name">The original specification name.</param>
    /// <param name="command">The command being issued.</param>
    /// <param name="status">The expected enum member.</param>
    /// <returns>The full expected document.</returns>
    public static string For(string name, string command, string status) => $$"""
        domain Invoices

        concept InvoiceId : Uuid

        concept InvoiceStatus : Enum
          draft = -1
          issued = 4
          cancelled = 90

        module Invoices
          feature Lifecycle
            slice StateChange Record
              command Issue
                id InvoiceId identifier
                produces StatusRecorded
                  for id
                  status = "issued"
              command RecordStatus
                id InvoiceId identifier
                status InvoiceStatus
                produces StatusRecorded
                  for id
                  status = status
              event StatusRecorded
                description "The event that occurs when status recorded."
                status InvoiceStatus
              specification {{name}}
                when {{command}}
                  id = "{{RenderedEnumCommandSources.Identity}}"
        {{(command == "RecordStatus" ? $"          status = \"{status}\"\n" : string.Empty)}}        then StatusRecorded
                  for "{{RenderedEnumCommandSources.Identity}}"
                  status = "{{status}}"
        """;
}
