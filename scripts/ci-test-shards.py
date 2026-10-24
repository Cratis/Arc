#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
"""Holds the .NET Build test shards in .github/test-shards.json to exactly the test units they replace.

The .NET Build workflow used to run one job per ProxyGenerator.Specs 'for_' namespace (dotnet test
--filter "FullyQualifiedName~<namespace>") and one job per spec project. It now runs a handful of shards.
A proxy shard ORs the same per-namespace VSTest filters together and a spec shard runs each prebuilt
xUnit executable. The assignment covers the same namespaces and projects when every unit is assigned
exactly once and no shard names a unit that does not exist. That is what 'check' enforces; case and
outcome parity across runner versions requires a separate explicit identity comparison.

Commands:
  check                 Discover the units and verify the assignment (run before the build).
  matrix                Print proxy-shards=<json> and spec-shards=<json> lines for $GITHUB_OUTPUT.
  filter --shard NAME   Print the dotnet test filter of a proxy shard.
  projects --shard NAME Print the spec project folders of a spec shard, one per line.
  verify-trx --shard NAME --results DIR
                        Fail when a unit of the shard has no test result in its TRX file(s).
  run --shard NAME --results DIR
                        Run prebuilt spec shards into a new results directory and validate TRX.
  --self-test           Plant every defect the checks exist for and fail unless each one is caught.

Exit codes: 0 clean, 1 defects found, 2 could not run (missing or malformed input, empty population).
"""

from __future__ import annotations

import argparse
import contextlib
import io
import json
import os
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
from collections import Counter
from dataclasses import dataclass
from pathlib import Path
from unittest.mock import patch

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


def terminal_trx_counts(results: list[tuple[str, str]]) -> dict[str, int]:
    outcomes = Counter(outcome for _, outcome in results)
    if not results or set(outcomes) - {"Passed", "Failed", "NotExecuted"}:
        raise ValueError("expected a positive total with complete terminal outcomes")
    return {
        "total": len(results),
        "executed": outcomes["Passed"] + outcomes["Failed"],
        "passed": outcomes["Passed"],
        "failed": outcomes["Failed"],
        "notExecuted": outcomes["NotExecuted"],
    }


def validate_trx_counters(counters: ET.Element, expected: dict[str, int]) -> None:
    for name, count in expected.items():
        if int(counters.get(name, "-1")) != count:
            raise ValueError(f"TRX counter '{name}' does not match result outcomes")
    validate_trx_nonterminal_counters(counters)
    if expected["executed"] == 0:
        raise ValueError("all tests were skipped")


def validate_trx_nonterminal_counters(counters: ET.Element) -> None:
    for name in ("error", "timeout", "aborted", "inconclusive", "passedButRunAborted",
                 "notRunnable", "disconnected", "inProgress", "pending"):
        if int(counters.get(name, "0")) != 0:
            raise ValueError(f"TRX counter '{name}' is not zero")


def validated_trx_results(path: Path) -> list[tuple[str, str]]:
    """Require complete terminal outcomes and matching counters, not just a populated file."""
    try:
        root = ET.parse(path).getroot()
        counters = root.findall(f"{TRX_NAMESPACE}ResultSummary/{TRX_NAMESPACE}Counters")
        if root.tag != f"{TRX_NAMESPACE}TestRun" or len(counters) != 1:
            raise ValueError("expected one TRX result summary counter")
        results = trx_results(path)
        validate_trx_counters(counters[0], terminal_trx_counts(results))
        return results
    except (OSError, ET.ParseError, ValueError) as error:
        raise CannotRun(f"{path} is not a complete TRX result: {error}") from error


