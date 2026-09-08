#!/usr/bin/env node
const net = require('net');
const { spawn } = require('child_process');

const CANDIDATE_PORTS = [4200, 4300, 4400, 4500];

function isPortFree(port) {
  return new Promise((resolve) => {
    const tester = net.createServer();
    tester.once('error', () => resolve(false));
    tester.once('listening', () => tester.close(() => resolve(true)));
    tester.listen(port, '0.0.0.0');
  });
}

async function findFreePort() {
  for (const port of CANDIDATE_PORTS) {
    if (await isPortFree(port)) {
      return port;
    }
  }
  return null;
}

async function main() {
  const port = await findFreePort();
  if (!port) {
    console.error(
      `All candidate ports are in use (${CANDIDATE_PORTS.join(', ')}). Free one of them or pass --port explicitly.`
    );
    process.exit(1);
  }

  if (port !== CANDIDATE_PORTS[0]) {
    console.log(`Port ${CANDIDATE_PORTS[0]} is busy, using ${port} instead.`);
  }

  const extraArgs = process.argv.slice(2);
  const ng = spawn(
    'ng',
    ['serve', '--port', String(port), ...extraArgs],
    { stdio: 'inherit', shell: true }
  );

  ng.on('exit', (code) => process.exit(code ?? 0));
}

main();
