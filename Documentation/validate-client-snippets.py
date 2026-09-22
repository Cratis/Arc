#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Compile every shared-docs C# snippet against Arc's real source.

`Documentation/client-snippets/**` is not a docs page. It is the C# side of the
language tabs the shared Arc pages render, pulled by the documentation site from
this repository and from each other Arc client repository. Nothing in a Markdown
file is compiled by anything, so without this gate a snippet is a string that
nobody checks: a renamed attribute, a changed signature or an outright invented
API keeps rendering happily on the published site.

This generates a throwaway project that references the real Arc projects, turns
every snippet into a compilation unit, and builds it with warnings as errors.
A snippet is a fragment, not a file, so each one declares a context in
`SNIPPET_CONTEXTS` saying which shape it is (`declaration`, `member`, `body` or
`file`), which shared domain fixtures it draws its supporting types from, and any
extra prelude. The snippet body itself is compiled verbatim - never rewritten -
so what compiles is exactly what a reader sees.

Usage:
    python3 Documentation/validate-client-snippets.py
    python3 Documentation/validate-client-snippets.py --self-test

Exit codes: 0 clean, 1 on any failure (including finding nothing to validate).
"""

import argparse
import re
import shutil
import subprocess
import sys
import textwrap
from dataclasses import dataclass, field
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
SNIPPET_ROOT = REPO_ROOT / "Documentation" / "client-snippets"
GENERATED_DIR = REPO_ROOT / "Documentation" / ".client-snippet-validation"
GENERATED_PROJECT = GENERATED_DIR / "ClientSnippetValidation.csproj"

FENCE_RE = re.compile(r"```([^\s`]+)[^\n]*\n(.*?)\n```", re.DOTALL)
USING_DIRECTIVE_RE = re.compile(
    r"^using\s+(?:static\s+)?[A-Za-z_][A-Za-z0-9_.]*(?:\s*=\s*[A-Za-z_][A-Za-z0-9_.]*)?\s*;$")

SNIPPET_LANGUAGE = "csharp"

# Shared domain fixtures are emitted once, into their own namespaces, and pulled in
# by a `using` on the snippets that need them - so `AuthorId` is declared once and
# referenced from every snippet in the library domain instead of being copy-pasted
# into sixteen preludes.
FIXTURE_NAMESPACE_ROOT = "Cratis.Arc.Documentation.Snippets.Fixtures"
SNIPPET_NAMESPACE_ROOT = "Cratis.Arc.Documentation.Snippets"

# A client with no equivalent workflow keeps its tab visible with an explicit
# statement instead of disappearing from the tab group - see the Chronicle
# "Contributing to Clients" page, which this mirrors. Such a snippet is a `text`
# fence containing this marker, and is deliberately not compiled.
UNSUPPORTED_FENCE_LANGUAGE = "text"
UNSUPPORTED_MARKER = "does not support this workflow yet"

# Usings every snippet gets. A snippet may also carry its own `using` lines; those
# are hoisted to the top of the generated file so a declaration fragment does not
# have to be rewritten to compile.
DEFAULT_USINGS = (
    "using Cratis.Arc.Commands.ModelBound;",
    "using Cratis.Arc.Queries.ModelBound;",
)


@dataclass(frozen=True)
class DomainFixture:
    """Supporting domain types shared by every snippet in one domain.

    Emitted once into `FIXTURE_NAMESPACE_ROOT.<Name>`, and made visible to a
    snippet by listing the fixture in its `SnippetContext.fixtures`. A snippet that
    declares one of these types itself shadows the fixture's copy - a type declared
    in the snippet's own namespace wins over one reached through a using directive -
    so a snippet whose whole point is to show `RegisterAuthor` still shows its own.
    """

    usings: tuple[str, ...] = ()
    declarations: str = ""


# The three domains the snippets draw on. Keep these minimal: they exist to give a
# fragment the types it references, not to model anything.
FIXTURES: dict[str, DomainFixture] = {
    "library": DomainFixture(
        usings=(
            "using Cratis.Concepts;",
            "using MongoDB.Driver;",
        ),
        declarations="""
            public record AuthorId(Guid Value) : ConceptAs<Guid>(Value)
            {
                public static AuthorId New() => new(Guid.NewGuid());
            }

            public record AuthorName(string Value) : ConceptAs<string>(Value);

            public record BookId(Guid Value) : ConceptAs<Guid>(Value)
            {
                public static BookId New() => new(Guid.NewGuid());
            }

            public record BookTitle(string Value) : ConceptAs<string>(Value);

            public record Author(AuthorId Id, AuthorName Name);

            public record Book(BookId Id, AuthorId AuthorId, BookTitle Title);

            public record RegisterAuthor(AuthorId Id, AuthorName Name)
            {
                public Task Handle(IMongoCollection<Author> authors) =>
                    authors.InsertOneAsync(new Author(Id, Name));
            }
        """,
    ),
    "loan": DomainFixture(
        usings=(
            "using Cratis.Concepts;",
        ),
        declarations="""
            public record LoanId(Guid Value) : ConceptAs<Guid>(Value)
            {
                public static LoanId New() => new(Guid.NewGuid());
            }

            public record ApplicantId(Guid Value) : ConceptAs<Guid>(Value)
            {
                public static ApplicantId New() => new(Guid.NewGuid());
            }

            public record CreditScore(int Value) : ConceptAs<int>(Value);

            public enum RiskBand
            {
                Low,
                Medium,
                High
            }

            public record LoanAssessment(LoanId Loan, CreditScore Score, RiskBand? Band = null);

            public interface ICreditBureau
            {
                CreditScore GetScore(ApplicantId applicant);

                Task<CreditScore> GetScore(ApplicantId applicant, CancellationToken cancellationToken);
            }

            public interface IRiskModel
            {
                RiskBand Band(ApplicantId applicant);
            }

            public record AssessLoan(LoanId LoanId, ApplicantId Applicant)
            {
                public LoanAssessment Handle(CreditScore creditScore) => new(LoanId, creditScore);
            }
        """,
    ),
    "account": DomainFixture(
        usings=(
            "using Cratis.Concepts;",
        ),
        declarations="""
            public record AccountId(Guid Value) : ConceptAs<Guid>(Value);

            public record AccountHolder(string Value) : ConceptAs<string>(Value);

            public record AccountName(string Value) : ConceptAs<string>(Value);

            public record CustomerId(Guid Value) : ConceptAs<Guid>(Value);

            public record Account(AccountId Id, AccountHolder Owner);

            public record DebitAccount(AccountId Id, AccountName Name);

            public interface IAccountService
            {
                Task Open(AccountId id, AccountName name, CustomerId owner);
            }
        """,
    ),
    "ledger": DomainFixture(
        usings=(
            "using Cratis.Chronicle.Events;",
        ),
        declarations="""
            public record LedgerId(Guid Value) : EventSourceId<Guid>(Value);

            public record AccountId(Guid Value) : EventSourceId<Guid>(Value);

            public record LedgerBalance(decimal Balance);

            public record AccountBalance(decimal Balance);

            public record LedgerSettled(decimal Balance);

            public record FundsWithdrawn(decimal Amount, decimal Remaining);

            public record MoneyDeposited(decimal Amount);

            public record Withdraw(AccountId AccountId, decimal Amount);
        """,
    ),
    "order": DomainFixture(
        declarations="""
            public enum OrderStatus
            {
                Draft,
                ReadyForSubmission,
                Submitted
            }

            public record OrderReadModel(Guid Id, OrderStatus Status, string Destination, double TotalWeight);

            public record SubmitOrder(Guid Id);

            public record ShippingQuote(decimal Amount)
            {
                public static readonly ShippingQuote None = new(0m);
            }

            public interface IShippingRates
            {
                Task<ShippingQuote> Quote(string destination, double weight);
            }
        """,
    ),
}


def fixture_namespace(name: str) -> str:
    return f"{FIXTURE_NAMESPACE_ROOT}.{name.capitalize()}"


@dataclass(frozen=True)
class SnippetContext:
    """The enclosing context a fragment needs in order to compile.

    kind:      "declaration" for a fragment of type declarations (a `[Command]`
               record, an `IProvideIdentityDetails<T>` class) - the prelude and the
               snippet are emitted side by side in the generated namespace.
               "member" for a fragment of type members (a bare `Provide(...)`, a
               static query method, a `[Fact]`) - the prelude and the snippet are
               emitted inside a generated enclosing record, whose primary
               constructor is `host`, so the fragment sees the properties the real
               enclosing command would give it.
               "body" for a fragment of statements - the prelude and the snippet
               are emitted inside a generated async method.
               "file" for a snippet that is a whole file and declares its own
               namespace - it is emitted verbatim after the usings, with no
               generated namespace wrapped around it.
    fixtures:  names of shared domain fixtures the snippet draws its supporting
               types from; each becomes a using directive.
    usings:    extra usings on top of DEFAULT_USINGS and the fixtures'.
    host:      "member" only - the primary constructor parameter list of the
               generated enclosing record, naming the properties the fragment reads
               off `this`.
    prelude:   supporting declarations, sibling members or locals that the rendered
               snippet deliberately leaves out and no fixture supplies.
    """

    kind: str = "declaration"
    fixtures: tuple[str, ...] = ()
    usings: tuple[str, ...] = ()
    host: str = ""
    prelude: str = ""


KNOWN_KINDS = ("declaration", "member", "body", "file")

USING_REACTIVE = "using System.Reactive.Subjects;"
USING_MONGO = "using MongoDB.Driver;"
USING_FLUENT_VALIDATION = "using FluentValidation;"
USING_ARC_COMMANDS = "using Cratis.Arc.Commands;"
USING_ARC_VALIDATION = "using Cratis.Arc.Validation;"
USING_MONADS = "using Cratis.Monads;"
USING_ARC_TESTING = "using Cratis.Arc.Testing.Commands;"
USING_ARC_CHRONICLE_TESTING = "using Cratis.Arc.Chronicle.Testing.Commands;"


# Per-snippet preludes. A snippet id is its path under client-snippets without the
# extension. Unlisted snippets compile as declarations with DEFAULT_USINGS only;
# add an entry here when a snippet needs more context than that.
SNIPPET_CONTEXTS: dict[str, SnippetContext] = {
    "scenarios/provide-data-to-a-command/assess-loan": SnippetContext(
        kind="declaration",
        fixtures=("loan",),
    ),
    "scenarios/provide-data-to-a-command/cancellation": SnippetContext(
        kind="declaration",
        fixtures=("loan",),
    ),
    "scenarios/provide-data-to-a-command/provider-owned-state": SnippetContext(
        kind="member",
        fixtures=("order",),
    ),
    "scenarios/provide-data-to-a-command/several-values": SnippetContext(
        kind="member",
        fixtures=("loan",),
        host="LoanId LoanId, ApplicantId Applicant",
    ),
    "scenarios/provide-data-to-a-command/short-circuit": SnippetContext(
        kind="member",
        fixtures=("loan",),
        usings=(USING_ARC_VALIDATION, USING_MONADS),
        host="LoanId LoanId, ApplicantId Applicant",
    ),
    "scenarios/provide-data-to-a-command/test-the-decision": SnippetContext(
        kind="member",
        fixtures=("loan",),
        usings=("using Cratis.Specifications;", "using Xunit;"),
    ),
    "scenarios/query-related-data/books-for-author": SnippetContext(
        kind="declaration",
        fixtures=("library",),
        usings=(USING_REACTIVE, USING_MONGO),
    ),
    "scenarios/return-a-result-or-error/handle-result": SnippetContext(
        kind="member",
        fixtures=("library",),
        usings=(USING_MONGO, USING_ARC_VALIDATION, USING_MONADS),
        host="AuthorId Id, AuthorName Name",
    ),
    "scenarios/test-a-command/command-under-test": SnippetContext(
        kind="file",
        fixtures=("library",),
    ),
    "scenarios/test-a-command/spec": SnippetContext(
        # Compiles against the `Library.Authors` types declared by the
        # command-under-test snippet it is rendered next to, in the same project.
        kind="file",
        fixtures=("library",),
    ),
    "scenarios/validate-a-command/command-rule": SnippetContext(
        kind="declaration",
        fixtures=("library",),
        usings=(USING_ARC_COMMANDS, USING_FLUENT_VALIDATION),
    ),
    "scenarios/validate-a-command/concept-rule": SnippetContext(
        kind="declaration",
        fixtures=("library",),
        usings=(USING_ARC_VALIDATION, USING_FLUENT_VALIDATION),
    ),
    "scenarios/validate-a-command/service-rule": SnippetContext(
        kind="declaration",
        fixtures=("library",),
        usings=(USING_ARC_COMMANDS, USING_FLUENT_VALIDATION),
    ),
    "scenarios/validate-a-command/state-rule": SnippetContext(
        kind="declaration",
        fixtures=("order",),
        usings=(USING_ARC_COMMANDS, USING_FLUENT_VALIDATION),
    ),
    "scenarios/use-current-state-in-a-command/rename-author": SnippetContext(
        kind="declaration",
        fixtures=("library",),
        usings=(USING_MONGO,),
    ),
    "scenarios/use-current-state-in-a-command/rename-author-validator": SnippetContext(
        kind="declaration",
        fixtures=("library",),
        usings=(USING_ARC_COMMANDS, USING_FLUENT_VALIDATION),
        prelude="""
            public record RenameAuthor(AuthorId Id, AuthorName NewName);
        """,
    ),
    "scenarios/use-current-state-in-a-command/register-customer-validator": SnippetContext(
        kind="declaration",
        usings=(USING_ARC_COMMANDS, USING_FLUENT_VALIDATION),
        prelude="""
            public record Customer(Guid Id, string Name);

            public record RegisterCustomer(Guid Id, string Name);
        """,
    ),
    "scenarios/use-current-state-in-a-command/required-order-state": SnippetContext(
        kind="declaration",
        fixtures=("order",),
        usings=(USING_ARC_COMMANDS, USING_FLUENT_VALIDATION),
    ),
    "scenarios/use-current-state-in-a-command/chronicle-commands": SnippetContext(
        kind="declaration",
        fixtures=("ledger",),
    ),
    "scenarios/use-current-state-in-a-command/seed-events": SnippetContext(
        # The Chronicle testing surface lives in Cratis.Arc.Chronicle.Testing; the generated
        # enclosing record stands in for the spec class the fragment is a member of.
        kind="member",
        fixtures=("ledger",),
        usings=(USING_ARC_CHRONICLE_TESTING, USING_ARC_TESTING),
        prelude="""
            readonly CommandScenario<Withdraw> _scenario = new();
            readonly AccountId _accountId = new(Guid.NewGuid());
        """,
    ),
    "scenarios/use-current-state-in-a-command/pin-read-model": SnippetContext(
        kind="member",
        fixtures=("ledger",),
        usings=(USING_ARC_CHRONICLE_TESTING, USING_ARC_TESTING),
        prelude="""
            readonly CommandScenario<Withdraw> _scenario = new();
            readonly AccountId _accountId = new(Guid.NewGuid());
        """,
    ),
    "frontend/index/open-account": SnippetContext(
        kind="declaration",
        fixtures=("account",),
        usings=(USING_MONGO,),
    ),
    "frontend/react/proxy-generation/open-debit-account": SnippetContext(
        # Deliberately self-contained: the page teaches what a whole backend file looks like
        # before the generator turns it into TypeScript.
        kind="declaration",
    ),
    "frontend/react/commands/index/command-payload": SnippetContext(
        kind="declaration",
        fixtures=("account",),
    ),
    "frontend/react/queries/usage/parameterized-query": SnippetContext(
        kind="member",
        fixtures=("account",),
        usings=(USING_MONGO, "using Microsoft.AspNetCore.Mvc;"),
        prelude="""
            readonly IMongoCollection<DebitAccount> _collection = null!;
        """,
    ),
    "frontend/react/command-form/validation/profile-command": SnippetContext(
        kind="file",
    ),
    "frontend/react/command-form/auto-server-validation/server-only-rule": SnippetContext(
        # A replacement for the validator the validation page declares, so it cannot share that
        # snippet's namespace - the two would declare UpdateProfileValidator twice.
        kind="declaration",
        usings=(USING_ARC_COMMANDS, USING_FLUENT_VALIDATION, "using Cratis.Concepts;"),
        prelude="""
            public record ProfileName(string Value) : ConceptAs<string>(Value);

            public record EmailAddress(string Value) : ConceptAs<string>(Value);

            public record UpdateProfile(ProfileName Name, EmailAddress Email);
        """,
    ),
    "tutorial/first-slice/author-slice": SnippetContext(
        # The chapter's own RegisterAuthor and Author shadow the fixture's, which is the
        # point of the page - the reader is looking at the files they wrote.
        kind="declaration",
        fixtures=("library",),
        usings=(USING_REACTIVE, USING_MONGO),
    ),
    "tutorial/validation/author-name-rule": SnippetContext(
        kind="declaration",
        fixtures=("library",),
        usings=(USING_ARC_VALIDATION, USING_FLUENT_VALIDATION),
    ),
    "tutorial/validation/duplicate-name-rule": SnippetContext(
        kind="declaration",
        fixtures=("library",),
        usings=(USING_ARC_COMMANDS, USING_FLUENT_VALIDATION, USING_MONGO),
    ),
    "tutorial/books-and-relationships/book-concepts": SnippetContext(
        # Deliberately fixture-free: the chapter is teaching the reader to declare these
        # two concepts, so the snippet must be the whole declaration.
        kind="declaration",
        usings=("using Cratis.Concepts;",),
    ),
    "tutorial/books-and-relationships/add-book": SnippetContext(
        kind="declaration",
        fixtures=("library",),
        usings=(USING_MONGO,),
    ),
    "tutorial/books-and-relationships/books-for-author": SnippetContext(
        kind="declaration",
        fixtures=("library",),
        usings=(USING_REACTIVE, USING_MONGO),
    ),
    "tutorial/authorization/roles-on-command": SnippetContext(
        kind="declaration",
        fixtures=("library",),
        usings=("using Cratis.Arc.Authorization;", USING_MONGO),
    ),
    "tutorial/authorization/roles-on-query": SnippetContext(
        kind="declaration",
        fixtures=("library",),
        usings=("using Cratis.Arc.Authorization;", USING_REACTIVE, USING_MONGO),
    ),
    "tutorial/real-time/observable-query": SnippetContext(
        kind="member",
        fixtures=("library",),
        usings=(USING_REACTIVE, USING_MONGO),
    ),
    "tutorial/real-time/one-shot-query": SnippetContext(
        kind="member",
        fixtures=("library",),
        usings=(USING_MONGO,),
    ),
    "understanding-identity-and-access/authorization": SnippetContext(
        kind="declaration",
        fixtures=("library",),
        usings=(
            USING_REACTIVE,
            "using Cratis.Arc.Authorization;",
            USING_MONGO,
        ),
    ),
    "understanding-the-proxy-boundary/register-author": SnippetContext(
        kind="declaration",
        fixtures=("library",),
        usings=(USING_MONGO,),
    ),
    "understanding-the-proxy-boundary/rename-property": SnippetContext(
        kind="declaration",
        fixtures=("library",),
        usings=(USING_MONGO,),
    ),
    "understanding-identity-and-access/identity-provider": SnippetContext(
        kind="declaration",
        usings=(
            "using Cratis.Arc.Identity;",
            "using MongoDB.Driver;",
        ),
        prelude="""
            public record Member(Guid Id, string Role, string Name, string Subject);

            public record LibraryIdentity(Guid Id, string Role, string Name)
            {
                public static readonly LibraryIdentity None = new(Guid.Empty, string.Empty, string.Empty);
            }
        """,
    ),
}


class SnippetError(Exception):
    """The exception that is thrown when a snippet violates the snippet contract."""


@dataclass
class Snippet:
    """One snippet file resolved into something compilable."""

    identifier: str
    path: Path
    code: str = ""
    unsupported: bool = False
    usings: list[str] = field(default_factory=list)


def snippet_identifier(path: Path) -> str:
    return path.relative_to(SNIPPET_ROOT).with_suffix("").as_posix()


def display_path(path: Path) -> str:
    return path.relative_to(REPO_ROOT).as_posix()


def snippet_files() -> list[Path]:
    if not SNIPPET_ROOT.is_dir():
        raise SnippetError(f"Snippet root does not exist: {display_path(SNIPPET_ROOT)}")

    files = sorted([*SNIPPET_ROOT.rglob("*.md"), *SNIPPET_ROOT.rglob("*.mdx")])
    seen: dict[str, Path] = {}
    for path in files:
        identifier = snippet_identifier(path)
        if identifier in seen:
            raise SnippetError(
                f"Duplicate snippet id {identifier!r}: "
                f"{display_path(seen[identifier])} and {display_path(path)}")
        seen[identifier] = path
    return files


def read_snippet(path: Path) -> Snippet:
    """Enforce the snippet contract and return the fenced code."""
    identifier = snippet_identifier(path)
    matches = FENCE_RE.findall(path.read_text(encoding="utf-8"))

    if len(matches) != 1:
        raise SnippetError(
            f"{display_path(path)} must contain exactly one fenced code block, found {len(matches)}")

    language, code = matches[0]

    if language == UNSUPPORTED_FENCE_LANGUAGE:
        if UNSUPPORTED_MARKER not in code:
            raise SnippetError(
                f"{display_path(path)} uses a {UNSUPPORTED_FENCE_LANGUAGE!r} fence but does not contain "
                f"the unsupported marker {UNSUPPORTED_MARKER!r}. A non-{SNIPPET_LANGUAGE} fence is only "
                f"allowed to state that this client does not support the workflow.")
        return Snippet(identifier=identifier, path=path, unsupported=True)

    if language != SNIPPET_LANGUAGE:
        raise SnippetError(
            f"{display_path(path)} must use a {SNIPPET_LANGUAGE!r} code fence, got {language!r}")

    usings, body = split_usings(code.strip())
    if not body:
        raise SnippetError(f"{display_path(path)} contains no code to compile")

    return Snippet(identifier=identifier, path=path, code=body, usings=usings)


def split_usings(code: str) -> tuple[list[str], str]:
    """Hoist top-level using directives out of the fragment so it can be wrapped."""
    usings: list[str] = []
    body: list[str] = []
    for line in code.splitlines():
        if USING_DIRECTIVE_RE.match(line):
            usings.append(line)
        else:
            body.append(line)
    return usings, "\n".join(body).strip()


def sanitized(identifier: str) -> str:
    return re.sub(r"[^A-Za-z0-9_]", "_", identifier)


GENERATED_HEADER = (
    "// Do not edit - edit the snippet instead.",
    "#pragma warning disable CS1998, CS0219, CA1812, CA1852, IDE0051",
)


def generate_snippet_source(snippet: Snippet) -> str:
    """Render one snippet into its own compilation unit.

    Each snippet gets its own namespace so that two snippets declaring the same
    supporting type cannot collide, and so a compiler error names the snippet. A
    "file" snippet declares its own namespace - it is a whole file, not a fragment -
    and is still named by the generated file it lands in.
    """
    context = SNIPPET_CONTEXTS.get(snippet.identifier, SnippetContext())
    if context.kind not in KNOWN_KINDS:
        raise SnippetError(
            f"Snippet {snippet.identifier!r} declares unknown context kind {context.kind!r}")
    if context.host and context.kind != "member":
        raise SnippetError(
            f"Snippet {snippet.identifier!r} declares a host on a {context.kind!r} context; "
            "only a 'member' context has a generated enclosing type")

    fixture_usings = []
    for name in context.fixtures:
        if name not in FIXTURES:
            raise SnippetError(
                f"Snippet {snippet.identifier!r} asks for unknown domain fixture {name!r}")
        fixture_usings.append(f"using {fixture_namespace(name)};")

    usings = sorted({*DEFAULT_USINGS, *fixture_usings, *context.usings, *snippet.usings})
    prelude = textwrap.dedent(context.prelude).strip()
    namespace = f"{SNIPPET_NAMESPACE_ROOT}.Snippet_{sanitized(snippet.identifier)}"
    combined = "\n\n".join(part for part in (prelude, snippet.code) if part)

    if context.kind == "file":
        return "\n".join([
            f"// Generated from {display_path(snippet.path)} by Documentation/validate-client-snippets.py.",
            *GENERATED_HEADER,
            "",
            *usings,
            "",
            combined,
            "",
        ])

    if context.kind == "declaration":
        members = combined
    elif context.kind == "member":
        # The generated enclosing record stands in for the `[Command]` or `[ReadModel]`
        # record the member really lives on, so a `Provide(...)` fragment can read the
        # command's own properties exactly as the rendered snippet does.
        members = (
            f"internal sealed record SnippetHost({context.host})\n"
            "{\n"
            f"{textwrap.indent(combined, '    ')}\n"
            "}")
    else:
        members = (
            "internal static class SnippetBody\n"
            "{\n"
            "    public static async Task Run()\n"
            "    {\n"
            f"{textwrap.indent(combined, '        ')}\n"
            "    }\n"
            "}")

    return "\n".join([
        f"// Generated from {display_path(snippet.path)} by Documentation/validate-client-snippets.py.",
        *GENERATED_HEADER,
        "",
        *usings,
        "",
        f"namespace {namespace};",
        "",
        members,
        "",
    ])


def generate_fixture_source(name: str) -> str:
    """Render one shared domain fixture into its own compilation unit."""
    fixture = FIXTURES[name]
    usings = sorted({*DEFAULT_USINGS, *fixture.usings})
    return "\n".join([
        f"// Shared {name!r} domain fixture generated by Documentation/validate-client-snippets.py.",
        *GENERATED_HEADER,
        "",
        *usings,
        "",
        f"namespace {fixture_namespace(name)};",
        "",
        textwrap.dedent(fixture.declarations).strip(),
        "",
    ])


def generate_project(sources: list[Path]) -> str:
    """The throwaway project.

    Analyzers are off because a documentation fragment is written for a reader,
    not to satisfy StyleCop; warnings are still errors, so a snippet using an
    obsolete or ambiguous API fails. TargetFrameworks is pinned to a single
    framework so this does not fan out across the repository's release matrix.
    """
    compile_items = "\n".join(
        f'        <Compile Include="{path.name}" />' for path in sources)
    return f"""<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFrameworks>net10.0</TargetFrameworks>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
        <IsPackable>false</IsPackable>
        <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
        <RunAnalyzers>false</RunAnalyzers>
        <RunAnalyzersDuringBuild>false</RunAnalyzersDuringBuild>
        <EnableNETAnalyzers>false</EnableNETAnalyzers>
        <EnforceCodeStyleInBuild>false</EnforceCodeStyleInBuild>
        <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
        <MSBuildTreatWarningsAsErrors>true</MSBuildTreatWarningsAsErrors>
    </PropertyGroup>

    <ItemGroup>
{compile_items}
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="../../Source/DotNET/Arc/Arc.csproj" />
        <ProjectReference Include="../../Source/DotNET/Arc.Core/Arc.Core.csproj" />
        <ProjectReference Include="../../Source/DotNET/MongoDB/MongoDB.csproj" />
        <!-- Cratis.Arc.Testing - the CommandScenario<T> and CommandResult assertions the
             test-a-command snippets are teaching. -->
        <ProjectReference Include="../../Source/DotNET/Testing/Testing.csproj" />
        <!-- Cratis.Arc.Chronicle.Testing - the optional Chronicle integration's own scenario
             seeding, which the use-current-state page shows for projected state. It brings
             Cratis.Arc.Chronicle, and with it EventSourceId<T>, along transitively. -->
        <ProjectReference Include="../../Source/DotNET/Chronicle.Testing/Chronicle.Testing.csproj" />
    </ItemGroup>

    <!-- Versions come from the repository's central package management, so validating the
         docs cannot pin a version of its own. -->
    <ItemGroup>
        <PackageReference Include="Cratis.Specifications.XUnit" />
        <PackageReference Include="NSubstitute" />
        <PackageReference Include="xunit" />
    </ItemGroup>