@dataclass
class SpecificationRunner:
    """Prebuilt runner paths and per-invocation output configuration."""

    root: Path
    results: Path
    configuration: str

    def assembly_path(self, project: str) -> Path:
        project_file = self.root / project / f"{Path(project).name}.csproj"
        assemblies = ET.parse(project_file).getroot().findall("./PropertyGroup/AssemblyName")
        if len(assemblies) != 1 or not assemblies[0].text:
            raise ValueError(f"expected one AssemblyName in {project_file}")
        return (project_file.parent / "bin" / self.configuration / "net10.0" / assemblies[0].text).resolve()

    def command(self, project: str, namespaces: list[str] | None) -> list[str]:
        assembly = self.assembly_path(project)
        suffix = ".exe" if os.name == "nt" else ""
        command = [str(assembly.with_name(f"{assembly.name}{suffix}"))]
        if namespaces is not None:
            assembly = assembly.with_name(f"{assembly.name}.dll")
            if not assembly.is_file():
                raise CannotRun(f"missing prebuilt proxy assembly: {assembly}")
            command = ["dotnet", str(assembly), "-filterVSTest", proxy_filter(namespaces)]
        return command + ["-noLogo", "-noColor", "-result-trx", str(self.result_file(project))]

    def result_file(self, project: str) -> Path:
        return self.results / f"{Path(project).name}.trx"

    def run_project(self, project: str, namespaces: list[str] | None) -> int:
        print(f"::group::{project}", flush=True)
        status = 0
        try:
            child = subprocess.run(self.command(project, namespaces), cwd=self.root, check=False)
            status = child.returncode if child.returncode >= 0 else 128 - child.returncode
            if status:
                print(f"::error title=Specs failed::{project} exited with {status}")
            parsed = validated_trx_results(self.result_file(project))
            print(f"  {project}: {len(parsed)} tests ({describe(parsed)})")
            if any(outcome == "Failed" for _, outcome in parsed):
                status = status or 1
        except (OSError, ET.ParseError, ValueError, CannotRun) as error:
            print(f"::error title=Specs failed::{project}: {error}")
            status = status or 2
        finally:
            print("::endgroup::", flush=True)
        return status

    def prepare_results(self) -> None:
        try:
            self.results = self.results.resolve()
            self.results.mkdir(parents=True, exist_ok=False)
        except OSError as error:
            raise CannotRun(f"results directory must be new: {self.results}: {error}") from error

    def run(self, document: dict, name: str) -> int:
        """Launch a shard without reusing any previous invocation's results."""
        projects, namespaces = runner_selection(document, name)
        self.prepare_results()
        status = 0
        for project in projects:
            project_status = self.run_project(project, namespaces)
            status = status or project_status
        if namespaces is not None:
            status = status or verify_trx(document, name, self.results)
        return status


def runner_selection(document: dict, name: str) -> tuple[list[str], list[str] | None]:
    kind, shard = find_shard(document, name)
    if kind == "specShards":
        return shard["projects"], None
    if not shard["namespaces"]:
        raise CannotRun(f"'{name}' has no proxy namespaces")
    return [str(PROXY_SPECS)], shard["namespaces"]


def describe(results: list[tuple[str, str]]) -> str:
    outcomes = Counter(outcome for _, outcome in results)
    return ", ".join(f"{count} {outcome.lower()}" for outcome, count in sorted(outcomes.items())) or "none"


def unexpected_proxy_results(results: list[tuple[str, str]], shard: dict) -> list[str]:
    unexpected = [test for test, _ in results if not any(namespace in test for namespace in shard["namespaces"])]
    if unexpected:
        return [f"proxy shard '{shard['name']}' ran {len(unexpected)} tests outside its namespaces"]
    return []


def verify_trx(document: dict, name: str, results: Path) -> int:
    kind, shard = find_shard(document, name)
    defects = []
    total = 0
    if kind == "proxyShards":
        files = sorted(results.glob("*.trx"))
        if not files:
            raise CannotRun(f"no TRX file in {results} for shard '{name}'")
        all_results = [result for path in files for result in trx_results(path)]
        defects.extend(unexpected_proxy_results(all_results, shard))
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
        f"<Results>{outcomes}</Results><TestDefinitions>{definitions}</TestDefinitions>"
        f'<ResultSummary><Counters total="{len(tests)}" '
        f'executed="{sum(outcome != "NotExecuted" for _, _, outcome in tests)}" '
        f'passed="{sum(outcome == "Passed" for _, _, outcome in tests)}" '
        f'failed="{sum(outcome == "Failed" for _, _, outcome in tests)}" '
        f'notExecuted="{sum(outcome == "NotExecuted" for _, _, outcome in tests)}" /></ResultSummary></TestRun>'
    )


def self_test_trx_population(document: dict, results: Path, expect_status, quietly) -> None:
    write(results / "proxy.trx", trx([("Cratis.P.for_A.when_x", "should_work", "Passed")]))
    expect_status("proxy TRX with every namespace passes", quietly(verify_trx, document, "proxy-1", results), 0)
    write(results / "proxy.trx", trx([("Cratis.P.Scoped.for_B.when_z", "should_skip", "NotExecuted")]))
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


