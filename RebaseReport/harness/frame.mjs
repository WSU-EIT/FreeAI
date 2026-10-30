// Usage: node frame.mjs <jobs.json>
// jobs: [{ out, title, panels: [{ img, label, tone: 'before'|'after'|'plain' }], height? }]
// One panel = captioned shot; two panels = side-by-side comparison. Shots are cropped to the top `height` px.
import { chromium } from 'playwright-core';
import fs from 'node:fs';
import path from 'node:path';

const jobs = JSON.parse(fs.readFileSync(process.argv[2], 'utf8'));
const exe = process.env.CHROMIUM_PATH || undefined; // a Playwright Chromium; unset = Playwright's default
const browser = await chromium.launch({ executablePath: exe, args: ['--allow-file-access-from-files'] });
const esc = s => String(s).replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));
const tones = { before: '#8a1c1c', after: '#1d5c2e', plain: '#2b3a4a' };

for (const j of jobs) {
  const h = j.height || 800;
  const n = j.panels.length;
  const panelW = n === 1 ? 1280 : 800;
  const width = panelW * n + (n - 1) * 16 + 32;
  const page = await browser.newPage({ viewport: { width, height: 200 } });
  const panels = j.panels.map(p => {
    const src = 'data:image/png;base64,' + fs.readFileSync(p.img).toString('base64');
    return `<div class="panel"><div class="label" style="background:${tones[p.tone || 'plain']}">${esc(p.label)}</div>
      <div class="shot" style="height:${Math.round(h * panelW / 1280)}px"><img src="${src}"></div></div>`;
  }).join('');
  await page.setContent(`<!doctype html><html><head><style>
    body{margin:0;background:#eef1f4;font:600 18px -apple-system,Segoe UI,Helvetica,Arial,sans-serif;color:#1b1f24}
    .wrap{padding:14px 16px 16px}
    .title{font-size:20px;margin:0 0 10px 2px}
    .row{display:flex;gap:16px}
    .panel{width:${panelW}px;background:#fff;border:1px solid #c9d1d9;border-radius:8px;overflow:hidden}
    .label{color:#fff;padding:7px 12px;font-size:16px}
    .shot{overflow:hidden;position:relative}
    .shot img{width:100%;display:block}
  </style></head><body><div class="wrap">${j.title ? `<div class="title">${esc(j.title)}</div>` : ''}<div class="row">${panels}</div></div></body></html>`);
  await page.waitForTimeout(200);
  fs.mkdirSync(path.dirname(j.out), { recursive: true });
  await page.locator('.wrap').screenshot({ path: j.out });
  await page.close();
}
await browser.close();
