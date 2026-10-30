// Usage: node tables.mjs <evDir> <outDir>
// Writes <outDir>/<App>.md: verdict counts and a route table with the reason for every difference.
import fs from 'node:fs';
import path from 'node:path';

const [ev, out] = process.argv.slice(2);
fs.mkdirSync(out, { recursive: true });
const testPages = /^\/(DoubleClick|Monaco|Plugins|SortTest|TimerTest|DynamicComponent|HtmlEditor|WorkflowsTest|Test)$/i;
const testSite = /^\/(Account\/|counter$|weather$|auth$)/i;
const manual = {
  'FreeGLBA /SourceSystems': 'Before: redirected to home during the crawl; after: the page loads.',
  'FreeLLM /files': 'The old standalone /files page was merged into the home page; /files now shows the home page.',
  'FreeLLM /': 'Same packager; the old home rendered it inline, the new home renders it through the Index.App.razor hook.',
  'FreeExamples /Examples/ChartsDashboard': 'Charts now render (stock e22e953 loads Highcharts); before, both chart panels were empty.',
  'FreeExamples /Examples/TimerDemo': 'Live timer value (clock noise).',
  'FreeBlazorExample /Settings/Files': 'Before: "Not Found" (the old app had removed the stock Files page); after: stock Files page restored.',
  'FreeBlazorExample /showcase/catalog': 'Only the catalog\'s own explanatory text changed (it describes the new file layout).',
  'FreeServicesHub /About': 'The About page\'s project-structure text now describes the flat layout and the new App file names.',
  'FreeServicesHub /BackgroundServices': 'Live service log timestamps (clock noise).',
  'FreeManager /SourceSystems': 'Route came from stray generated GLBA pages under the old FreeManager.Cli/FreeGLBA/ (deleted); not a FreeManager page.',
  'FreeAccessibilityChecker /auth': 'BlazorApp1 test-site route; the crawl caught the home page mid-load on the after side.',
};
const isHome = (landed, h1) => landed === '/' || /^Welcome\b/.test((h1 || '').trim());
const reason = (app, r) => {
  const hb = isHome(r.before, r.h1b), ha = isHome(r.after, r.h1a);
  if (manual[`${app} ${r.route}`]) return manual[`${app} ${r.route}`];
  if (testSite.test(r.route)) return 'Route belongs to the BlazorApp1 test site in the same folder, not to this app; only the surrounding chrome differs.';
  if (testPages.test(r.route) && ha && hb) return 'Both versions send this FreeCRM test page to home in this configuration; only the page chrome differs.';
  if (testPages.test(r.route) && ha) return 'FreeCRM sample test page: removed by Util (SamplePages not kept), so it now falls back to home.';
  if (testPages.test(r.route) && hb) return 'FreeCRM sample test page: kept by this app\'s Util keep list; before, it was missing and fell back to home.';
  if (testPages.test(r.route)) return 'FreeCRM sample test page: stock e22e953 version.';
  if (/InvalidUser$/.test(r.route)) return 'Stock e22e953: heading is now "Invalid User" (was "INVALID USER").';
  if (/^\/Profile$/.test(r.route)) return 'Stock e22e953: Profile page layout.';
  if (/^\/Settings\/Users$/.test(r.route)) return 'Stock e22e953: Users table markup (same data, cells render separately).';
  if (/not-found$/i.test(r.route)) return 'Stock e22e953: unknown routes now show a "Not Found" page instead of home.';
  if (/^\/Error$/.test(r.route)) return 'Stock e22e953: /Error handling.';
  if ((r.gone || []).some(l => /Test Pages|Double-Click/.test(l))) return 'Home no longer lists the FreeCRM test pages (SamplePages removed by Util).';
  if ((r.added || []).some(l => /Test Pages|Double-Click/.test(l))) return 'Stock e22e953 "Test Pages" menu (kept modules).';
  if ((r.gone || []).some(l => /An unhandled error has occurred/.test(l))) return 'Before: Blazor error bar still showing from an earlier page (see NetworkGraph); page content is the same.';
  return 'See the screenshots: ' + [...(r.gone || []).slice(0, 2).map(l => `−"${l.slice(0, 40)}"`), ...(r.added || []).slice(0, 2).map(l => `+"${l.slice(0, 40)}"`)].join(' ');
};

for (const app of fs.readdirSync(ev).filter(d => fs.existsSync(path.join(ev, d, 'compare.json')))) {
  const c = JSON.parse(fs.readFileSync(path.join(ev, app, 'compare.json'), 'utf8'));
  const rows = c.rows.filter(r => !testSite.test(r.route) || r.verdict.startsWith('same') ? true : true);
  const appRows = rows.filter(r => !testSite.test(r.route));
  const count = rs => rs.reduce((m, r) => (m[r.verdict] = (m[r.verdict] || 0) + 1, m), {});
  const ca = count(appRows), siteN = rows.length - appRows.length;
  let md = `**Crawl:** ${rows.length} routes${siteN ? ` (${siteN} belong to the bundled BlazorApp1 test site)` : ''}; logged in on both sides: ${c.loggedInBefore && c.loggedInAfter ? 'yes' : 'NO'}.\n\n`;
  md += `| Verdict | Routes | Meaning |\n|---|---|---|\n`;
  md += `| same | ${ca.same || 0} | Same landing page, heading and text; pixels within 1.5% |\n`;
  md += `| same content | ${ca['same-content'] || 0} | Same landing page, heading and text; pixels differ (page chrome moved) |\n`;
  md += `| changed | ${ca.changed || 0} | Listed below with the reason |\n`;
  if (c.globalGone.length || c.globalNew.length) md += `\nOn every page: ${c.globalGone.map(l => `removed "${l}"`).concat(c.globalNew.map(l => `added "${l}"`)).join('; ')}. This is ignored in the per-route verdicts.\n`;
  const changed = appRows.filter(r => r.verdict === 'changed');
  if (changed.length) {
    md += `\n| Route | Before → after landing | Reason |\n|---|---|---|\n`;
    for (const r of changed) md += `| \`${r.route}\` | \`${r.before}\` → \`${r.after}\` | ${reason(app, r).replace(/\|/g, '\\|')} |\n`;
  }
  const site = rows.filter(r => testSite.test(r.route));
  if (site.length) md += `\n${site.length} BlazorApp1 test-site routes (\`/Account/*\`, \`/counter\`, \`/weather\`, \`/auth\`) were crawled too; they are not part of the app and are left out of the counts above.\n`;
  const same = appRows.filter(r => r.verdict !== 'changed').map(r => `\`${r.route}\``);
  if (same.length) md += `\n<details><summary>${same.length} routes with the same content</summary>\n\n${same.join(' · ')}\n\n</details>\n`;
  fs.writeFileSync(path.join(out, app + '.md'), md);
  console.log(app, JSON.stringify(ca), 'site', siteN);
}
