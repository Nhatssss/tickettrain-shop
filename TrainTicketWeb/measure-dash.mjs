import fs from 'node:fs';

const info = JSON.parse(fs.readFileSync(process.env.TEMP + '\\cdp-info.json', 'utf8'));
const ws = new WebSocket(info.url);
let id = 0;
const pend = new Map();

ws.addEventListener('message', (ev) => {
  const m = JSON.parse(ev.data);
  if (m.id && pend.has(m.id)) {
    const q = pend.get(m.id);
    pend.delete(m.id);
    m.error ? q.reject(new Error(JSON.stringify(m.error))) : q.resolve(m.result);
  }
});

const send = (method, params = {}, sessionId) =>
  new Promise((resolve, reject) => {
    const mid = ++id;
    pend.set(mid, { resolve, reject });
    const payload = { id: mid, method, params };
    if (sessionId) payload.sessionId = sessionId;
    ws.send(JSON.stringify(payload));
  });

await new Promise((r) => ws.addEventListener('open', r, { once: true }));

const { targetId } = await send('Target.createTarget', { url: 'about:blank' });
const { sessionId } = await send('Target.attachToTarget', { targetId, flatten: true });
const s = (m, p) => send(m, p, sessionId);

await s('Page.enable');
await s('Network.enable');
await s('Network.setCookie', {
  name: 'TrainTicket.Auth', value: info.cookie, domain: 'localhost', path: '/', httpOnly: true,
});

const MEASURE = `(() => {
  const w = s => { const e = document.querySelector(s); if (!e) return null; const r = e.getBoundingClientRect(); return Math.round(r.width); };
  const cs = s => { const e = document.querySelector(s); return e ? getComputedStyle(e).maxWidth : null; };
  const gridCols = s => { const e = document.querySelector(s); return e ? getComputedStyle(e).gridTemplateColumns : null; };
  return JSON.stringify({
    wrap: w('.admin-wrap'), wrapMax: cs('.admin-wrap'),
    side: w('.admin-side'), main: w('.admin-main'), dash: w('.dash'),
    kpiGrid: w('.kpi-grid'), kpiCols: gridCols('.kpi-grid'),
    miniCols: gridCols('.mini-grid'),
    splitCols: gridCols('.dash-split'),
    chart: w('.chart'),
  });
})()`;

for (const width of [1920, 1440, 1280, 1024]) {
  await s('Emulation.setDeviceMetricsOverride', { width, height: 1000, deviceScaleFactor: 1, mobile: false });
  await s('Page.navigate', { url: 'http://localhost:5025/quan-tri' });
  await new Promise((r) => setTimeout(r, 1500));
  const r = await s('Runtime.evaluate', { expression: MEASURE, returnByValue: true });
  console.log(width, r.result.value);
}
ws.close();
process.exit(0);
