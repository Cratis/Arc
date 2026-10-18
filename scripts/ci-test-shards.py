#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
"""Holds the .NET Build test shards in .github/test-shards.json to exactly the test units they replace.

The .NET Build workflow used to run one job per ProxyGenerator.Specs 'for_' namespace (dotnet test
--filter "FullyQualifiedName~<namespace>") and one job per spec project. It now runs a handful of shards.
A proxy shard ORs the same per-namespace filters together and a spec shard runs dotnet test once per
project, so the shards cover exactly what the old jobs covered as long as every unit is assigned to
exactly one shard and no shard names a unit that does not exist. That is what 'check' enforces.

Commands:
  check                 Discover the units and verify the assignment (run before the build).
  matrix                Print proxy-shards=<json> and spec-shards=<json> lines for $GITHUB_OUTPUT.
  filter --shard NAME   Print the dotnet test filter of a proxy shard.
  projects --shard NAME Print the spec project folders of a spec shard, one per line.
  verify-trx --shard NAME --results DIR
                        Fail when a unit of the shard has no test result in its TRX file(s).
  --self-test           Plant every defect the checks exist for and fail unless each one is caught.

Exit codes: 0 clean, 1 defects found, 2 could not run (missing or malformed input, empty population).
"""

from __future__ import annotations

import argparse
import contextlib
import io
import json
import os
import re
import shutil
import sys
import tempfile
import xml.etree.ElementTree as ET
from collections import Counter
from pathlib import Path

PROXY_SPECS = Path("Source/DotNET/Tools/ProxyGenerator.Specs")
PROXY_SPECS_PROJECT_FILE = "ProxyGenerator.Specs.csproj"
SPEC_ROOT = Path("Source/DotNET")
SHARDS_FILE = Path(".github/test-shards.json")
SKIPPED_DIRECTORIES = {"bin", "obj", "node_modules", ".git"}
TRX_NAMESPACE = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"


class CannotRun(Exception):
    """The check could not run; never reported as a pass."""


def walk_files(root: Path):
    for directory, subdirectories, files in os.walk(root):
        subdirectories[:] = sorted(d for d in subdirectories if d not in SKIPPED_DIRECTORIES)
        for name in sorted(files):
            yield Path(directory) / name


def namespace_unit(declaration: str) -> str | None:
    """The 'for_' prefix of a namespace, exactly as the old workflow's awk computed it.

    'namespace A.B.for_X.when_y;' -> 'A.B.for_X'. A namespace without a 'for_' segment is not a unit.
    """
    name = declaration[len("namespace "):].rstrip("\r\n").replace(";", "", 1)
    segments = name.split(".")
    for index, segment in enumerate(segments):
        if segment.startswith("for_"):
            return ".".join(segments[: index + 1])
    return None


def discover_proxy_namespaces(root: Path) -> list[str]:
    folder = root / PROXY_SPECS
    if not folder.is_dir():
        raise CannotRun(f"{PROXY_SPECS} does not exist under {root}")
    units = set()
    for path in walk_files(folder):
        if path.suffix != ".cs":
            continue
        for line in path.read_text(encoding="utf-8-sig", errors="replace").splitlines():
            if line.startswith("namespace "):
                unit = namespace_unit(line)
                if unit:
                    units.add(unit)
    return sorted(units)


def discover_spec_projects(root: Path) -> list[str]:
    folder = root / SPEC_ROOT
    if not folder.is_dir():
        raise CannotRun(f"{SPEC_ROOT} does not exist under {root}")
    projects = set()
    for path in walk_files(folder):
        if path.name.endswith(".Specs.csproj") and path.name != PROXY_SPECS_PROJECT_FILE:
            projects.add(path.parent.relative_to(root).as_posix())
    return sorted(projects)


def load_shards(root: Path) -> dict:
    path = root / SHARDS_FILE
    try:
        document = json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError as error:
        raise CannotRun(f"{SHARDS_FILE} does not exist") from error
    except json.JSONDecodeError as error:
        raise CannotRun(f"{SHARDS_FILE} is not valid JSON: {error}") from error
    for kind, key in (("proxyShards", "namespaces"), ("specShards", "projects")):
        shards = document.get(kind)
        if not isinstance(shards, list):
            raise CannotRun(f"{SHARDS_FILE} has no '{kind}' list")
        for shard in shards:
            if not isinstance(shard, dict) or not isinstance(shard.get("name"), str) or not isinstance(shard.get(key), list):
                raise CannotRun(f"{SHARDS_FILE}: every entry of '{kind}' needs a 'name' and a '{key}' list; got {shard!r}")
    return document