def self_test_trx_validation(results: Path, expect_status, quietly) -> None:
    validation_file = results / "validation.trx"
    complete = trx([("Cratis.One.for_Z.when", "should", "Passed"), ("Cratis.One.for_Z.when", "skip", "NotExecuted")])

    def validate(content: str) -> int:
        write(validation_file, content)
        validated_trx_results(validation_file)
        return 0

    expect_status("complete TRX counters and outcomes", quietly(validate, complete), 0)
    for label, content in (
        ("empty TRX", trx([])),
        ("malformed TRX", "<TestRun>"),
        ("wrong TRX root", complete.replace("TestRun", "Other")),
        ("missing TRX counters", complete.replace("Counters", "Missing")),
        ("duplicate TRX counters", complete.replace("</ResultSummary>", '<Counters total="2" /></ResultSummary>')),
        ("inconsistent total", complete.replace('total="2"', 'total="3"')),
        ("inconsistent executed", complete.replace('executed="1"', 'executed="2"')),
        ("inconsistent outcomes", complete.replace('passed="1"', 'passed="0"')),
        ("missing skipped counter", complete.replace('notExecuted="1"', "")),
        ("incomplete outcomes", complete.replace('outcome="Passed"', 'outcome="InProgress"')),
        ("pending counter", complete.replace('total="2"', 'total="2" pending="1"')),
        ("all skipped", trx([("Cratis.One.for_Z.when", "skip", "NotExecuted")])),
    ):
        expect_status(label, quietly(validate, content), 2)
    validation_file.unlink()
    expect_status("missing TRX", quietly(validated_trx_results, validation_file), 2)


def self_test_ordinary_runner(root: Path, valid: dict, expect, quietly) -> None:
    def expect_status(label, result, expected):
        expect(label, result[0], expected, result[1])

    complete = trx([("Cratis.One.for_Z.when", "should", "Passed"), ("Cratis.One.for_Z.when", "skip", "NotExecuted")])
    runner_document = {"proxyShards": valid["proxyShards"], "specShards": [{"name": "runner", "projects": ["Source/DotNET/One.Specs"]}]}
    write(root / SPEC_ROOT / "One.Specs" / "One.Specs.csproj", "<Project><PropertyGroup><AssemblyName>Cratis.One.Specs</AssemblyName></PropertyGroup></Project>")
    runner_results = root / "runner-results"
    runner = SpecificationRunner(root, runner_results, "Release")

    def run_fake_child(code: int, content: str | None) -> tuple[int, str]:
        def fake_child(command, *, cwd, check):
            expect("runner command", command[:4], [str((root / SPEC_ROOT / "One.Specs/bin/Release/net10.0" / ("Cratis.One.Specs.exe" if os.name == "nt" else "Cratis.One.Specs")).resolve()), "-noLogo", "-noColor", "-result-trx"])
            expect("runner working directory", cwd, root)
            expect("runner retains child exit", check, False)
            if content is not None:
                write(Path(command[4]), content)
            return subprocess.CompletedProcess(command, code)

        with patch("subprocess.run", side_effect=fake_child):
            return quietly(runner.run, runner_document, "runner")

    for label, code, content, expected in (
        ("runner success", 0, complete, 0),
        ("runner child failure", 7, complete, 7),
        ("runner child failure with missing TRX", 7, None, 7),
        ("runner signal", -9, complete, 137),
        ("runner missing TRX", 0, None, 2),
        ("runner incomplete TRX", 0, '<TestRun />', 2),
        ("runner zero tests", 0, trx([]), 2),
        ("runner skipped only", 0, trx([("C", "skip", "NotExecuted")]), 2),
        ("runner cannot hide failed tests", 0, trx([("C", "fails", "Failed")]), 1),
    ):
        expect_status(label, run_fake_child(code, content), expected)
        shutil.rmtree(runner_results)
    with patch("subprocess.run", side_effect=OSError("missing executable")):
        expect_status("runner launch failure", quietly(runner.run, runner_document, "runner"), 2)
    shutil.rmtree(runner_results)
    runner_results.mkdir()
    write(runner_results / "One.Specs.trx", complete)
    with patch("subprocess.run") as child:
        expect_status("runner rejects stale directory", quietly(runner.run, runner_document, "runner"), 2)
        expect("stale results never launch a child", child.called, False)


