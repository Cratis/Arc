// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { mkdtempSync, readFileSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';
import { compareVersions, parseDowngrades, raisePins } from './raise-central-cratis-pins.mjs';

const script = fileURLToPath(new URL('./raise-central-cratis-pins.mjs', import.meta.url));

const props = `<Project>
  <ItemGroup>
    <PackageVersion Include="Cratis.Chronicle" Version="19.163.0" />
    <PackageVersion Include="Cratis.Fundamentals" Version="7.34.0" />
    <PackageVersion Include="Cratis.Metrics.Roslyn" Version="7.34.0" />
    <PackageVersion Include="Newtonsoft.Json" Version="13.0.1" />
  </ItemGroup>
</Project>
`;

function downgrade(name, required, pinned, project = 'Arc.Core') {
    return `/src/${project}.csproj : error NU1109: Detected package downgrade: ${name} from ${required} to centrally defined ${pinned}. Update the centrally managed package version to a higher version. \n` +
        ` ${project} -> Cratis.Chronicle 19.163.0 -> ${name} (>= ${required}) \n ${project} -> ${name} (>= ${pinned})\n`;
}

function run(log, content = props) {
    const directory = mkdtempSync(join(tmpdir(), 'raise-pins-'));
    const logPath = join(directory, 'restore.log');
    const propsPath = join(directory, 'Directory.Packages.props');
    writeFileSync(logPath, log);
    writeFileSync(propsPath, content);
    const result = spawnSync(process.execPath, [script, '--log', logPath, '--props', propsPath], { encoding: 'utf8' });
    return { status: result.status, stdout: result.stdout, props: readFileSync(propsPath, 'utf8') };
}

test('raises a lower central Cratis pin to the required minimum and names it', () => {
    const result = run(downgrade('Cratis.Fundamentals', '7.35.0', '7.34.0'));
    assert.equal(result.status, 0);
    assert.match(result.props, /Include="Cratis.Fundamentals" Version="7.35.0"/);
    assert.match(result.stdout, /Raised Cratis.Fundamentals from 7.34.0 to 7.35.0\./);
});

test('leaves pins unrelated to the reported conflict untouched', () => {
    const result = run(downgrade('Cratis.Fundamentals', '7.35.0', '7.34.0'));
    assert.equal(result.props, props.replace('"Cratis.Fundamentals" Version="7.34.0"', '"Cratis.Fundamentals" Version="7.35.0"'));
});

test('refuses without changes when any conflict is not a Cratis package', () => {
    const log = downgrade('Cratis.Fundamentals', '7.35.0', '7.34.0') + downgrade('Newtonsoft.Json', '13.0.3', '13.0.1');
    const result = run(log);
    assert.equal(result.status, 2);
    assert.equal(result.props, props);
    assert.match(result.stdout, /Cannot raise Newtonsoft.Json \(not a Cratis package\)/);
});

test('never lowers a pin', () => {
    const result = run(downgrade('Cratis.Metrics.Roslyn', '7.33.0', '7.34.0'));
    assert.equal(result.status, 2);
    assert.equal(result.props, props);
});

test('refuses a Cratis conflict without a central pin', () => {
    const result = run(downgrade('Cratis.Unknown', '1.2.0', '1.1.0'));
    assert.equal(result.status, 2);
    assert.equal(result.props, props);
    assert.match(result.stdout, /no central pin/);
});

test('takes the highest required version across projects', () => {
    const log = downgrade('Cratis.Fundamentals', '7.35.0', '7.34.0', 'Arc.Core') + downgrade('Cratis.Fundamentals', '7.36.1', '7.34.0', 'Arc');
    assert.deepEqual(parseDowngrades(log), [{ name: 'Cratis.Fundamentals', required: '7.36.1', pinned: '7.34.0' }]);
    const result = run(log);
    assert.equal(result.status, 0);
    assert.match(result.props, /Include="Cratis.Fundamentals" Version="7.36.1"/);
});

test('raises several Cratis pins in one pass', () => {
    const log = downgrade('Cratis.Fundamentals', '7.35.0', '7.34.0') + downgrade('Cratis.Metrics.Roslyn', '7.35.0', '7.34.0');
    const { raised } = raisePins(props, parseDowngrades(log));
    assert.deepEqual(raised.map(pin => pin.name), ['Cratis.Fundamentals', 'Cratis.Metrics.Roslyn']);
});

test('fails with nothing to raise when the log has no downgrade', () => {
    const result = run('error NU1101: Unable to find package Cratis.Chronicle.');
    assert.equal(result.status, 2);
    assert.equal(result.props, props);
});

test('refuses without changes when the log also holds a non-central downgrade', () => {
    const log = downgrade('Cratis.Fundamentals', '7.35.0', '7.34.0') +
        '/src/Arc.csproj : error NU1605: Detected package downgrade: Cratis.Metrics.Roslyn from 7.35.0 to 7.34.0. Reference the package directly from the project to select a different version.\n';
    const result = run(log);
    assert.equal(result.status, 2);
    assert.equal(result.props, props);
});

test('refuses a required version that is not a NuGet version', () => {
    const result = run(downgrade('Cratis.Fundamentals', '7.35.0-x"/><Injected/>', '7.34.0'));
    assert.equal(result.status, 2);
    assert.equal(result.props, props);
});

test('compares prerelease labels case-insensitively', () => {
    assert.equal(compareVersions('1.0.0-BETA', '1.0.0-alpha'), 1);
    assert.equal(compareVersions('1.0.0-Alpha', '1.0.0-alpha'), 0);
});

test('compares versions with prerelease ordering', () => {
    assert.equal(compareVersions('7.35.0', '7.34.9'), 1);
    assert.equal(compareVersions('7.35.0-pr1.2', '7.35.0'), -1);
    assert.equal(compareVersions('7.35.0-pr1.10', '7.35.0-pr1.9'), 1);
    assert.equal(compareVersions('7.35', '7.35.0'), 0);
});