def find_defects(document: dict, namespaces: list[str], projects: list[str]) -> list[str]:
    defects = []
    names = Counter(shard["name"] for shard in document["proxyShards"] + document["specShards"])
    defects += [f"shard name '{name}' is used {count} times" for name, count in sorted(names.items()) if count > 1]
    for kind, key, discovered, label in (
        ("proxyShards", "namespaces", namespaces, "proxy namespace"),
        ("specShards", "projects", projects, "spec project"),
    ):
        assigned = Counter()
        for shard in document[kind]:
            if not shard[key]:
                defects.append(f"shard '{shard['name']}' has no {label}s")
            for unit in shard[key]:
                assigned[unit] += 1
        for unit in discovered:
            if assigned[unit] == 0:
                defects.append(f"{label} '{unit}' is not assigned to any shard in {SHARDS_FILE}")
            elif assigned[unit] > 1:
                shards = [shard["name"] for shard in document[kind] if unit in shard[key]]
                defects.append(f"{label} '{unit}' is assigned {assigned[unit]} times ({', '.join(shards)})")
        for unit in sorted(set(assigned) - set(discovered)):
            defects.append(f"{label} '{unit}' is assigned in {SHARDS_FILE} but does not exist")
    # Each spec project writes <folder name>.trx, so two projects with one folder name would share a file.
    folders = Counter(Path(project).name for project in projects)
    defects += [f"spec project folder name '{name}' is used {count} times; TRX files would collide" for name, count in sorted(folders.items()) if count > 1]
    return defects


def estimates(document: dict) -> list[tuple[str, float, int]]:
    measurement = document.get("measurement", {})
    measured = document.get("measuredSeconds", {})
    setup = measurement.get("setupSeconds", {})
    process = measurement.get("proxyProcessSeconds", 0)
    rows = []
    for shard in document["proxyShards"]:
        seconds = measured.get("proxyNamespaces", {})
        known = [unit for unit in shard["namespaces"] if unit in seconds]
        test = process + sum(max(seconds[unit] - process, 0.5) for unit in known)
        rows.append((shard["name"], setup.get("proxyShard", 0) + test, len(shard["namespaces"]) - len(known)))
    for shard in document["specShards"]:
        seconds = measured.get("specProjects", {})
        known = [unit for unit in shard["projects"] if unit in seconds]
        test = sum(seconds[unit] for unit in known)
        rows.append((shard["name"], setup.get("specShard", 0) + test, len(shard["projects"]) - len(known)))
    return rows


def check(root: Path, quiet: bool = False) -> tuple[int, dict]:
    document = load_shards(root)
    namespaces = discover_proxy_namespaces(root)
    projects = discover_spec_projects(root)
    if not namespaces:
        raise CannotRun(f"discovered no 'for_' namespaces in {PROXY_SPECS}; refusing to report a vacuous pass")
    if not projects:
        raise CannotRun(f"discovered no spec projects in {SPEC_ROOT}; refusing to report a vacuous pass")
    defects = find_defects(document, namespaces, projects)
    if defects:
        for defect in defects:
            print(f"::error title=Test shard assignment::{defect}")
        print(f"test shards: {len(defects)} defect(s) in the assignment of {len(namespaces)} proxy namespaces and {len(projects)} spec projects")
        return 1, document
    if not quiet:
        print(
            f"test shards: {len(namespaces)} proxy namespaces in {len(document['proxyShards'])} shards and "
            f"{len(projects)} spec projects in {len(document['specShards'])} shards; each assigned exactly once"
        )
        for name, seconds, unmeasured in estimates(document):
            note = f" ({unmeasured} unmeasured)" if unmeasured else ""
            print(f"  {name:<10} estimated {seconds / 60:4.1f} min{note}")
    return 0, document


