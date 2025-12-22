#!/usr/bin/env node

const fs = require('fs');
const path = require('path');
const https = require('https');
const { execSync } = require('child_process');

const rootDir = __dirname;
const packageJsonPath = path.join(rootDir, 'package.json');

function readJson(filePath) {
    if (!fs.existsSync(filePath)) {
        return null;
    }

    return JSON.parse(fs.readFileSync(filePath, 'utf-8'));
}

function writeJson(filePath, data) {
    fs.writeFileSync(filePath, `${JSON.stringify(data, null, 2)}\n`, 'utf-8');
}

function getLatestNpmVersion(packageName) {
    try {
        const result = execSync(`npm view ${packageName} version`, { encoding: 'utf-8' }).trim();
        return result || null;
    } catch (error) {
        console.warn(`Could not fetch npm version for ${packageName}.`);
        return null;
    }
}

function getJson(url) {
    return new Promise((resolve) => {
        https
            .get(url, (response) => {
                if (response.statusCode !== 200) {
                    resolve(null);
                    return;
                }

                let data = '';
                response.on('data', (chunk) => {
                    data += chunk;
                });

                response.on('end', () => {
                    try {
                        resolve(JSON.parse(data));
                    } catch {
                        resolve(null);
                    }
                });
            })
            .on('error', () => resolve(null));
    });
}

async function getLatestNugetVersion(packageName) {
    const packageId = packageName.toLowerCase();
    const url = `https://api.nuget.org/v3-flatcontainer/${packageId}/index.json`;

    const result = await getJson(url);
    if (!result || !Array.isArray(result.versions) || result.versions.length === 0) {
        return null;
    }

    const stableVersions = result.versions.filter((version) => !version.includes('-'));
    return stableVersions[stableVersions.length - 1] || result.versions[result.versions.length - 1] || null;
}

function updatePackageReference(content, packageName, version) {
    if (!version) {
        return content;
    }

    const withVersion = new RegExp(`(<PackageReference\\s+[^>]*Include="${packageName}"[^>]*Version=")([^"]+)("[^>]*>)`, 'm');
    if (withVersion.test(content)) {
        return content.replace(withVersion, `$1${version}$3`);
    }

    const selfClosing = new RegExp(`(<PackageReference\\s+[^>]*Include="${packageName}"[^>]*)(/>)`, 'm');
    if (selfClosing.test(content)) {
        return content.replace(selfClosing, `$1 Version="${version}" />`);
    }

    return content;
}

async function updateCsproj() {
    const csprojFile = fs.readdirSync(rootDir).find((file) => file.endsWith('.csproj'));
    if (!csprojFile) {
        console.warn('No .csproj file found. Skipping NuGet package updates.');
        return;
    }

    const csprojPath = path.join(rootDir, csprojFile);
    const content = fs.readFileSync(csprojPath, 'utf-8');

    const cratisVersion = await getLatestNugetVersion('Cratis');
    const proxyGeneratorVersion = await getLatestNugetVersion('Cratis.Arc.ProxyGenerator.Build');

    let updatedContent = updatePackageReference(content, 'Cratis', cratisVersion);
    updatedContent = updatePackageReference(updatedContent, 'Cratis.Arc.ProxyGenerator.Build', proxyGeneratorVersion);

    if (updatedContent !== content) {
        fs.writeFileSync(csprojPath, updatedContent, 'utf-8');
        console.log('Updated NuGet package versions in project file.');
    } else {
        console.log('No NuGet package updates were applied.');
    }
}

async function updateFrontendDependencies() {
    const packageJson = readJson(packageJsonPath);
    if (!packageJson) {
        console.log('No package.json found. Skipping frontend dependency updates.');
        return;
    }

    const arcVersion = getLatestNpmVersion('@cratis/arc');
    const arcReactVersion = getLatestNpmVersion('@cratis/arc.react');

    const dependencies = packageJson.dependencies || {};

    if (arcVersion && dependencies['@cratis/arc']) {
        dependencies['@cratis/arc'] = `^${arcVersion}`;
        console.log(`Updated @cratis/arc to ^${arcVersion}`);
    }

    if (arcReactVersion && dependencies['@cratis/arc.react']) {
        dependencies['@cratis/arc.react'] = `^${arcReactVersion}`;
        console.log(`Updated @cratis/arc.react to ^${arcReactVersion}`);
    }

    packageJson.dependencies = dependencies;
    writeJson(packageJsonPath, packageJson);
}

async function main() {
    try {
        console.log('Updating Cratis dependencies to latest versions...');
        await updateCsproj();
        await updateFrontendDependencies();
        console.log('Dependency update complete.');
    } catch (error) {
        console.error(`Failed to update dependencies: ${error.message}`);
        process.exit(1);
    }
}

main();
