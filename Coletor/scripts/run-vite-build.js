#!/usr/bin/env node

import { spawnSync } from 'node:child_process';

const cliArgs = process.argv.slice(2);
let mode;
const passthrough = [];

for (let i = 0; i < cliArgs.length; i += 1) {
  const arg = cliArgs[i];
  if (arg === '--mode' && cliArgs[i + 1]) {
    mode = cliArgs[i + 1];
    i += 1;
    continue;
  }

  if (arg.startsWith('--mode=')) {
    mode = arg.split('=')[1];
    continue;
  }

  if (!mode && !arg.startsWith('-') && passthrough.length === 0) {
    // Support `npm run build --mode qa` where npm forwards only the value.
    mode = arg;
    continue;
  }

  passthrough.push(arg);
}

if (!mode) {
  const envMode = process.env.npm_config_mode;
  if (envMode && envMode !== 'true') {
    mode = envMode;
  }
}

const args = ['build'];

if (mode) {
  args.push('--mode', mode);
}

args.push(...passthrough);

const result = spawnSync('vite', args, {
  stdio: 'inherit',
  shell: true,
});

if (result.status !== 0) {
  process.exit(result.status ?? 1);
}
