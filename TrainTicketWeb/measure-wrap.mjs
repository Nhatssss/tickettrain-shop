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
await s('Emulation.setDeviceMetricsOverride', { width: 1440, height: 1000, deviceScaleFactor: 1, mobile: false });
await s('Page.navigate', { url: 'http://localhost:5025/quan-tri' });
await new Promise((r) => setTimeout(r, 1600));

const EXPR = `(() => {
  const el = document.querySelector('.admin-wrap');
  const cs = getComputedStyle(el);
  const parent = el.parentElement;
  const pcs = getComputedStyle(parent);
  return JSON.stringify({
    adminWrap: {
      w: Math.round(el.getBoundingClientRect().width),
      display: cs.display, maxWidth: cs.maxWidth, width: cs.width, flex: cs.flex,
      margin: cs.margin, padding: cs.padding, alignSelf: cs.alignSelf,
    },
    parent: {
      tag: parent.tagName, cls: parent.className,
      w: Math.round(parent.getBoundingClientRect().width),
      display: pcs.display, alignItems: pcs.alignItems, flexDirection: pcs.flexDirection,
    },
    bodyDisplay: getComputedStyle(document.body).display,
    bodyW: Math.round(document.body.getBoundingClientRect().width),
    innerW: window.innerWidth,
  });
})()`;

const r = await s('Runtime.evaluate', { expression: EXPR, returnByValue: true });
console.log(r.result.value);
ws.close();
process.exit(0);
