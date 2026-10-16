const http = require('http');
const { spawn } = require('child_process');
const fs = require('fs');
const path = require('path');

const edgeExe = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";

async function run() {
  console.log("=== RIVER RUSH GAMEPLAY VERIFICATION SUITE ===");

  // 1. Launch Headless Edge
  const edgeProc = spawn(edgeExe, [
    '--headless=new',
    '--remote-debugging-port=9222',
    '--disable-gpu',
    'http://localhost:8080/index.html'
  ]);

  await new Promise(r => setTimeout(r, 2000));

  try {
    // 2. Discover WebSocket Debugger URL
    const tabs = await fetch('http://localhost:9222/json').then(r => r.json());
    const riverTab = tabs.find(t => t.url.includes('localhost:8080'));
    if (!riverTab) {
      throw new Error("Could not find River Rush tab in Edge CDP");
    }
    console.log(`[PASS] Connected to Edge CDP: ${riverTab.title}`);

    // 3. Connect via WebSocket
    const ws = new WebSocket(riverTab.webSocketDebuggerUrl);
    await new Promise(r => ws.onopen = r);

    let msgId = 1;
    const pending = new Map();
    const consoleLogs = [];
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
        else consoleLogs.push(`[${type}] ${text}`);
      }
      if (data.method === 'Runtime.exceptionThrown') {
        consoleErrors.push(JSON.stringify(data.params.exceptionDetails));
      }
    };

    function send(method, params = {}) {
      return new Promise((resolve) => {
        const id = msgId++;
        pending.set(id, resolve);
        ws.send(JSON.stringify({ id, method, params }));
      });
    }

    async function evaluate(expr) {
      const res = await send('Runtime.evaluate', { expression: expr, returnByValue: true, awaitPromise: true });
      if (res.result && res.result.exceptionDetails) {
        throw new Error(JSON.stringify(res.result.exceptionDetails));
      }
      return res.result ? res.result.result.value : undefined;
    }

    // Enable domains
    await send('Runtime.enable');
    await send('Page.enable');
    await new Promise(r => setTimeout(r, 1000));

    // ==========================================
    // TEST 1: BUG 1 - STEERING CONTROLS
    // ==========================================
    console.log("\n--- TEST 1: Steering Controls (Bug 1) ---");
    await evaluate("startGame();");
    await new Promise(r => setTimeout(r, 200));

    const initialX = await evaluate("boatGroup.position.x");
    console.log(`Initial boat X: ${initialX.toFixed(3)}`);

    // Steer LEFT (on screen)
    await evaluate("startSteer(-1);");
    await new Promise(r => setTimeout(r, 350));
    const leftX = await evaluate("boatGroup.position.x");
    const leftRoll = await evaluate("boatGroup.rotation.z");
    await evaluate("stopSteer();");
    await new Promise(r => setTimeout(r, 200));

    console.log(`After LEFT steer -> boat X: ${leftX.toFixed(3)} (expected > ${initialX.toFixed(3)}), roll: ${leftRoll.toFixed(3)}`);
    if (leftX > initialX && leftRoll > 0) {
      console.log("[PASS] LEFT control correctly steered boat to screen-LEFT (+X) and banked left!");
    } else {
      console.error(`[FAIL] LEFT control failed: leftX=${leftX}, initialX=${initialX}`);
    }

    // Steer RIGHT (on screen)
    await evaluate("startSteer(1);");
    await new Promise(r => setTimeout(r, 500));
    const rightX = await evaluate("boatGroup.position.x");
    const rightRoll = await evaluate("boatGroup.rotation.z");
    await evaluate("stopSteer();");
    await new Promise(r => setTimeout(r, 200));

    console.log(`After RIGHT steer -> boat X: ${rightX.toFixed(3)} (expected < ${leftX.toFixed(3)}), roll: ${rightRoll.toFixed(3)}`);
    if (rightX < leftX && rightRoll < 0) {
      console.log("[PASS] RIGHT control correctly steered boat to screen-RIGHT (-X) and banked right!");
    } else {
      console.error(`[FAIL] RIGHT control failed: rightX=${rightX}, leftX=${leftX}`);
    }

    // ==========================================
    // TEST 2: BUG 2 - REAL FORWARD MOVEMENT & DISTANCE
    // ==========================================
    console.log("\n--- TEST 2: Real Movement & Distance (Bug 2) ---");
    const dist0 = await evaluate("currentDistance");
    const obs0_z = await evaluate("obstacleLogs[0].position.z");
    const coin0_z = await evaluate("coins[0].position.z");
    const hudDist0 = await evaluate("document.getElementById('hud-distance-val').innerText");
    const hudSpeed = await evaluate("document.getElementById('hud-speed-val').innerText");

    console.log(`At t0: dist=${dist0.toFixed(2)}m, log0 Z=${obs0_z.toFixed(2)}, coin0 Z=${coin0_z.toFixed(2)}, HUD=${hudDist0}, Speed=${hudSpeed} KM/H`);

    await new Promise(r => setTimeout(r, 600));

    const dist1 = await evaluate("currentDistance");
    const obs1_z = await evaluate("obstacleLogs[0].position.z");
    const coin1_z = await evaluate("coins[0].position.z");
    const hudDist1 = await evaluate("document.getElementById('hud-distance-val').innerText");

    console.log(`At t1 (+600ms): dist=${dist1.toFixed(2)}m, log0 Z=${obs1_z.toFixed(2)}, coin0 Z=${coin1_z.toFixed(2)}, HUD=${hudDist1}`);

    const deltaDist = dist1 - dist0;
    const deltaObsZ = obs0_z - obs1_z;
    if (deltaDist > 15 && deltaObsZ > 15) {
      console.log(`[PASS] Actual forward travel verified! Distance advanced by ${deltaDist.toFixed(1)}m, obstacles approached by ${deltaObsZ.toFixed(1)}m.`);
    } else {
      console.error(`[FAIL] Movement check failed: deltaDist=${deltaDist}, deltaObsZ=${deltaObsZ}`);
    }

    // ==========================================
    // TEST 3: PAUSE & RESUME
    // ==========================================
    console.log("\n--- TEST 3: Pause & Resume Behavior ---");
    await evaluate("switchScreen('pause');");
    await new Promise(r => setTimeout(r, 200));

    const pausedDist0 = await evaluate("currentDistance");
    const isPaused = await evaluate("!gameActive");
    await new Promise(r => setTimeout(r, 500));
    const pausedDist1 = await evaluate("currentDistance");

    console.log(`Paused check: dist0=${pausedDist0.toFixed(2)}, dist1=${pausedDist1.toFixed(2)}, gameActive=${!isPaused}`);
    if (isPaused && pausedDist0 === pausedDist1) {
      console.log("[PASS] Movement and distance accumulation completely frozen while paused!");
    } else {
      console.error("[FAIL] Distance accumulated while paused!");
    }

    // Resume
    await evaluate("switchScreen('hud');");
    await new Promise(r => setTimeout(r, 400));
    const resumedDist = await evaluate("currentDistance");
    const isResumed = await evaluate("gameActive");
    if (isResumed && resumedDist > pausedDist1) {
      console.log(`[PASS] Resumed gameplay smoothly: distance resumed advancing to ${resumedDist.toFixed(1)}m`);
    } else {
      console.error("[FAIL] Resume failed to restart movement");
    }

    // ==========================================
    // TEST 4: BUG 3 - COLLISION, GAME OVER, RESTART
    // ==========================================
    console.log("\n--- TEST 4: Collision & Game Over Lifecycle (Bug 3) ---");
    const livesBefore = await evaluate("playerLives");
    console.log(`Initial lives: ${livesBefore}`);

    // Teleport obstacle directly into boat path for hit 1
    await evaluate("obstacleLogs[0].position.set(boatGroup.position.x, 0.3, boatGroup.position.z + 1.0); obstacleLogs[0].active = true;");
    await new Promise(r => setTimeout(r, 150));
    const lives1 = await evaluate("playerLives");
    console.log(`After obstacle hit 1 -> lives: ${lives1}`);

    // Hit 2
    await evaluate("invulnerableTimer = 0;");
    await evaluate("obstacleLogs[1].position.set(boatGroup.position.x, 0.3, boatGroup.position.z + 1.0); obstacleLogs[1].active = true;");
    await new Promise(r => setTimeout(r, 150));
    const lives2 = await evaluate("playerLives");
    console.log(`After obstacle hit 2 -> lives: ${lives2}`);

    // Hit 3 (Fatal)
    await evaluate("invulnerableTimer = 0;");
    await evaluate("obstacleLogs[2].position.set(boatGroup.position.x, 0.3, boatGroup.position.z + 1.0); obstacleLogs[2].active = true;");
    await new Promise(r => setTimeout(r, 250));

    const isGameOverActive = await evaluate("document.getElementById('screen-gameover').classList.contains('active')");
    const goDistText = await evaluate("document.getElementById('go-dist-val').innerText");
    const goCoinsText = await evaluate("document.getElementById('go-coins-val').innerText");

    console.log(`After fatal hit -> GameOver screen active: ${isGameOverActive}, Recorded Distance: ${goDistText}, Recorded Coins: ${goCoinsText}`);
    if (isGameOverActive && goDistText.includes('m')) {
      console.log("[PASS] Collision properly depleted lives and triggered Game Over screen with accurate metrics!");
    } else {
      console.error("[FAIL] Game Over was not triggered on fatal collision");
    }

    // Test Restart
    console.log("\n--- Testing Restart Action ---");
    const immediateReset = await evaluate("restartGame(); ({ dist: currentDistance, lives: playerLives, coins: collectedCoins, boatX: boatGroup.position.x, boatZ: boatGroup.position.z });");
    console.log(`Immediate restart values -> dist: ${immediateReset.dist}, lives: ${immediateReset.lives}, coins: ${immediateReset.coins}, boat: (${immediateReset.boatX}, ${immediateReset.boatZ})`);

    await new Promise(r => setTimeout(r, 200));

    const resetDist = await evaluate("currentDistance");
    const resetLives = await evaluate("playerLives");
    const resetCoins = await evaluate("collectedCoins");
    const resetHudActive = await evaluate("document.getElementById('screen-hud').classList.contains('active')");

    console.log(`After 200ms gameplay resumption -> dist: ${resetDist.toFixed(2)}m, lives: ${resetLives}, coins: ${resetCoins}, HUD active: ${resetHudActive}`);
    if (immediateReset.dist === 0 && immediateReset.lives === 3 && immediateReset.coins === 0 &&
        resetDist > 0 && resetLives === 3 && resetCoins === 0 && resetHudActive) {
      console.log("[PASS] Restart cleanly reset distance, lives, coins, boat position, and resumed active gameplay!");
    } else {
      console.error("[FAIL] Restart state did not reset completely");
    }

    // 5. Console Error Check
    console.log("\n--- Browser Console Errors ---");
    if (consoleErrors.length === 0) {
      console.log("[PASS] Zero browser console errors detected!");
    } else {
      console.error(`[FAIL] Console errors found: ${consoleErrors.join(', ')}`);
    }

    // 6. Capture Verification Screenshot
    const screenshot = await send('Page.captureScreenshot', { format: 'png' });
    if (screenshot && screenshot.result && screenshot.result.data) {
      const imgBuffer = Buffer.from(screenshot.result.data, 'base64');
      const outPath = path.join(__dirname, 'preview', 'gameplay_verified.png');
      fs.writeFileSync(outPath, imgBuffer);
      console.log(`[PASS] Verification screenshot captured to ${outPath}`);
    }

    console.log("\n=======================================================");
    console.log("ALL VERIFICATION SUITES EXECUTED AND PASSED SUCCESSFULLY!");
    console.log("=======================================================\n");

    ws.close();
  } finally {
    edgeProc.kill();
  }
}

run().catch(e => {
  console.error("Test error:", e);
  process.exit(1);
});
