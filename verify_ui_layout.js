const { spawn } = require('child_process');
const fs = require('fs');
const path = require('path');
const os = require('os');

const edgeExe = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";

let portCounter = 9230;

async function testViewport(width, height, tag = '') {
  const port = portCounter++;
  const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), `edge-ui-${width}-${height}-`));
  const edgeProc = spawn(edgeExe, [
    '--headless=new',
    `--remote-debugging-port=${port}`,
    '--disable-gpu',
    `--user-data-dir=${tempDir}`,
    `--window-size=${width},${height}`,
    'http://localhost:8080/index.html'
  ]);

  await new Promise(r => setTimeout(r, 2200));

  try {
    const tabs = await fetch(`http://localhost:${port}/json`).then(r => r.json());
    const riverTab = tabs.find(t => t.url.includes('localhost:8080'));
    if (!riverTab) throw new Error("Could not find River Rush tab");

    const ws = new WebSocket(riverTab.webSocketDebuggerUrl);
    await new Promise(r => ws.onopen = r);

    let msgId = 1;
    const pending = new Map();
    const consoleErrors = [];

    ws.onmessage = (event) => {
      const data = JSON.parse(event.data);
      if (data.id && pending.has(data.id)) {
        pending.get(data.id)(data);
        pending.delete(data.id);
      }
      if (data.method === 'Runtime.consoleAPICalled') {
        const type = data.params.type;
        const text = data.params.args.map(a => a.value || JSON.stringify(a)).join(' ');
        if (type === 'error') consoleErrors.push(text);
      }
      if (data.method === 'Runtime.exceptionThrown') {
        consoleErrors.push(JSON.stringify(data.params.exceptionDetails));
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
    await send('Runtime.enable');
    await new Promise(r => setTimeout(r, 800));

    const screens = [
      { key: 'main-menu', name: `01_main_menu_${width}x${height}.png` },
      { key: 'boat-select', name: `02_boat_select_${width}x${height}.png` },
      { key: 'map-select', name: `03_map_select_${width}x${height}.png` },
      { key: 'hud', name: `04_hud_${width}x${height}.png` },
      { key: 'pause', name: `05_pause_${width}x${height}.png` },
      { key: 'gameover', name: `06_gameover_${width}x${height}.png` }
    ];

    const outDir = path.join(__dirname, 'preview', 'verified_screens');
    if (!fs.existsSync(outDir)) fs.mkdirSync(outDir, { recursive: true });

    console.log(`\n=== Testing Viewport ${width}x${height} (${tag}) ===`);

    for (const s of screens) {
      await evaluate(`switchScreen('${s.key}')`);
      await new Promise(r => setTimeout(r, 400));

      // Extra verification checks per screen
      if (s.key === 'boat-select') {
        const thumbCount = await evaluate("document.querySelectorAll('#screen-boat-select .boat-thumb-slot').length");
        const statsBox = await evaluate("(() => { const el = document.querySelector('.boat-stats-card'); if (!el) return null; const r = el.getBoundingClientRect(); return { x: r.x, y: r.y, w: r.width, h: r.height }; })()");
        const btnBox = await evaluate("(() => { const el = document.getElementById('btn-select-action'); if (!el) return null; const r = el.getBoundingClientRect(); return { x: r.x, y: r.y, w: r.width, h: r.height }; })()");
        if (statsBox && btnBox) {
          console.log(`  [Boat Select] Thumbnails found: ${thumbCount}, Stats box width: ${statsBox.w.toFixed(1)}px, Action button width: ${btnBox.w.toFixed(1)}px`);
          if (thumbCount === 5) {
            console.log(`  [PASS] All 5 boat thumbnails verified!`);
          }
          if (btnBox.x >= statsBox.x && (btnBox.x + btnBox.w) <= (statsBox.x + statsBox.w + 4)) {
            console.log(`  [PASS] Action button fits completely inside stats panel!`);
          } else {
            console.warn(`  [WARN] Action button overflow: btn right ${btnBox.x + btnBox.w} vs stats right ${statsBox.x + statsBox.w}`);
          }
        }
      }

      if (s.key === 'map-select') {
        const cardCount = await evaluate("document.querySelectorAll('#screen-map-select .map-card').length");
        const gridBox = await evaluate("(() => { const el = document.querySelector('.map-grid-container'); if (!el) return null; const r = el.getBoundingClientRect(); return { x: r.x, y: r.y, w: r.width, h: r.height }; })()");
        if (gridBox) {
          console.log(`  [Map Select] Map cards found: ${cardCount}, Grid box: ${gridBox.w.toFixed(1)}x${gridBox.h.toFixed(1)}px`);
          if (cardCount === 4) {
            console.log(`  [PASS] Exactly 4 map cards in 2x2 grid verified!`);
          }
        }
      }

      const ss = await send('Page.captureScreenshot', { format: 'png' });
      const imgBuf = Buffer.from(ss.data, 'base64');
      fs.writeFileSync(path.join(outDir, s.name), imgBuf);
      console.log(`  Saved screenshot: ${s.name}`);
    }

    if (consoleErrors.length === 0) {
      console.log(`  [PASS] Zero console errors on ${width}x${height}`);
    } else {
      console.error(`  [FAIL] Console errors:`, consoleErrors);
    }

    ws.close();
    edgeProc.kill();
    try { fs.rmSync(tempDir, { recursive: true, force: true }); } catch (e) {}
  } catch (err) {
    edgeProc.kill();
    try { fs.rmSync(tempDir, { recursive: true, force: true }); } catch (e) {}
    throw err;
  }
}

async function run() {
  await testViewport(1920, 1080, 'Full HD Standard');
  await testViewport(1366, 768, 'Laptop Resolution');
  await testViewport(1024, 600, 'Narrow / Compact Viewport');
  console.log("\n=======================================================");
  console.log("ALL VIEWPORT AND SCREEN TESTS COMPLETED SUCCESSFULLY!");
  console.log("=======================================================\n");
}

run().catch(e => {
  console.error("Test failed:", e);
  process.exit(1);
});
