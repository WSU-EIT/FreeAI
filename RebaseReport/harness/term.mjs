// Usage: node term.mjs <out.png> <title> <textFile>  : renders console output as a terminal-style image
import { chromium } from 'playwright-core';
import fs from 'node:fs';
const [out, title, file] = process.argv.slice(2);
const text = fs.readFileSync(file, 'utf8').replace(/\/[^ ]*?\/console\/(old|new)\//g, '…/');
const esc = s => s.replace(/[&<>]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;' }[c]));
const exe = process.env.CHROMIUM_PATH || undefined; // a Playwright Chromium; unset = Playwright's default
const b = await chromium.launch({ executablePath: exe });
const p = await b.newPage({ viewport: { width: 1100, height: 200 } });
await p.setContent(`<!doctype html><body style="margin:0;background:#eef1f4;padding:14px 16px"><div id="t" style="width:1060px;border-radius:8px;overflow:hidden;border:1px solid #30363d">
<div style="background:#2d333b;color:#cdd9e5;font:600 15px -apple-system,Helvetica,sans-serif;padding:8px 12px">${esc(title)}</div>
<pre style="margin:0;background:#1c2128;color:#d5dde5;font:14px/1.45 Menlo,Consolas,monospace;padding:12px 14px;white-space:pre-wrap">${esc(text)}</pre></div></body>`);
await p.locator('#t').screenshot({ path: out }); await b.close();
