// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Arc.Screenplay.for_ScreenplayVerification.when_classifying_binding_errors.given;

public class known_admission_diagnostics : Specification
{
    protected IReadOnlyList<CompilerContractCatalog.DiagnosticFact> _catalog;
    protected IReadOnlyList<(Diagnostic Diagnostic, bool AuthoringOnly)> _diagnostics;
    protected int _expectedCount;

    void Establish()
    {
        _catalog = CompilerContractCatalog.Diagnostics.Where(fact => ExpectedBindingDiagnostics.AcceptedCodes.Contains(fact.Code)).ToArray();
        var handler = Diagnostic.Error("PLAY0268", "Command 'Register' handler requires a constrained implementation attachment.", SourceLocation.Start);
        var concurrency = Diagnostic.Error("PLAY0271", "Command 'Register' concurrency metadata keeps its legacy meaning and cannot bind to ESM v1.", SourceLocation.Start);
        var operations = Diagnostic.Error("PLAY0268", "Operations and systems are not admitted by any supported executable model (ESM) version yet (#301).", SourceLocation.Start);
        var reads = Diagnostic.Error("PLAY0271", "Command 'Register' reads 'Author' with legacy semantics that cannot imply decision consistency.", SourceLocation.Start);
        _diagnostics = [(handler, false), (handler, true), (concurrency, false), (concurrency, true), (operations, true), (reads, true)];
    }
}
