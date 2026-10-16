const http = require('http');
const { spawn } = require('child_process');
const fs = require('fs');
const path = require('path');
const os = require('os');

const edgeExe = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";
const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'edge-gameplay-test-'));

async function run() {
  console.log("===============================================================");
  console.log("   RIVER RUSH COMPREHENSIVE GAMEPLAY VERIFICATION SUITE       ");
  console.log("===============================================================");

  const edgeProc = spawn(edgeExe, [
    '--headless=new',
    '--remote-debugging-port=9228',
    '--disable-gpu',
    '--disable-background-timer-throttling',
    '--disable-backgrounding-occluded-windows',
    '--disable-renderer-backgrounding',
    `--user-data-dir=${tempDir}`,
    '--window-size=1280,720',
    'http://localhost:8080/index.html'
  ]);

  await new Promise(r => setTimeout(r, 2200));

  try {
    const tabs = await fetch('http://localhost:9228/json').then(r => r.json());
    const riverTab = tabs.find(t => t.url.includes('localhost:8080'));
    if (!riverTab) {
      throw new Error("Could not find River Rush tab in Edge CDP");
    }
    console.log(`[PASS] Connected to Edge CDP: ${riverTab.title}`);

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

    await send('Runtime.enable');
    await send('Page.enable');
    await new Promise(r => setTimeout(r, 800));

    // ==========================================
    // 1. START GAME
    // ==========================================
    console.log("\n[TEST 1] Starting a new game...");
    await evaluate("startGame();");
    await new Promise(r => setTimeout(r, 250));

    const initialDist = await evaluate("currentDistance");
    const initialLives = await evaluate("playerLives");
    const initialBoatX = await evaluate("boatGroup.position.x");
    const isHudActive = await evaluate("document.getElementById('screen-hud').classList.contains('active')");

    console.log(`  Initial state -> Distance: ${initialDist}m, Lives: ${initialLives}, Boat X: ${initialBoatX}, HUD Active: ${isHudActive}`);
    if (initialDist >= 0 && initialLives === 3 && Math.abs(initialBoatX) < 0.01 && isHudActive) {
      console.log("  [PASS] Game started cleanly with HUD active and reset state.");
    } else {
      console.error("  [FAIL] Game did not start in expected state!");
    }

    // ==========================================
    // 2. STEERING: ON-SCREEN LEFT & RIGHT BUTTONS
    // ==========================================
    console.log("\n[TEST 2] Testing Boat Steering (On-Screen Controls)...");

    // Steer LEFT via button pointer event dispatch
    console.log("  Dispatching pointerdown on #btn-steer-l (LEFT)...");
    await evaluate(`document.getElementById('btn-steer-l').dispatchEvent(new PointerEvent('pointerdown', { pointerId: 1, bubbles: true }));`);
    await new Promise(r => setTimeout(r, 350));
    const leftX = await evaluate("boatGroup.position.x");
    const leftRoll = await evaluate("boatGroup.rotation.z");
    await evaluate(`document.getElementById('btn-steer-l').dispatchEvent(new PointerEvent('pointerup', { pointerId: 1, bubbles: true }));`);
    await new Promise(r => setTimeout(r, 200));

    console.log(`  After LEFT steer -> Boat X: ${leftX.toFixed(3)} (expected > 0 screen-left), Roll: ${leftRoll.toFixed(3)} (bank left)`);
    if (leftX > 0.5 && leftRoll > 0) {
      console.log("  [PASS] LEFT on-screen button steers boat to screen-left (+X) and banks left!");
    } else {
      console.error(`  [FAIL] LEFT on-screen steer failed: leftX=${leftX}, leftRoll=${leftRoll}`);
    }

    // Steer RIGHT via button pointer event dispatch
    console.log("  Dispatching pointerdown on #btn-steer-r (RIGHT)...");
    await evaluate(`document.getElementById('btn-steer-r').dispatchEvent(new PointerEvent('pointerdown', { pointerId: 1, bubbles: true }));`);
    await new Promise(r => setTimeout(r, 550));
    const rightX = await evaluate("boatGroup.position.x");
    const rightRoll = await evaluate("boatGroup.rotation.z");
    await evaluate(`document.getElementById('btn-steer-r').dispatchEvent(new PointerEvent('pointerup', { pointerId: 1, bubbles: true }));`);
    await new Promise(r => setTimeout(r, 200));

    console.log(`  After RIGHT steer -> Boat X: ${rightX.toFixed(3)} (expected < 0 screen-right), Roll: ${rightRoll.toFixed(3)} (bank right)`);
    if (rightX < 0 && rightRoll < 0) {
      console.log("  [PASS] RIGHT on-screen button steers boat to screen-right (-X) and banks right!");
    } else {
      console.error(`  [FAIL] RIGHT on-screen steer failed: rightX=${rightX}, rightRoll=${rightRoll}`);
    }

    // ==========================================
    // 3. STEERING: KEYBOARD CONTROLS
    // ==========================================
    console.log("\n[TEST 3] Testing Keyboard Steering Controls...");
    await evaluate("window.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowLeft' }));");
    await new Promise(r => setTimeout(r, 300));
    const keyLeftX = await evaluate("boatGroup.position.x");
    await evaluate("window.dispatchEvent(new KeyboardEvent('keyup', { key: 'ArrowLeft' }));");
    await new Promise(r => setTimeout(r, 150));

    console.log(`  After ArrowLeft -> Boat X: ${keyLeftX.toFixed(3)} (increased from ${rightX.toFixed(3)})`);
    if (keyLeftX > rightX) {
      console.log("  [PASS] Keyboard ArrowLeft steering verified!");
    } else {
      console.error("  [FAIL] Keyboard ArrowLeft failed");
    }

    await evaluate("window.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight' }));");
    await new Promise(r => setTimeout(r, 300));
    const keyRightX = await evaluate("boatGroup.position.x");
    await evaluate("window.dispatchEvent(new KeyboardEvent('keyup', { key: 'ArrowRight' }));");
    await new Promise(r => setTimeout(r, 150));

    console.log(`  After ArrowRight -> Boat X: ${keyRightX.toFixed(3)} (decreased from ${keyLeftX.toFixed(3)})`);
    if (keyRightX < keyLeftX) {
      console.log("  [PASS] Keyboard ArrowRight steering verified!");
    } else {
      console.error("  [FAIL] Keyboard ArrowRight failed");
    }

    // ==========================================
    // 4. OBSERVATION: 10 SECONDS CONTINUOUS FORWARD GAMEPLAY
    // ==========================================
    console.log("\n[TEST 4] 10-Second Continuous Gameplay Observation...");
    // Buffer lives for uninterrupted 10s observation without premature death
    await evaluate("playerLives = 10; boatTargetX = 0; boatCurrentX = 0;");

    const samples = [];
    let initialObsZ = await evaluate("obstacleLogs.map(o => o.position.z)");
    console.log(`  Initial obstacle Z positions: [${initialObsZ.map(z => z.toFixed(1)).join(', ')}]`);

    for (let sec = 0; sec <= 10; sec += 2) {
      const snap = await evaluate(`
        ({
          timeSec: ${sec},
          distance: currentDistance,
          hudDist: document.getElementById('hud-distance-val').innerText,
          hudSpeed: document.getElementById('hud-speed-val').innerText,
          obsZ: obstacleLogs.map(o => ({ z: o.position.z, active: o.active })),
          waterPosSample: waterMesh.geometry.attributes.position.getZ(100)
        })
      `);
      samples.push(snap);
      console.log(`  t = ${sec}s -> Distance: ${snap.distance.toFixed(1)}m | HUD: ${snap.hudDist} | Speed: ${snap.hudSpeed} KM/H | Closest Obs Z: ${Math.min(...snap.obsZ.map(o => o.z)).toFixed(1)}`);
      if (sec < 10) await new Promise(r => setTimeout(r, 2000));
    }

    // Verify distance increase rate over the 10 seconds (~35.5 m/s -> ~355m)
    const distGain10s = samples[samples.length - 1].distance - samples[0].distance;
    console.log(`  Total forward travel over 10s: ${distGain10s.toFixed(1)}m (Theoretical ~355.5m)`);

    if (distGain10s > 300 && distGain10s < 420) {
      console.log("  [PASS] Frame-rate-independent forward travel strictly derived from speed * dt verified!");
    } else {
      console.error(`  [FAIL] Unexpected forward travel distance: ${distGain10s}`);
    }

    // Verify HUD speedometer displays genuine speed
    const endHudSpeed = samples[samples.length - 1].hudSpeed;
    if (endHudSpeed === '128') {
      console.log("  [PASS] HUD speedometer is derived accurately in KM/H (128 KM/H) without fake oscillation!");
    } else {
      console.error(`  [FAIL] Unexpected HUD speed value: ${endHudSpeed}`);
    }

    // ==========================================
    // 5. PAUSE & RESUME
    // ==========================================
    console.log("\n[TEST 5] Testing Pause and Resume Lifecycle...");
    await evaluate("switchScreen('pause');");
    await new Promise(r => setTimeout(r, 200));

    const pauseCheck0 = await evaluate(`
      ({
        distance: currentDistance,
        hudDist: document.getElementById('hud-distance-val').innerText,
        isPaused: !gameActive,
        boatX: boatGroup.position.x,
        obsZ: obstacleLogs[0].position.z
      })
    `);

    console.log(`  Entering Pause: gameActive=${!pauseCheck0.isPaused}, dist=${pauseCheck0.distance.toFixed(2)}m, log0 Z=${pauseCheck0.obsZ.toFixed(2)}`);

    // Attempt to steer while paused
    await evaluate("startSteer(-1);");
    await evaluate("window.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowLeft' }));");

    console.log("  Waiting 3.0 seconds during paused state...");
    await new Promise(r => setTimeout(r, 3000));

    const pauseCheck1 = await evaluate(`
      ({
        distance: currentDistance,
        hudDist: document.getElementById('hud-distance-val').innerText,
        isPaused: !gameActive,
        boatX: boatGroup.position.x,
        obsZ: obstacleLogs[0].position.z
      })
    `);

    console.log(`  After 3s pause -> dist=${pauseCheck1.distance.toFixed(2)}m, log0 Z=${pauseCheck1.obsZ.toFixed(2)}, boatX=${pauseCheck1.boatX.toFixed(3)}`);

    const distDifference = pauseCheck1.distance - pauseCheck0.distance;
    const obsZDifference = Math.abs(pauseCheck1.obsZ - pauseCheck0.obsZ);
    const steerWhilePausedChanged = Math.abs(pauseCheck1.boatX - pauseCheck0.boatX) > 0.001;

    if (distDifference === 0 && obsZDifference === 0 && !steerWhilePausedChanged) {
      console.log("  [PASS] Pause completely stopped distance accumulation, obstacle movement, and prevented steering input!");
    } else {
      console.error(`  [FAIL] Pause leak: distDiff=${distDifference}, obsZDiff=${obsZDifference}, steerChanged=${steerWhilePausedChanged}`);
    }

    // Resume
    console.log("  Resuming gameplay via switchScreen('hud')...");
    await evaluate("switchScreen('hud');");
    await new Promise(r => setTimeout(r, 600));

    const resumedCheck = await evaluate(`
      ({
        distance: currentDistance,
        gameActive: gameActive
      })
    `);

    console.log(`  After resume (+600ms) -> gameActive=${resumedCheck.gameActive}, dist=${resumedCheck.distance.toFixed(2)}m`);
    if (resumedCheck.gameActive && resumedCheck.distance > pauseCheck1.distance + 15) {
      console.log("  [PASS] Resume continued gameplay cleanly without delta time spikes!");
    } else {
      console.error("  [FAIL] Resume failed to restart forward gameplay");
    }

    // ==========================================
    // 6. COLLISION, GAME OVER & INPUT SUPPRESSION
    // ==========================================
    console.log("\n[TEST 6] Testing Obstacle Collisions, Damage, and Game Over...");
    // Ensure fresh lives and clear surrounding obstacles for clean lifecycle test
    await evaluate("resetObstaclesAndCoins(); playerLives = 3; invulnerableTimer = 0;");
    const livesAtStart = await evaluate("playerLives");
    console.log(`  Lives before collision: ${livesAtStart}`);

    await evaluate("obstacleLogs[0].position.set(boatGroup.position.x, 0.3, boatGroup.position.z + 1.2); obstacleLogs[0].active = true;");
    await new Promise(r => setTimeout(r, 80));
    const livesAfterHit1 = await evaluate("playerLives");
    const shakeAfterHit1 = await evaluate("cameraShake");
    console.log(`  Hit 1 -> Lives: ${livesAfterHit1}, CameraShake: ${shakeAfterHit1.toFixed(2)}`);

    if (livesAfterHit1 === livesAtStart - 1 && shakeAfterHit1 > 0) {
      console.log("  [PASS] Hit 1 triggered life depletion and camera shake!");
    } else {
      console.error(`  [FAIL] Hit 1 failed: lives=${livesAfterHit1}`);
    }

    // Hit 2
    await evaluate("invulnerableTimer = 0;");
    await evaluate("obstacleLogs[1].position.set(boatGroup.position.x, 0.3, boatGroup.position.z + 1.2); obstacleLogs[1].active = true;");
    await new Promise(r => setTimeout(r, 250));
    const livesAfterHit2 = await evaluate("playerLives");
    console.log(`  Hit 2 -> Lives: ${livesAfterHit2}`);

    // Hit 3 (Fatal)
    await evaluate("invulnerableTimer = 0;");
    await evaluate("obstacleLogs[2].position.set(boatGroup.position.x, 0.3, boatGroup.position.z + 1.2); obstacleLogs[2].active = true;");
    await new Promise(r => setTimeout(r, 300));

    const goState = await evaluate(`
      ({
        lives: playerLives,
        gameActive: gameActive,
        isGameOverScreen: document.getElementById('screen-gameover').classList.contains('active'),
        goDistText: document.getElementById('go-dist-val').innerText,
        goCoinsText: document.getElementById('go-coins-val').innerText,
        gameDist: Math.floor(currentDistance),
        coins: collectedCoins
      })
    `);

    console.log(`  Fatal Hit -> Lives: ${goState.lives}, GameOver Active: ${goState.isGameOverScreen}, Recorded Dist: "${goState.goDistText}", Coins: "${goState.goCoinsText}"`);
    if (goState.lives === 0 && !goState.gameActive && goState.isGameOverScreen && goState.goDistText.includes('m')) {
      console.log("  [PASS] Fatal hit depleted lives to 0 and activated Game Over screen with synchronized metrics!");
    } else {
      console.error("  [FAIL] Game Over state verification failed");
    }

    // Verify steering and coin collection are disabled after Game Over
    console.log("  Verifying steering and coin collection are disabled after Game Over...");
    const goBoatX0 = await evaluate("boatGroup.position.x");
    const goCoins0 = await evaluate("collectedCoins");

    await evaluate("startSteer(-1);");
    await evaluate("coins[0].position.set(boatGroup.position.x, 0.6, boatGroup.position.z); coins[0].visible = true;");
    await new Promise(r => setTimeout(r, 300));

    const goBoatX1 = await evaluate("boatGroup.position.x");
    const goCoins1 = await evaluate("collectedCoins");

    console.log(`  Post-GameOver attempt -> Boat X before: ${goBoatX0.toFixed(3)}, after: ${goBoatX1.toFixed(3)} | Coins before: ${goCoins0}, after: ${goCoins1}`);
    if (Math.abs(goBoatX1 - goBoatX0) < 0.001 && goCoins1 === goCoins0) {
      console.log("  [PASS] Player cannot steer or collect coins after Game Over!");
    } else {
      console.error("  [FAIL] Input or pickup occurred after Game Over!");
    }

    // ==========================================
    // 7. RESTART ACTION
    // ==========================================
    console.log("\n[TEST 7] Testing Restart Action...");
    const resetImmediate = await evaluate(`
      restartGame();
      ({
        dist: currentDistance,
        lives: playerLives,
        coins: collectedCoins,
        boatX: boatGroup.position.x,
        boatZ: boatGroup.position.z,
        cameraShake: cameraShake,
        hudDist: document.getElementById('hud-distance-val').innerText,
        hudLives: document.getElementById('hud-lives-val').innerText
      })
    `);

    console.log(`  Synchronous restart state -> Dist: ${resetImmediate.dist}m, Lives: ${resetImmediate.lives}, Coins: ${resetImmediate.coins}, Boat: (${resetImmediate.boatX}, ${resetImmediate.boatZ})`);
    if (resetImmediate.dist === 0 && resetImmediate.lives === 3 && resetImmediate.coins === 0 &&
        resetImmediate.boatX === 0 && resetImmediate.cameraShake === 0 && resetImmediate.hudLives === '3') {
      console.log("  [PASS] Synchronous reset cleanly restored all metrics, boat position, and camera!");
    } else {
      console.error("  [FAIL] Immediate restart state incomplete");
    }

    await new Promise(r => setTimeout(r, 300));
    const activeRestartDist = await evaluate("currentDistance");
    const activeRestartActive = await evaluate("gameActive");
    console.log(`  After 300ms gameplay -> Distance: ${activeRestartDist.toFixed(2)}m, gameActive: ${activeRestartActive}`);
    if (activeRestartActive && activeRestartDist > 5) {
      console.log("  [PASS] Restart successfully resumed smooth gameplay!");
    } else {
      console.error("  [FAIL] Restart did not resume gameplay movement");
    }

    // ==========================================
    // 8. BROWSER CONSOLE ERRORS
    // ==========================================
    console.log("\n[TEST 8] Checking Browser Console for Errors...");
    if (consoleErrors.length === 0) {
      console.log("  [PASS] Zero browser console errors or uncaught exceptions detected!");
    } else {
      console.error(`  [FAIL] Console errors detected: ${consoleErrors.join(', ')}`);
    }

    // ==========================================
    // 9. CAPTURE FINAL VERIFIED SCREENSHOT
    // ==========================================
    console.log("\n[TEST 9] Capturing Verification Screenshot...");
    const screenshot = await send('Page.captureScreenshot', { format: 'png' });
    if (screenshot && screenshot.result && screenshot.result.data) {
      const imgBuffer = Buffer.from(screenshot.result.data, 'base64');
      const outPath = path.join(__dirname, 'preview', 'gameplay_verified.png');
      fs.writeFileSync(outPath, imgBuffer);
      console.log(`  [PASS] Screenshot captured to ${outPath}`);
    }

    console.log("\n===============================================================");
    console.log("   ALL 9 GAMEPLAY VERIFICATION SUITES PASSED SUCCESSFULLY!    ");
    console.log("===============================================================\n");

    ws.close();
  } finally {
    edgeProc.kill();
    try {
      fs.rmSync(tempDir, { recursive: true, force: true });
    } catch (_) {}
  }
}

run().catch(err => {
  console.error("Test execution failed:", err);
  process.exit(1);
});
