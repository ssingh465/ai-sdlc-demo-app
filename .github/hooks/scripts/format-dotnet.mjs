import path from 'node:path';
import { execFileSync } from 'node:child_process';
import { existsSync, readdirSync } from 'node:fs';

function runGit(args) {
  try {
    return execFileSync('git', args, {
      cwd: process.cwd(),
      encoding: 'utf8',
      stdio: ['ignore', 'pipe', 'ignore'],
    });
  } catch {
    return '';
  }
}

function findProjectFile(filePath) {
  let directory = path.dirname(filePath);
  const root = process.cwd();

  while (directory === root || directory.startsWith(`${root}${path.sep}`)) {
    if (existsSync(directory)) {
      try {
        const projectFiles = readdirSync(directory)
          .filter((name) => name.toLowerCase().endsWith('.csproj'))
          .sort();
        if (projectFiles.length > 0) {
          return path.join(directory, projectFiles[0]);
        }
      } catch {
        return null;
      }
    }

    const parent = path.dirname(directory);
    if (parent === directory) {
      break;
    }
    directory = parent;
  }

  return null;
}

try {
  const root = process.cwd();
  const files = new Set([
    ...runGit(['diff', '--name-only']).split(/\r?\n/),
    ...runGit(['ls-files', '--others', '--exclude-standard']).split(/\r?\n/),
  ]);
  const filesByProject = new Map();

  for (const relativeFile of files) {
    if (!relativeFile.toLowerCase().endsWith('.cs')) {
      continue;
    }

    const filePath = path.resolve(root, relativeFile);
    const projectFile = findProjectFile(filePath);
    if (!projectFile) {
      continue;
    }

    const projectFiles = filesByProject.get(projectFile) ?? [];
    projectFiles.push(relativeFile);
    filesByProject.set(projectFile, projectFiles);
  }

  for (const [projectFile, projectFiles] of filesByProject) {
    try {
      execFileSync(
        'dotnet',
        ['format', 'whitespace', projectFile, '--include', ...projectFiles],
        { cwd: root, stdio: 'ignore' },
      );
    } catch {
      // Formatting is best-effort and must not block the agent.
    }
  }
} catch {
  // Formatting must not block the agent.
}
