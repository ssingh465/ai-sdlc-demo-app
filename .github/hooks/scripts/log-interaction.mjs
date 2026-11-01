import os from 'node:os';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { appendFileSync, mkdirSync } from 'node:fs';
import { editedFiles } from './edited-files.mjs';

const truncate = (value) => {
  const text = toText(value);
  const marker = '…[truncated]';
  return text.length > 2000
    ? `${text.slice(0, 2000 - marker.length)}${marker}`
    : text;
};

function toText(value) {
  if (typeof value === 'string') {
    return value;
  }

  if (value === null || value === undefined) {
    return '';
  }

  try {
    return JSON.stringify(value);
  } catch {
    return String(value);
  }
}

function firstValue(source, names) {
  for (const name of names) {
    if (source?.[name] !== undefined && source[name] !== null) {
      return source[name];
    }
  }
  return null;
}

function redactString(value) {
  let redacted = value;
  const home = os.homedir();
  const encodedHome = pathToFileURL(home).href.replace(/:/g, '%3A');
  const homeVariants = new Set([
    home,
    home.replace(/\\/g, '/'),
    home.replace(/\//g, '\\'),
    encodeURI(home).replace(/:/g, '%3A'),
    encodedHome.replace(/^file:/i, ''),
    encodedHome,
  ]);

  for (const variant of homeVariants) {
    if (variant) {
      redacted = redacted.replace(
        new RegExp(variant.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'gi'),
        '~',
      );
    }
  }

  const username = os.userInfo().username;
  if (username) {
    redacted = redacted.replace(
      new RegExp(username.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'gi'),
      'user',
    );
  }

  return redacted
    .replace(/\bgithub_pat_[A-Za-z0-9_]+/gi, '[redacted]')
    .replace(/\bgho_[A-Za-z0-9_]+/gi, '[redacted]')
    .replace(/\bghp_[A-Za-z0-9_]+/gi, '[redacted]')
    .replace(/\bsk-[A-Za-z0-9_-]+/gi, '[redacted]')
    .replace(/\bBearer\s+\S+/gi, 'Bearer [redacted]');
}

function redactValues(value) {
  if (typeof value === 'string') {
    return redactString(value);
  }
  if (Array.isArray(value)) {
    return value.map(redactValues);
  }
  if (value && typeof value === 'object') {
    return Object.fromEntries(
      Object.entries(value).map(([key, item]) => [
        redactString(key),
        redactValues(item),
      ]),
    );
  }
  return value;
}

async function readStdin() {
  let payload = '';
  for await (const chunk of process.stdin) {
    payload += chunk;
  }
  return JSON.parse(payload);
}

try {
  const payload = await readStdin();
  const event = process.argv[2] ?? null;
  const toolArgs = firstValue(payload, ['toolArgs', 'tool_input']);
  const initialPrompt = firstValue(payload, ['initialPrompt', 'initial_prompt']);
  const prompt = event === 'userPromptSubmitted'
    ? firstValue(payload, ['prompt', 'userPrompt', 'user_prompt', 'text'])
    : null;
  const result = firstValue(payload, [
    'toolResult',
    'tool_response',
    'subagentResponse',
    'subagent_response',
    'agentResponse',
    'agent_response',
    'response',
    'result',
    'output',
    'stopReason',
    'stop_reason',
    'reason',
  ]);

  const record = {
    ts: new Date().toISOString(),
    sessionId: firstValue(payload, ['sessionId', 'session_id']),
    event,
    agent: firstValue(payload, ['agentName', 'agent_name']),
    prompt: prompt === null ? null : truncate(prompt),
    tool: firstValue(payload, ['toolName', 'tool_name']),
    input: toolArgs === null && initialPrompt === null
      ? null
      : truncate(toolArgs ?? initialPrompt),
    output: result === null ? null : truncate(result),
    files: editedFiles(firstValue(payload, ['toolName', 'tool_name']), toolArgs),
  };

  const logDirectory = path.join(process.cwd(), 'logs');
  mkdirSync(logDirectory, { recursive: true });
  appendFileSync(
    path.join(logDirectory, 'ai-interactions.jsonl'),
    `${JSON.stringify(redactValues(record))}\n`,
    'utf8',
  );
} catch {
  // Hook failures must not block the tool or session.
}