def find_shard(document: dict, name: str) -> tuple[str, dict]:
    for kind in ("proxyShards", "specShards"):
        for shard in document[kind]:
            if shard["name"] == name:
                return kind, shard
    raise CannotRun(f"no shard named '{name}' in {SHARDS_FILE}")


def proxy_filter(namespaces: list[str]) -> str:
    return "|".join(f"FullyQualifiedName~{namespace}" for namespace in namespaces)


def trx_results(path: Path) -> list[tuple[str, str]]:
    """(fully qualified test name, outcome) for every result in a TRX file."""
    try:
        tree = ET.parse(path)
    except ET.ParseError as error:
        raise CannotRun(f"{path} is not a readable TRX file: {error}") from error
    names = {}
    for test in tree.iter(f"{TRX_NAMESPACE}UnitTest"):
        method = test.find(f"{TRX_NAMESPACE}TestMethod")
        if method is not None:
            names[test.get("id")] = f"{method.get('className', '')}.{method.get('name', '')}"
    return [
        (names.get(result.get("testId"), result.get("testName", "")), result.get("outcome", ""))
        for result in tree.iter(f"{TRX_NAMESPACE}UnitTestResult")
    ]


def describe(results: list[tuple[str, str]]) -> str:
    outcomes = Counter(outcome for _, outcome in results)
    return ", ".join(f"{count} {outcome.lower()}" for outcome, count in sorted(outcomes.items())) or "none"


def verify_trx(document: dict, name: str, results: Path) -> int:
    kind, shard = find_shard(document, name)
    defects = []
    total = 0
    if kind == "proxyShards":
        files = sorted(results.glob("*.trx"))
        if not files:
            raise CannotRun(f"no TRX file in {results} for shard '{name}'")
        all_results = [result for path in files for result in trx_results(path)]
        for namespace in shard["namespaces"]:
            matched = [result for result in all_results if namespace in result[0]]
            total += len(matched)
            print(f"  {namespace}: {len(matched)} tests ({describe(matched)})")
            if not matched:
                defects.append(f"proxy namespace '{namespace}' ran no tests in shard '{name}'")
    else:
        for project in shard["projects"]:
            path = results / f"{Path(project).name}.trx"
            if not path.is_file():
                defects.append(f"spec project '{project}' left no TRX file ({path.name}) in shard '{name}'")
                continue
            project_results = trx_results(path)
            total += len(project_results)
            print(f"  {project}: {len(project_results)} tests ({describe(project_results)})")
            if not project_results:
                defects.append(f"spec project '{project}' ran no tests in shard '{name}'")
    for defect in defects:
        print(f"::error title=Zero-test guard::{defect}")
    units = len(shard["namespaces"] if kind == "proxyShards" else shard["projects"])
    print(f"shard {name}: {total} test results across {units} units, {len(defects)} unit(s) without tests")
    return 1 if defects else 0


# ---------------------------------------------------------------------------------------------- self-test


