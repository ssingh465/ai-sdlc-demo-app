import path from 'node:path';
import { fileURLToPath } from 'node:url';

const fileKeys = new Set(['filePath', 'file_path', 'path', 'paths', 'files', 'uri']);

function repoRelativePath(candidate) {
  if (typeof candidate !== 'string' || candidate.length === 0) {
    return null;
  }

  const value = candidate.trim();
  if (
    value.length === 0 ||
    /[\r\n]/.test(value) ||
    /^(?:dotnet|git|npm|npx|node|powershell|pwsh|cmd|bash)\b/i.test(value) ||
    /(?:&&|\|\||[|<>])/.test(value)
  ) {
    return null;
  }

  try {
    const root = process.cwd();
    const absolutePath = /^file:/i.test(value)
      ? fileURLToPath(value)
      : path.resolve(root, value);
    const relativePath = path.relative(root, absolutePath);

    if (
      relativePath === '' ||
      relativePath === '..' ||
      relativePath.startsWith(`..${path.sep}`) ||
      path.isAbsolute(relativePath)
    ) {
      return null;
    }

    return relativePath.split(path.sep).join('/');
  } catch {
    return null;
  }
}

function patchStrings(toolArgs) {
  const strings = [];
  if (typeof toolArgs === 'string') {
    strings.push(toolArgs);
  } else if (toolArgs && typeof toolArgs === 'object') {
    for (const key of ['input', 'patch']) {
      if (typeof toolArgs[key] === 'string') {
        strings.push(toolArgs[key]);
      }
    }
  }
  return strings;
}

function patchFiles(toolArgs) {
  const files = new Set();
  let containsFileHeader = false;
  for (const patch of patchStrings(toolArgs)) {
    for (const line of patch.split(/\r?\n/)) {
      const match = /^\*\*\* (?:Update|Add) File:\s*(.*?)\s*$/.exec(line);
      if (match) {
        containsFileHeader = true;
        const relativePath = repoRelativePath(match[1]);
        if (relativePath) {
          files.add(relativePath);
        }
      }
    }
  }
  return containsFileHeader ? [...files] : null;
}

function collectFileValues(value, key, files) {
  if (fileKeys.has(key)) {
    const candidates = Array.isArray(value) ? value : [value];
    for (const candidate of candidates) {
      const relativePath = repoRelativePath(candidate);
      if (relativePath) {
        files.add(relativePath);
      }
    }
    return;
  }

  if (Array.isArray(value)) {
    value.forEach((item) => collectFileValues(item, '', files));
  } else if (value && typeof value === 'object') {
    for (const [childKey, childValue] of Object.entries(value)) {
      collectFileValues(childValue, childKey, files);
    }
  }
}

export function editedFiles(toolName, toolArgs) {
  try {
    const patchPaths = patchFiles(toolArgs);
    if (patchPaths !== null) {
      return patchPaths;
    }

    const files = new Set();
    collectFileValues(toolArgs, '', files);
    return [...files];
  } catch {
    return [];
  }
}
