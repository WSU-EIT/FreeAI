// Usage: node capture.mjs <baseUrl> <routes.json> <outDir>
// Logs in as admin/admin, then for each route records status, landing path, heading,
// masked visible text and console errors, and saves a full-page screenshot.
import { chromium } from 'playwright-core';
import fs from 'node:fs';
import path from 'node:path';

const [base, routesFile, outDir] = process.argv.slice(2);
const routes = JSON.parse(fs.readFileSync(routesFile, 'utf8'));
fs.mkdirSync(path.join(outDir, 'shots'), { recursive: true });
const exe = process.env.CHROMIUM_PATH || undefined; // a Playwright Chromium; unset = Playwright's default

export const mask = s => s
  .replace(/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi, '<guid>')
  .replace(/\b\d{1,4}[\/-]\d{1,2}[\/-]\d{1,4}\b/g, '<date>')
  .replace(/\b\d{1,2}:\d{2}(:\d{2})?(\s?[AP]M)?\b/gi, '<time>')
  .replace(/\b(Mon|Tue|Wed|Thu|Fri|Sat|Sun)[a-z]*,?\s+[A-Z][a-z]+\s+\d{1,2},?\s+\d{4}\b/g, '<date>')
  .replace(/\b[A-Z][a-z]+ \d{1,2}, \d{4}\b/g, '<date>')
  .replace(/\b\d+(\.\d+)?\s?(ms|seconds?|minutes?|hours?|days?) ago\b/g, '<ago>')
  .replace(/[ \t]+/g, ' ').replace(/\n\s*\n+/g, '\n').trim();

const slug = r => (r === '/' ? 'home' : r.replace(/^\//, '').replace(/[^A-Za-z0-9]+/g, '_'));

// Wait until the page text has stopped changing and no "Loading..." message is left (max ~25 s).
async function settle(page) {
  try { await page.waitForLoadState('networkidle', { timeout: 10000 }); } catch {}
  let prev = null;
  for (let i = 0; i < 25; i++) {
    await page.waitForTimeout(1000);
    const t = await page.evaluate(() => document.body?.innerText || '').catch(() => null);
    if (t === null) continue;
    if (t === prev && !/\bLoading\b[^\n]{0,30}(\.\.\.|…|please wait)/i.test(t)) break;
    prev = t;
  }
  await page.waitForTimeout(500);
}

// Navigate inside the running Blazor app (a link click the router intercepts) so WASM isn't reloaded per page.
async function nav(page, base, r) {
  const ok = await page.evaluate((href) => {
    if (!window.Blazor) return false;
    const a = document.createElement('a'); a.href = href; document.body.appendChild(a); a.click(); a.remove(); return true;
  }, base + r).catch(() => false);
  if (!ok) await page.goto(base + r, { waitUntil: 'domcontentloaded', timeout: 30000 });
}

const browser = await chromium.launch({ executablePath: exe });
const ctx = await browser.newContext({ viewport: { width: 1280, height: 800 }, colorScheme: 'light', reducedMotion: 'reduce' });
const page = await ctx.newPage();
let errors = [];
page.on('console', m => { if (m.type() === 'error') errors.push(m.text().slice(0, 300)); });
page.on('pageerror', e => errors.push(String(e).slice(0, 300)));

// Log in
await page.goto(base + '/Login', { waitUntil: 'domcontentloaded' });
await settle(page);
let loggedIn = false;
try {
  await page.fill('#login-email', 'admin', { timeout: 20000 });
  await page.fill('#login-password', 'admin');
  await page.getByRole('button', { name: /log in/i }).click();
  await page.waitForTimeout(3000);
  await settle(page);
  loggedIn = !/\/Login/i.test(page.url());
} catch (e) { console.error('login failed', String(e).slice(0, 200)); }

const results = { base, loggedIn, routes: [] };
for (const r of routes) {
  errors = [];
  let status = null;
  try {
    // HTTP status from the server for the route itself; the screen comes from in-app navigation
    status = (await page.request.get(base + r, { maxRedirects: 0 })).status();
    await nav(page, base, r);
  } catch (e) { status = 'ERR ' + String(e).slice(0, 80); }
  await settle(page);
  const info = await page.evaluate(() => ({
    h1: (document.querySelector('h1')?.innerText || '').trim(),
    title: document.title,
    text: document.body?.innerText || '',
  })).catch(() => ({ h1: '', title: '', text: '' }));
  const file = `shots/${slug(r)}.png`;
  await page.screenshot({ path: path.join(outDir, file), fullPage: true }).catch(() => {});
  const landed = new URL(page.url()).pathname;
  results.routes.push({ route: r, status, landed, h1: info.h1, title: info.title, text: mask(info.text), errors: [...new Set(errors)].slice(0, 5), shot: file });
  console.log(`${String(status).padEnd(4)} ${r} -> ${landed} | ${info.h1.slice(0, 50)}`);
}
fs.writeFileSync(path.join(outDir, 'results.json'), JSON.stringify(results, null, 1));
await browser.close();
