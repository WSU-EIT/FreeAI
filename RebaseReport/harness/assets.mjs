// Usage: node assets.mjs <evDir> <outDir>
// Builds the report's images from the captures: per-app feature frames, before/after composites, and a jobs file for GIFs.
import fs from 'node:fs';
import path from 'node:path';
import { execFileSync } from 'node:child_process';

const [ev, out] = process.argv.slice(2);
const H = path.dirname(new URL(import.meta.url).pathname);
const features = JSON.parse(fs.readFileSync(path.join(H, 'features.json'), 'utf8'));
const compares = {
  FreeGLBA: ['/GlbaDashboard', '/SourceSystems'],
  FreeAccessibilityChecker: ['/Compliance', '/About'],
  FreeServicesHub: ['/', '/AgentDashboard'],
  FreeManager: ['/AppBuilder/EntityWizard', '/Templates'],
  FreeExamples: ['/Examples/ChartsDashboard', '/'],
  FreeBlazorExample: ['/showcase/feature103-calendar', '/Settings/Files'],
  FreePlugins: ['/Plugins', '/WorkflowsTest'],
  FreeLLM: ['/', '/files'],
  FreeSmartsheets: ['/', '/About'],
  ChatWithAI: ['/', '/About'],
};
const common = [['FreeGLBA', '/Profile'], ['FreeGLBA', '/Settings/Users'], ['FreeGLBA', '/Authorization/InvalidUser'], ['FreeLLM', '/not-found']];
const slug = r => (r === '/' ? 'home' : r.replace(/^\//, '').replace(/[^A-Za-z0-9]+/g, '-').toLowerCase());
const load = (app, v) => new Map(JSON.parse(fs.readFileSync(path.join(ev, app, v, 'results.json'), 'utf8')).routes.map(x => [x.route, x]));

const jobs = [], gifs = [];
for (const [app, routes] of Object.entries(features)) {
  const A = load(app, 'after'), B = load(app, 'before');
  const frames = [];
  for (const r of routes) {
    const a = A.get(r); if (!a) continue;
    const o = path.join(out, app, `feature-${slug(r)}.png`);
    jobs.push({ out: o, height: 800, panels: [{ img: path.join(ev, app, 'after', a.shot), label: `${app} (after rebase) · ${r}${a.h1 ? ' · ' + a.h1.replace(/\s+/g, ' ').slice(0, 60) : ''}`, tone: 'after' }] });
    frames.push(o);
  }
  if (frames.length > 1) gifs.push({ out: path.join(out, app, `${app}-tour.gif`), frames });
  for (const r of compares[app] || []) {
    const a = A.get(r), b = B.get(r); if (!a || !b) continue;
    jobs.push({ out: path.join(out, app, `compare-${slug(r)}.png`), height: 1000, title: `${app} · ${r}`,
      panels: [{ img: path.join(ev, app, 'before', b.shot), label: `Before · main 096393aa · landed ${b.landed}`, tone: 'before' },
               { img: path.join(ev, app, 'after', a.shot), label: `After · HEAD · landed ${a.landed}`, tone: 'after' }] });
  }
}
for (const [app, r] of common) {
  const a = load(app, 'after').get(r), b = load(app, 'before').get(r);
  jobs.push({ out: path.join(out, '_common', `stock-${slug(r)}.png`), height: 900, title: `Stock FreeCRM change, seen in ${app} · ${r}`,
    panels: [{ img: path.join(ev, app, 'before', b.shot), label: 'Before (old FreeCRM base)', tone: 'before' },
             { img: path.join(ev, app, 'after', a.shot), label: 'After (FreeCRM e22e953)', tone: 'after' }] });
}
fs.writeFileSync(path.join(H, 'jobs.json'), JSON.stringify(jobs, null, 1));
execFileSync('node', [path.join(H, 'frame.mjs'), path.join(H, 'jobs.json')], { stdio: 'inherit' });
for (const g of gifs) execFileSync('bash', [path.join(H, 'gif.sh'), g.out, '2.5', '960', ...g.frames], { stdio: 'inherit' });
console.log(`${jobs.length} images, ${gifs.length} gifs`);
