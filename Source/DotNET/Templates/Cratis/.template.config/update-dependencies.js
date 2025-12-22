#!/usr/bin/env node

const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

const sourceDir = path.join(__dirname, '..', 'Source');
const packageJsonPath = path.join(sourceDir, 'package.json');

async function getLatestVersion(packageName) {
  try {
    const result = execSync(`npm view ${packageName} version`, { encoding: 'utf-8' }).trim();
    return result;
  } catch (error) {
    console.warn(`Could not fetch version for ${packageName}, using default`);
    return null;
  }
}

async function updateDependencies() {
  try {
    console.log('Updating Cratis packages to latest versions...');
    
    // Read package.json
    const packageJson = JSON.parse(fs.readFileSync(packageJsonPath, 'utf-8'));
    
    // Get latest versions
    const arcVersion = await getLatestVersion('@cratis/arc');
    const arcReactVersion = await getLatestVersion('@cratis/arc.react');
    
    // Update if versions were found
    if (arcVersion && packageJson.dependencies['@cratis/arc']) {
      packageJson.dependencies['@cratis/arc'] = `^${arcVersion}`;
      console.log(`Updated @cratis/arc to ^${arcVersion}`);
    }
    
    if (arcReactVersion && packageJson.dependencies['@cratis/arc.react']) {
      packageJson.dependencies['@cratis/arc.react'] = `^${arcReactVersion}`;
      console.log(`Updated @cratis/arc.react to ^${arcReactVersion}`);
    }
    
    // Write back package.json
    fs.writeFileSync(packageJsonPath, JSON.stringify(packageJson, null, 2) + '\n', 'utf-8');
    
    console.log('Dependencies updated successfully!');
  } catch (error) {
    console.error('Error updating dependencies:', error.message);
    process.exit(1);
  }
}

updateDependencies();
