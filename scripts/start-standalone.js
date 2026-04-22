const fs = require('fs');
const path = require('path');
const { spawn } = require('child_process');

const projectRoot = process.cwd();
const standaloneDir = path.join(projectRoot, '.next', 'standalone');
const standaloneServer = path.join(standaloneDir, 'server.js');

function copyDirectory(source, destination) {
  if (!fs.existsSync(source)) {
    return;
  }

  fs.mkdirSync(path.dirname(destination), { recursive: true });
  fs.cpSync(source, destination, { recursive: true, force: true });
}

if (!fs.existsSync(standaloneServer)) {
  console.error(`Standalone server not found at ${standaloneServer}`);
  process.exit(1);
}

// Next.js standalone output expects static assets and public files to live
// alongside the generated server bundle.
copyDirectory(path.join(projectRoot, '.next', 'static'), path.join(standaloneDir, '.next', 'static'));
copyDirectory(path.join(projectRoot, 'public'), path.join(standaloneDir, 'public'));

const child = spawn(process.execPath, ['server.js'], {
  cwd: standaloneDir,
  stdio: 'inherit',
  env: {
    ...process.env,
    HOSTNAME: process.env.HOSTNAME || '0.0.0.0',
  },
});

child.on('exit', (code, signal) => {
  if (signal) {
    process.kill(process.pid, signal);
    return;
  }

  process.exit(code ?? 0);
});