</Project>
"""


def write_generated(snippets: list[Snippet], corrupt: str | None = None) -> list[Path]:
    GENERATED_DIR.mkdir(parents=True, exist_ok=True)
    sources: list[Path] = []

    required_fixtures = sorted({
        name
        for snippet in snippets
        for name in SNIPPET_CONTEXTS.get(snippet.identifier, SnippetContext()).fixtures})
    for name in required_fixtures:
        path = GENERATED_DIR / f"Fixture_{sanitized(name)}.cs"
        path.write_text(generate_fixture_source(name), encoding="utf-8")
        sources.append(path)

    snippet_sources: list[Path] = []
    for snippet in snippets:
        source = generate_snippet_source(snippet)
        if corrupt is not None and snippet.identifier == corrupt:
            source += (
                "\n// --self-test planted defect\n"
                "internal sealed class PlantedSelfTestDefect : "
                "ThisTypeDoesNotExistAnywhereInArc;\n")
        path = GENERATED_DIR / f"Snippet_{sanitized(snippet.identifier)}.cs"
        path.write_text(source, encoding="utf-8")
        snippet_sources.append(path)

    GENERATED_PROJECT.write_text(generate_project([*sources, *snippet_sources]), encoding="utf-8")
    return snippet_sources


def build() -> int:
    # -p:CratisProxiesOutputPath= clears the property the proxy generator target is
    # conditioned on, so validating the docs never re-runs proxy generation over the
    # repository's real generated TypeScript.
    return subprocess.run(
        [
            "dotnet",
            "build",
            str(GENERATED_PROJECT),
            "--configuration",
            "Release",
            "-p:CratisProxiesOutputPath=",
        ],
        cwd=REPO_ROOT,
        check=False,
    ).returncode


def collect() -> tuple[list[Snippet], list[Snippet]]:
    files = snippet_files()

    # Non-vacuity fuse. A checker that validates nothing and prints a tick is worse
    # than no checker: it turns "nobody looked" into a green check.
    if not files:
        raise SnippetError(
            f"No client snippets found under {display_path(SNIPPET_ROOT)}. "
            "Either the snippets were moved or this validator is looking in the wrong place; "
            "validating zero snippets is a failure, not a pass.")

    snippets = [read_snippet(path) for path in files]

    # A context for a snippet that no longer exists is a rule quietly deleted: the
    # snippet it used to describe may have been renamed and now compiles with no
    # context at all.
    known = {snippet.identifier for snippet in snippets}
    stale = sorted(set(SNIPPET_CONTEXTS) - known)
    if stale:
        raise SnippetError(
            "SNIPPET_CONTEXTS has entries for snippets that do not exist: "
            f"{', '.join(stale)}")

    compilable = [snippet for snippet in snippets if not snippet.unsupported]
    unsupported = [snippet for snippet in snippets if snippet.unsupported]
    return compilable, unsupported


def run(self_test: bool) -> int:
    compilable, unsupported = collect()

    if self_test:
        if not compilable:
            raise SnippetError("--self-test needs at least one compilable snippet to plant a defect in")
        target = compilable[0].identifier
        print(f"Self-test: planting a reference to a non-existent type in {target!r}.")
    elif not compilable:
        print(f"All {len(unsupported)} snippet(s) are unsupported markers - nothing to compile.")
        return 0

    shutil.rmtree(GENERATED_DIR, ignore_errors=True)
    try:
        sources = write_generated(compilable, corrupt=target if self_test else None)
        if len(sources) != len(compilable):
            raise SnippetError(
                f"Generated {len(sources)} source file(s) for {len(compilable)} snippet(s)")

        exit_code = build()
    finally:
        shutil.rmtree(GENERATED_DIR, ignore_errors=True)

    if self_test:
        if exit_code == 0:
            print(
                "Self-test FAILED: the build succeeded with a planted reference to a non-existent "
                "type, so this validator is not detecting anything.",
                file=sys.stderr)
            return 1
        print(f"Self-test passed: the planted defect failed the build (exit code {exit_code}).")
        return 0

    if exit_code != 0:
        print(f"dotnet build failed with exit code {exit_code}.", file=sys.stderr)
        return exit_code

    print(
        f"Compiled {len(compilable)} C# Arc client snippet(s) against Arc source successfully"
        f"{f', skipped {len(unsupported)} unsupported marker(s)' if unsupported else ''}.")
    for snippet in compilable:
        print(f"  - {snippet.identifier}")
    for snippet in unsupported:
        print(f"  - {snippet.identifier} (unsupported marker, not compiled)")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--self-test",
        action="store_true",
        help="Plant a reference to a non-existent type in a snippet and fail unless the build rejects it.")
    arguments = parser.parse_args()

    try:
        return run(arguments.self_test)
    except SnippetError as error:
        print(f"Client snippet validation failed: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
