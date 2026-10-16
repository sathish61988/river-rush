const { spawn } = require('child_process');
const fs = require('fs');
const path = require('path');
const os = require('os');

const edgeExe = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";
const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'edge-capture-'));

async function run() {
  const edgeProc = spawn(edgeExe, [
    '--headless=new',
    '--remote-debugging-port=9225',
    '--disable-gpu',
    `--user-data-dir=${tempDir}`,
    '--window-size=1920,1080',
    'http://localhost:8080/index.html'
  ]);

  await new Promise(r => setTimeout(r, 2000));

  try {
    const tabs = await fetch('http://localhost:9225/json').then(r => r.json());
    const riverTab = tabs.find(t => t.url.includes('localhost:8080'));
    if (!riverTab) throw new Error("Could not find River Rush tab");

    const ws = new WebSocket(riverTab.webSocketDebuggerUrl);
    await new Promise(r => ws.onopen = r);

    let msgId = 1;
    const pending = new Map();
    ws.onmessage = (event) => {
      const data = JSON.parse(event.data);
      if (data.id && pending.has(data.id)) {
        pending.get(data.id)(data);
        pending.delete(data.id);
      }
    };

    function send(method, params = {}) {
      return new Promise((resolve, reject) => {
        const id = msgId++;
        pending.set(id, (data) => {
          if (data.error) reject(data.error);
          else resolve(data.result);
        });
        ws.send(JSON.stringify({ id, method, params }));
      });
    }

    async function evaluate(expr) {
      const res = await send('Runtime.evaluate', { expression: expr, returnByValue: true, awaitPromise: true });
      return res.result ? res.result.value : null;
    }

    await send('Page.enable');
    await new Promise(r => setTimeout(r, 1000));

    const screens = [
      { key: 'main-menu', name: '01_main_menu_current.png' },
      { key: 'boat-select', name: '02_boat_select_current.png' },
      { key: 'map-select', name: '03_map_select_current.png' },
      { key: 'hud', name: '04_hud_current.png' },
      { key: 'pause', name: '05_pause_current.png' },
      { key: 'gameover', name: '06_gameover_current.png' }
    ];

    const outDir = path.join(__dirname, 'preview', 'debug_screens');
    if (!fs.existsSync(outDir)) fs.mkdirSync(outDir, { recursive: true });

    for (const s of screens) {
      await evaluate(`switchScreen('${s.key}')`);
      await new Promise(r => setTimeout(r, 400));
      const ss = await send('Page.captureScreenshot', { format: 'png' });
      const imgBuf = Buffer.from(ss.data, 'base64');
      fs.writeFileSync(path.join(outDir, s.name), imgBuf);
      console.log(`Saved ${s.name}`);
    }

    ws.close();
    edgeProc.kill();
    try { fs.rmSync(tempDir, { recursive: true, force: true }); } catch (e) {}
    console.log("All screens captured successfully!");
  } catch (err) {
    edgeProc.kill();
    try { fs.rmSync(tempDir, { recursive: true, force: true }); } catch (e) {}
    throw err;
  }
}

run().catch(e => {
  console.error(e);
  process.exit(1);
});