def self_test_proxy_runner(root: Path, valid: dict, expect, quietly) -> None:
    def expect_status(label, result, expected):
        expect(label, result[0], expected, result[1])

    proxy = root / PROXY_SPECS
    write(proxy / PROXY_SPECS_PROJECT_FILE, "<Project><PropertyGroup><AssemblyName>Cratis.Arc.ProxyGenerator.Specs</AssemblyName></PropertyGroup></Project>")
    assembly = (proxy / "bin/Release/net10.0/Cratis.Arc.ProxyGenerator.Specs.dll").resolve()
    proxy_document = json.loads(json.dumps(valid))
    proxy_document["proxyShards"][0]["namespaces"].append("Cratis.P.Scoped.for_B")
    proxy_results = (root / "proxy-results").resolve()
    runner = SpecificationRunner(root, proxy_results, "Release")
    selected = trx([("Cratis.P.for_A.when_x", "works", "Passed"), ("Cratis.P.Scoped.for_B.when_z", "works", "Passed")])
    with patch("subprocess.run") as child:
        expect_status("missing proxy binary", quietly(runner.run, proxy_document, "proxy-1"), 2)
        expect("missing binary never launches", child.called, False)
    shutil.rmtree(proxy_results)
    write(assembly, "mock binary; never executed")
    for label, code, content, expected in (
        ("proxy direct success", 0, selected, 0),
        ("proxy child failure", 7, selected, 7),
        ("proxy failure without TRX", 7, None, 7),
        ("proxy missing TRX", 0, None, 2),
        ("proxy malformed TRX", 0, "<TestRun>", 2),
        ("proxy missing namespace", 0, trx([("Cratis.P.for_A.when_x", "works", "Passed")]), 1),
        ("proxy wrong project", 0, trx([("Cratis.One.for_Z.when", "should", "Passed"), ("Cratis.One.for_Z.when", "skip", "NotExecuted")]), 1),
        ("proxy extra namespace", 0, trx([("Cratis.P.for_A.when_x", "works", "Passed"), ("Cratis.P.Scoped.for_B.when_z", "works", "Passed"), ("Cratis.Other.when", "works", "Passed")]), 1),
        ("proxy signal", -9, selected, 137),
    ):
        def fake_proxy(command, *, cwd, check):
            expect("proxy exact filtered command", command, ["dotnet", str(assembly), "-filterVSTest", "FullyQualifiedName~Cratis.P.for_A|FullyQualifiedName~Cratis.P.Scoped.for_B", "-noLogo", "-noColor", "-result-trx", str(proxy_results / "ProxyGenerator.Specs.trx")])
            expect("proxy working directory", cwd, root)
            expect("proxy child exit handling", check, False)
            if content is not None:
                write(Path(command[-1]), content)
            return subprocess.CompletedProcess(command, code)

        with patch("subprocess.run", side_effect=fake_proxy):
            expect_status(label, quietly(runner.run, proxy_document, "proxy-1"), expected)
        shutil.rmtree(proxy_results)
    with patch("subprocess.run") as child:
        expect_status("invalid proxy shard", quietly(runner.run, proxy_document, "missing"), 2)
        empty_proxy = json.loads(json.dumps(valid))
        empty_proxy["proxyShards"][0].update(namespaces=[])
        expect_status("empty proxy selection", quietly(runner.run, empty_proxy, "proxy-1"), 2)
        expect("invalid selection never launches an unfiltered child", child.called, False)
        proxy_results.mkdir()
        write(proxy_results / "ProxyGenerator.Specs.trx", selected)
        expect_status("proxy stale results", quietly(runner.run, proxy_document, "proxy-1"), 2)
        expect("stale proxy results never launch", child.called, False)


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
        write(root / SHARDS_FILE, json.dumps(valid))
        self_test_trx_population(load_shards(root), results, expect_status, quietly)
        self_test_trx_validation(results, expect_status, quietly)
        self_test_ordinary_runner(root, valid, expect, quietly)
        self_test_proxy_runner(root, valid, expect, quietly)
    finally:
        shutil.rmtree(root, ignore_errors=True)

    for failure in failures:
        print(f"::error title=ci-test-shards self-test::{failure}")
    print(f"self-test: {cases - len(failures)} of {cases} planted cases behaved as expected")
    return 1 if failures else 0


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("command", nargs="?", default="check", choices=["check", "matrix", "filter", "projects", "verify-trx", "run"])
    parser.add_argument("--self-test", action="store_true")
    parser.add_argument("--shard")
    parser.add_argument("--results", type=Path)
    parser.add_argument("--configuration", choices=["Debug", "Release"], default="Release")
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
            raise CannotRun(f"'{arguments.command}' needs --results")
        handlers = {
            "run": lambda: SpecificationRunner(arguments.root.resolve(), arguments.results, arguments.configuration).run(document, arguments.shard),
            "verify-trx": lambda: verify_trx(document, arguments.shard, arguments.results),
        }
        return handlers[arguments.command]()
    except CannotRun as error:
        print(f"::error title=ci-test-shards::{error}")
        return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
