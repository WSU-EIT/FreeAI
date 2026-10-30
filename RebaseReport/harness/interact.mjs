// Usage: node interact.mjs <scenario> <baseUrl> <outDir> [arg]
// Scripted interactions the route crawl can't show. Each saves numbered viewport frames to outDir.
import { chromium } from 'playwright-core';
import fs from 'node:fs';
import path from 'node:path';

const [scenario, base, outDir, arg] = process.argv.slice(2);
fs.mkdirSync(outDir, { recursive: true });
const exe = process.env.CHROMIUM_PATH || undefined; // a Playwright Chromium; unset = Playwright's default
const browser = await chromium.launch({ executablePath: exe });
const page = await (await browser.newContext({ viewport: { width: 1280, height: 800 }, colorScheme: 'light' })).newPage();
let n = 0;
const shot = async (name) => { await page.waitForTimeout(800); await page.screenshot({ path: path.join(outDir, `${String(++n).padStart(2, '0')}-${name}.png`) }); };
const settle = async (ms = 1500) => { try { await page.waitForLoadState('networkidle', { timeout: 10000 }); } catch {} await page.waitForTimeout(ms); };

async function login() {
  await page.goto(base + '/Login'); await settle(3000);
  await page.fill('#login-email', 'admin'); await page.fill('#login-password', 'admin');
  await page.getByRole('button', { name: /log in/i }).click(); await settle(4000);
}

const scenarios = {
  // Tab once on a fresh page: the old layout reveals "Skip to main content"; the new stock layout has none.
  async skiplink() {
    await login();
    await page.goto(base + '/GlbaDashboard'); await settle(5000);
    // Put the sequential-focus starting point at the top of <body>, then Tab once
    await page.evaluate(() => { const t = document.createElement('span'); t.tabIndex = -1; document.body.prepend(t); t.focus(); t.remove(); });
    await page.keyboard.press('Tab'); await page.waitForTimeout(600);
    const focused = await page.evaluate(() => { const e = document.activeElement; return e ? `${e.tagName} "${(e.innerText || e.getAttribute('aria-label') || '').trim().slice(0, 40)}" href=${e.getAttribute('href') || ''}` : ''; });
    fs.writeFileSync(path.join(outDir, 'focused.txt'), focused + '\n');
    await page.screenshot({ path: path.join(outDir, 'tab1.png'), clip: { x: 0, y: 0, width: 1280, height: 260 } });
  },
  async llm() {
    await login(); await shot('packager-empty');
    await page.fill('#directoryPathInput', arg); await settle(4000); await shot('packager-files');
    const sel = page.getByRole('button', { name: 'Select All Visible', exact: true });
    await sel.scrollIntoViewIfNeeded(); await page.waitForTimeout(500); await shot('packager-file-list');
    await sel.click(); await settle(2500);
    await page.evaluate(() => window.scrollTo(0, 0)); await shot('packager-selected-output');
  },
  async blazorexample() {
    await login(); await shot('home');
    await page.getByRole('link', { name: /view component showcase/i }).click(); await settle(3000); await shot('showcase-home');
    await page.goto(base + '/showcase/kanbanboard/v1'); await settle(3000); await shot('kanban');
    await page.goto(base + '/showcase/feature107-hierarchical-tree'); await settle(3000); await shot('tree');
  },
  async examples() {
    await login(); await shot('home');
    const menus = ['Dashboard & Data', 'UI Components', 'Charts & Viz', 'Code & Real-Time'];
    for (const m of menus) {
      await page.goto(base + '/'); await settle(2500);
      await page.locator('nav a.dropdown-toggle, nav button.dropdown-toggle', { hasText: m }).first().click().catch(() => {});
      await page.waitForTimeout(700); await shot('menu-' + m.replace(/\W+/g, '-').toLowerCase());
    }
    await page.goto(base + '/Examples/CodeEditor'); await settle(4000); await shot('code-editor');
  },
};
await scenarios[scenario]();
await browser.close();
