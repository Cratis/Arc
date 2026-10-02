#!/usr/bin/env node
/* eslint-disable header/header */

if (process.argv.length < 3) {
    console.log('You have to specify what workspace task to run on all')
    console.log('\nUsage: run-task-on-workspaces [task] [arguments]');
    console.log('\nExamples of tasks: build|test|ci');
    process.exit(1);
    return;
}

const path = require('path');
const fs = require('fs');
const spawn = require('child_process').spawnSync;
const editJsonFile = require('edit-json-file');
const rootPackageJson = require('./package.json')
const glob = require('glob').sync;

const workspaces = {};
const workspacePackages = new Map();

const workspaceIgnorePatterns = rootPackageJson.workspaces
    .filter(_ => _.startsWith('!'))
    .map(_ => _.substring(1));

const distFolder = `dist${path.sep}`
for (const workspaceDef of rootPackageJson.workspaces) {
    if (workspaceDef.startsWith('!')) {
        console.log(`Skipping negated workspace definition '${workspaceDef}' \n`);
        continue;
    }

    console.log(`Getting packages for workspace definition '${workspaceDef}' \n`);

    const pattern = path.join(workspaceDef, '**','package.json');

    const packages = glob(pattern, { 
        cwd: `${process.cwd()}`,
        ignore: [
            ...workspaceIgnorePatterns,
            `**${path.sep}${distFolder}**`,
            '**/node_modules/**'
        ]
    });

    if (packages.length === 0) {
        console.log(`  No packages found for workspace definition '${workspaceDef}' \n`);
        continue;
    }

    packages.forEach(_ => {
        const package = JSON.parse(fs.readFileSync(_).toString());
        workspaces[package.name] = path.dirname(_);
        workspacePackages.set(package.name, package);

        console.log(`Including workspace '${package.name}' at '${workspaces[package.name]}'`);
    });
}

console.log('');

const task = process.argv[2];
args = process.argv.slice(3, process.argv.length);

console.log(`Performing '${task}' on workspaces`);

if (args.length > 0) {
    console.log(`  Using args : ${args}`);
}

console.log('');

const workspaceNames = Object.keys(workspaces);
let workspaceNamesToRun = workspaceNames;
let publishDependencies = new Map();
if (task === 'publish-version' && args.length === 1) {
    try {
        const getPublishPlan = require('./scripts/workspace-publish-order.cjs');
        const plan = getPublishPlan(workspacePackages);
        workspaceNamesToRun = plan.names;
        publishDependencies = plan.dependencies;
    } catch (error) {
        console.error(error.message);
        process.exit(1);
        return;
    }
}

function updateDependencyVersionsFromLocalWorkspaces(file, packageJson, version) {
    const dependencyFields = Object.keys(packageJson).filter(_ => _.endsWith('dependencies') || _.endsWith('Dependencies'));
    for (let field of dependencyFields) {
        const dependencies = packageJson[field] ?? {};
        const fileDependencies = file.get(field);

        for (let dependencyName of Object.keys(dependencies)) {
            if (workspaceNames.includes(dependencyName)) {
                console.log(`Updating workspace ${field} '${dependencyName}' to version ${version}`);
                fileDependencies[dependencyName] = version;
            }
        }

        file.set(field, fileDependencies);
    }
}

const publishFailures = [];
const blockedPublishes = new Set();

for (const workspaceName of workspaceNamesToRun) {
    const workspaceRelativeLocation = workspaces[workspaceName];
    const workspaceAbsoluteLocation = path.join(process.cwd(), workspaceRelativeLocation);
    const packageJsonFile = path.join(workspaceAbsoluteLocation, 'package.json');

    if (fs.existsSync(packageJsonFile)) {
        const file = editJsonFile(packageJsonFile, { stringify_width: 4 });
        const packageJson = file.toObject();
        if (packageJson.private === true) {
            console.log(`Workspace private '${workspaceName}' at '${workspaceRelativeLocation}'`);
            continue;
        }

        if (task === 'publish-version') {
            if (args.length === 1) {
                const failedDependencies = publishDependencies.get(workspaceName).filter(_ => blockedPublishes.has(_));
                if (failedDependencies.length > 0) {
                    console.log(`Skipping publish of workspace '${workspaceName}' - dependencies failed to publish: ${failedDependencies.join(', ')}`);
                    blockedPublishes.add(workspaceName);
                    continue;
                }

                const version = args[0];
                const isVersionPublished = require('./scripts/workspace-version-is-published.cjs');
                file.set('version', version);
                updateDependencyVersionsFromLocalWorkspaces(file, packageJson, version);
                file.save();

                if (isVersionPublished(spawn, workspaceName, version, packageJson, workspaceAbsoluteLocation)) {
                    console.log(`Skipping publish of workspace '${workspaceName}' - version ${version} is already published`);
                    continue;
                }

                const targetReadMe = path.join(workspaceAbsoluteLocation, 'README.md');

                if (!fs.existsSync(targetReadMe)) {
                    fs.copyFileSync(path.join(process.cwd(), "README.md"), targetReadMe);
                }

                console.log(`Publishing workspace '${workspaceName}' at '${workspaceRelativeLocation}'`);
                const result = spawn('npm', ['publish', '--provenance'], { cwd: workspaceAbsoluteLocation });
                console.log(result.stdout?.toString() ?? '');
                console.log(result.stderr?.toString() ?? '');
                if (result.status !== 0) {
                    if (isVersionPublished(spawn, workspaceName, version, packageJson, workspaceAbsoluteLocation)) {
                        console.log(`Workspace '${workspaceName}' version ${version} is published despite the publish error`);
                        continue;
                    }
                    // Keep publishing independent packages, but never publish a dependent
                    // that would pin a version which failed to reach the registry.
                    if (result.error) console.log(result.error.message);
                    console.log(`Error publishing workspace '${workspaceName}' - continuing with remaining workspaces`);
                    publishFailures.push(workspaceName);
                    blockedPublishes.add(workspaceName);
                }
            }
        } else {

            if (!packageJson.scripts || !packageJson.scripts.hasOwnProperty(task)) {
                console.log(`Skipping workspace '${workspaceName}' - no script with name '${task}'`);
                continue;
            }

            console.log(`Workspace '${workspaceName}' at '${workspaceRelativeLocation}'`);

            const result = spawn('yarn', [task], { cwd: workspaceAbsoluteLocation });
            console.log(result.stdout.toString());
            if (result.status !== 0) {
                console.log(`Error running task '${task}' on workspace '${workspaceName}'`);
                console.log(result.stderr.toString());
                process.exit(1);
                return;
            }
        }
    }
}

if (publishFailures.length > 0) {
    console.log(`\n${publishFailures.length} workspace(s) failed to publish: ${publishFailures.join(', ')}`);
    process.exit(1);
}
