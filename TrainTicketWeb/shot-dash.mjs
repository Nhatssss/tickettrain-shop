// Chup dashboard qua CDP (dung ws cua browser, khong phai page).
import fs from 'node:fs';

const info = JSON.parse(fs.readFileSync(process.env.TEMP + '\\cdp-info.json', 'utf8'));
const browserWs = info.url;
const cookieVal = info.cookie;

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

function rpc(ws) {
  let id = 0;
  const pending = new Map();
  ws.addEventListener('message', (ev) => {
    let msg;
    try { msg = JSON.parse(ev.data); } catch { return; }
    if (msg.id && pending.has(msg.id)) {
      const p = pending.get(msg.id);
      pending.delete(msg.id);
      msg.error ? p.reject(new Error(JSON.stringify(msg.error))) : p.resolve(msg.result);
    }
  });
  return (method, params = {}, sessionId) => new Promise((resolve, reject) => {
    const mid = ++id;
    pending.set(mid, { resolve, reject });
    const payload = { id: mid, method, params };
    if (sessionId) payload.sessionId = sessionId;
    ws.send(JSON.stringify(payload));
  });
}

const ws = new WebSocket(browserWs);
await new Promise((res, rej) => {
  ws.addEventListener('open', res, { once: true });
  ws.addEventListener('error', rej, { once: true });
});
const send = rpc(ws);

// tao target moi (tab that) roi attach
const { targetId } = await send('Target.createTarget', { url: 'about:blank' });
const { sessionId } = await send('Target.attachToTarget', { targetId, flatten: true });
const s = (m, p) => send(m, p, sessionId);

await s('Page.enable');
await s('Network.enable');
await s('Runtime.enable');
await s('Network.setCookie', {
  name: 'TrainTicket.Auth', value: cookieVal, domain: 'localhost', path: '/', httpOnly: true,
});

const widths = [
  { w: 1440, h: 1100, name: 'desktop' },
  { w: 1024, h: 1100, name: 'laptop' },
  { w: 430, h: 1400, name: 'mobile' },
];

const PROBE = `(() => {
  const doc = document.documentElement;
  const q = s => document.querySelector(s);
  const box = s => { const e = q(s); if (!e) return null; const r = e.getBoundingClientRect(); return { w: Math.round(r.width), h: Math.round(r.height), x: Math.round(r.x) }; };
  const cols = s => { const e = q(s); if (!e) return 0; return getComputedStyle(e).gridTemplateColumns.split(' ').filter(Boolean).length; };
  const bars = [...document.querySelectorAll('.col-bar')].map(b => Math.round(b.getBoundingClientRect().height));
  const kpis = [...document.querySelectorAll('.kpi')].map(k => { const r = k.getBoundingClientRect(); return { w: Math.round(r.width), y: Math.round(r.y) }; });
  const split = [...document.querySelectorAll('.dash-split > .card')].map(e => { const r = e.getBoundingClientRect(); return { w: Math.round(r.width), y: Math.round(r.y) }; });
  return JSON.stringify({
    title: document.title,
    scrollW: doc.scrollWidth, clientW: doc.clientWidth,
    overflowX: doc.scrollWidth > doc.clientWidth + 1,
    kpiColumns: cols('.kpi-grid'),
    miniColumns: cols('.mini-grid'),
    splitColumns: cols('.dash-split'),
    kpis, split,
    barCount: bars.length,
    maxBarH: bars.length ? Math.max(...bars) : 0,
    minBarH: bars.length ? Math.min(...bars) : 0,
    valOverflow: [...document.querySelectorAll('.kpi-value')].some(e => e.scrollWidth > e.clientWidth + 1),
    chartW: box('.chart') ? box('.chart').w : 0,
    chartH: box('.chart') ? box('.chart').h : 0,
    occRows: document.querySelectorAll('.occ-row').length,
    progCount: document.querySelectorAll('.prog-fill').length,
    kpiGridDisplay: q('.kpi-grid') ? getComputedStyle(q('.kpi-grid')).display : 'missing',
    bodyH: Math.round(document.body.getBoundingClientRect().height),
  });
})()`;

const out = [];
for (const vp of widths) {
  await s('Emulation.setDeviceMetricsOverride', {
    width: vp.w, height: vp.h, deviceScaleFactor: 1, mobile: vp.w < 500,
  });
  await s('Page.navigate', { url: 'http://localhost:5025/quan-tri' });
  await sleep(1800);
  const r = await s('Runtime.evaluate', { expression: PROBE, returnByValue: true });
  const data = JSON.parse(r.result.value);
  const shot = await s('Page.captureScreenshot', { format: 'png', captureBeyondViewport: true });
  const file = `${process.env.TEMP}\\dash-${vp.name}.png`;
  fs.writeFileSync(file, Buffer.from(shot.data, 'base64'));
  out.push({ vp: vp.name, width: vp.w, file, ...data });
}

console.log('DEVICE_RESULTS_START');
out.forEach((o) => console.log(JSON.stringify(o)));
console.log('DEVICE_RESULTS_END');
ws.close();