def write(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")


def trx(tests: list[tuple[str, str, str]]) -> str:
    definitions = "".join(
        f'<UnitTest name="{method}" id="id{index}"><TestMethod className="{cls}" name="{method}" /></UnitTest>'
        for index, (cls, method, _) in enumerate(tests)
    )
    outcomes = "".join(
        f'<UnitTestResult testId="id{index}" testName="{method}" outcome="{outcome}" />'
        for index, (_, method, outcome) in enumerate(tests)
    )
    return (
        '<?xml version="1.0" encoding="utf-8"?>'
        '<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">'
        f"<Results>{outcomes}</Results><TestDefinitions>{definitions}</TestDefinitions></TestRun>"
    )


def self_test() -> int:
    failures = []
    cases = 0

    def expect(label: str, actual, expected, output: str = "") -> None:
        nonlocal cases
        cases += 1
        if actual != expected:
            failures.append(f"{label}: expected {expected!r}, got {actual!r}")
            print(output, end="")

    def expect_status(label: str, result: tuple[int, str], expected: int) -> None:
        status, output = result
        expect(label, status, expected, output)

    def quietly(function, *arguments):
        # Planted defects print ::error annotations; keep them out of a green run's annotations.
        buffer = io.StringIO()
        with contextlib.redirect_stdout(buffer):
            try:
                status = function(*arguments)
            except CannotRun:
                status = 2
        return status, buffer.getvalue()

    root = Path(tempfile.mkdtemp(prefix="ci-test-shards-"))
    try:
        proxy = root / PROXY_SPECS
        write(proxy / "for_A" / "when_x.cs", "namespace Cratis.P.for_A;\n")
        write(proxy / "for_A" / "nested" / "when_y.cs", "namespace Cratis.P.for_A.when_y.for_Inner;\n")
        write(proxy / "Scoped" / "for_B" / "when_z.cs", "\ufeffnamespace Cratis.P.Scoped.for_B\r\n{\r\n}\r\n")
        write(proxy / "Infrastructure" / "helper.cs", "namespace Cratis.P.Infrastructure;\n")
        write(proxy / "obj" / "Release" / "generated.cs", "namespace Cratis.P.for_Generated;\n")
        write(proxy / PROXY_SPECS_PROJECT_FILE, "<Project />")
        write(root / SPEC_ROOT / "One.Specs" / "One.Specs.csproj", "<Project />")
        write(root / SPEC_ROOT / "Nested" / "Two.Specs" / "Two.Specs.csproj", "<Project />")
        write(root / SPEC_ROOT / "Three" / "Three.csproj", "<Project />")

        expect("discovery: for_ prefixes, CRLF and BOM, bin/obj skipped", discover_proxy_namespaces(root), ["Cratis.P.Scoped.for_B", "Cratis.P.for_A"])
        expect("discovery: spec projects without ProxyGenerator.Specs", discover_spec_projects(root), ["Source/DotNET/Nested/Two.Specs", "Source/DotNET/One.Specs"])

        valid = {
            "proxyShards": [{"name": "proxy-1", "namespaces": ["Cratis.P.for_A"]}, {"name": "proxy-2", "namespaces": ["Cratis.P.Scoped.for_B"]}],
            "specShards": [{"name": "specs-1", "projects": ["Source/DotNET/One.Specs", "Source/DotNET/Nested/Two.Specs"]}],
        }

        def run_check(document) -> tuple[int, str]:
            write(root / SHARDS_FILE, json.dumps(document))
            return quietly(lambda: check(root, quiet=True)[0])

        def mutated(change) -> dict:
            document = json.loads(json.dumps(valid))
            change(document)
            return document

        expect_status("valid assignment passes", run_check(valid), 0)
        expect_status("unassigned namespace", run_check(mutated(lambda d: d["proxyShards"].pop(1))), 1)
        expect_status("namespace assigned twice", run_check(mutated(lambda d: d["proxyShards"][1]["namespaces"].append("Cratis.P.for_A"))), 1)
        expect_status("namespace that does not exist", run_check(mutated(lambda d: d["proxyShards"][0]["namespaces"].append("Cratis.P.for_Gone"))), 1)
        expect_status("spec project unassigned", run_check(mutated(lambda d: d["specShards"][0]["projects"].remove("Source/DotNET/One.Specs"))), 1)
        expect_status("spec project assigned twice", run_check(mutated(lambda d: d["specShards"].append({"name": "specs-2", "projects": ["Source/DotNET/One.Specs"]}))), 1)
        expect_status("spec project that does not exist", run_check(mutated(lambda d: d["specShards"][0]["projects"].append("Source/DotNET/Gone.Specs"))), 1)
        expect_status("empty shard", run_check(mutated(lambda d: d["specShards"].append({"name": "specs-2", "projects": []}))), 1)
        expect_status("duplicate shard name", run_check(mutated(lambda d: d["proxyShards"][1].update(name="proxy-1"))), 1)
        write(root / SPEC_ROOT / "Other" / "One.Specs" / "One.Specs.csproj", "<Project />")
        expect_status("two spec projects with one folder name", run_check(mutated(lambda d: d["specShards"][0]["projects"].append("Source/DotNET/Other/One.Specs"))), 1)
        shutil.rmtree(root / SPEC_ROOT / "Other")
        expect_status("malformed shards file", run_check({"proxyShards": [{"name": "proxy-1"}], "specShards": []}), 2)

        write(root / SPEC_ROOT / "Fresh.Specs" / "Fresh.Specs.csproj", "<Project />")
        expect_status("new spec project fails until assigned", run_check(valid), 1)
        (root / SPEC_ROOT / "Fresh.Specs" / "Fresh.Specs.csproj").unlink()
        write(proxy / "for_New" / "when_n.cs", "namespace Cratis.P.for_New;\n")
        expect_status("new namespace fails until assigned", run_check(valid), 1)
        shutil.rmtree(proxy / "for_New")

        for path in list(proxy.rglob("*.cs")):
            path.unlink()
        expect_status("no namespaces cannot run", run_check(valid), 2)

        expect("filter ORs the old per-namespace filters", proxy_filter(["A.for_X", "B.for_Y"]), "FullyQualifiedName~A.for_X|FullyQualifiedName~B.for_Y")

        results = root / "results"
        write(results / "proxy.trx", trx([("Cratis.P.for_A.when_x", "should_work", "Passed"), ("Cratis.P.Scoped.for_B.when_z", "should_skip", "NotExecuted")]))
        write(root / SHARDS_FILE, json.dumps(valid))
        document = load_shards(root)
        expect_status("proxy TRX with every namespace passes", quietly(verify_trx, document, "proxy-1", results), 0)
        expect_status("skipped-only namespace still counts as matched", quietly(verify_trx, document, "proxy-2", results), 0)
        write(results / "proxy.trx", trx([("Cratis.P.for_A.when_x", "should_work", "Passed")]))
        expect_status("proxy namespace without results", quietly(verify_trx, document, "proxy-2", results), 1)
        (results / "proxy.trx").unlink()
        expect_status("proxy shard without TRX cannot run", quietly(verify_trx, document, "proxy-1", results), 2)
        write(results / "One.Specs.trx", trx([("Cratis.One.for_Z.when", "should", "Passed")]))
        write(results / "Two.Specs.trx", trx([]))
        expect_status("spec project with zero tests", quietly(verify_trx, document, "specs-1", results), 1)
        write(results / "Two.Specs.trx", trx([("Cratis.Two.for_Z.when", "should", "Failed")]))
        expect_status("spec projects with tests pass", quietly(verify_trx, document, "specs-1", results), 0)
        (results / "Two.Specs.trx").unlink()
        expect_status("spec project without TRX", quietly(verify_trx, document, "specs-1", results), 1)
    finally:
        shutil.rmtree(root, ignore_errors=True)

    for failure in failures:
        print(f"::error title=ci-test-shards self-test::{failure}")
    print(f"self-test: {cases - len(failures)} of {cases} planted cases behaved as expected")
    return 1 if failures else 0


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("command", nargs="?", default="check", choices=["check", "matrix", "filter", "projects", "verify-trx"])
    parser.add_argument("--self-test", action="store_true")
    parser.add_argument("--shard")
    parser.add_argument("--results", type=Path)
    parser.add_argument("--root", type=Path, default=Path("."))
    arguments = parser.parse_args(argv)
    if arguments.self_test:
        return self_test()
    try:
        if arguments.command == "check":
            return check(arguments.root)[0]
        status, document = check(arguments.root, quiet=True)
        if status:
            return status
        if arguments.command == "matrix":
            print("proxy-shards=" + json.dumps([shard["name"] for shard in document["proxyShards"]]))
            print("spec-shards=" + json.dumps([shard["name"] for shard in document["specShards"]]))
            return 0
        if not arguments.shard:
            raise CannotRun(f"'{arguments.command}' needs --shard")
        kind, shard = find_shard(document, arguments.shard)
        if arguments.command == "filter":
            if kind != "proxyShards":
                raise CannotRun(f"'{arguments.shard}' is not a proxy shard")
            print(proxy_filter(shard["namespaces"]))
            return 0
        if arguments.command == "projects":
            if kind != "specShards":
                raise CannotRun(f"'{arguments.shard}' is not a spec shard")
            print("\n".join(shard["projects"]))
            return 0
        if not arguments.results:
            raise CannotRun("'verify-trx' needs --results")
        return verify_trx(document, arguments.shard, arguments.results)
    except CannotRun as error:
        print(f"::error title=ci-test-shards::{error}")
        return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
