// Usage: node compare.mjs <beforeDir> <afterDir> <outDir> [nameMapFrom nameMapTo]
// Compares route by route: status, landing path, heading, masked text, and pixels (1.5% threshold).
import fs from 'node:fs';
import path from 'node:path';
import { PNG } from 'pngjs';
import pixelmatch from 'pixelmatch';

const [bDir, aDir, outDir, mapFrom, mapTo] = process.argv.slice(2);
fs.mkdirSync(path.join(outDir, 'diffs'), { recursive: true });
const load = d => JSON.parse(fs.readFileSync(path.join(d, 'results.json'), 'utf8'));
const B = load(bDir), A = load(aDir);
const norm = s => (mapFrom ? s.split(mapFrom).join(mapTo) : s);
const byRoute = rs => new Map(rs.map(r => [r.route, r]));
const bm = byRoute(B.routes), am = byRoute(A.routes);
const all = [...new Set([...bm.keys(), ...am.keys()])].sort();

function pixelDiff(fb, fa, out) {
  try {
    const b = PNG.sync.read(fs.readFileSync(fb)), a = PNG.sync.read(fs.readFileSync(fa));
    const w = Math.min(b.width, a.width), h = Math.min(b.height, a.height);
    const crop = (img) => { const o = new PNG({ width: w, height: h }); PNG.bitblt(img, o, 0, 0, w, h, 0, 0); return o; };
    const cb = crop(b), ca = crop(a), d = new PNG({ width: w, height: h });
    const n = pixelmatch(cb.data, ca.data, d.data, w, h, { threshold: 0.1 });
    const extra = Math.abs(b.width * b.height - a.width * a.height);
    const ratio = (n + extra) / Math.max(b.width * b.height, a.width * a.height);
    if (ratio > 0.015) fs.writeFileSync(out, PNG.sync.write(d));
    return ratio;
  } catch { return null; }
}

// Lines that appear/disappear on most pages are page chrome (header, nav, skip link): report once, ignore per page.
const lines = s => new Set(s.replace(/[   ]/g, ' ').split('\n').map(x => x.replace(/\s+/g, ' ').trim()).filter(Boolean));
const both = all.filter(r => bm.get(r) && am.get(r));
const tally = (m, k) => m.set(k, (m.get(k) || 0) + 1);
const goneCount = new Map(), newCount = new Map();
for (const r of both) {
  const lb = lines(norm(bm.get(r).text)), la = lines(am.get(r).text);
  for (const l of lb) if (!la.has(l)) tally(goneCount, l);
  for (const l of la) if (!lb.has(l)) tally(newCount, l);
}
const globalCut = Math.max(3, Math.ceil(both.length * 0.6));
const globalGone = [...goneCount].filter(([, n]) => n >= globalCut).map(([l]) => l);
const globalNew = [...newCount].filter(([, n]) => n >= globalCut).map(([l]) => l);
const gG = new Set(globalGone), gN = new Set(globalNew);

const rows = [];
for (const r of all) {
  const b = bm.get(r), a = am.get(r);
  if (!b || !a) { rows.push({ route: r, verdict: b ? 'removed' : 'added', before: b?.landed, after: a?.landed, h1b: b?.h1, h1a: a?.h1 }); continue; }
  const lb = lines(norm(b.text)), la = lines(a.text);
  const gone = [...lb].filter(l => !la.has(l) && !gG.has(l)), added = [...la].filter(l => !lb.has(l) && !gN.has(l));
  const sameText = gone.length === 0 && added.length === 0;
  const sameLanded = norm(b.landed).toLowerCase() === a.landed.toLowerCase();
  const ws = s => (s || "").replace(/\s+/g, " ").trim();
  const sameH1 = ws(norm(b.h1)) === ws(a.h1);
  const slug = path.basename(a.shot, '.png');
  const px = pixelDiff(path.join(bDir, b.shot), path.join(aDir, a.shot), path.join(outDir, 'diffs', slug + '.png'));
  // same = same content once global chrome is set aside; pixels are reported, not judged, since chrome shifts every page
  const verdict = sameText && sameLanded && sameH1 ? (px !== null && px <= 0.015 ? 'same' : 'same-content') : 'changed';
  rows.push({ route: r, verdict, statusB: b.status, statusA: a.status, before: b.landed, after: a.landed, h1b: b.h1, h1a: a.h1, sameText, gone: gone.slice(0, 8), added: added.slice(0, 8), px: px === null ? null : +(px * 100).toFixed(2), errB: b.errors.length, errA: a.errors.length, shot: a.shot, shotB: b.shot });
}
const summary = rows.reduce((m, r) => (m[r.verdict] = (m[r.verdict] || 0) + 1, m), {});
fs.writeFileSync(path.join(outDir, 'compare.json'), JSON.stringify({ loggedInBefore: B.loggedIn, loggedInAfter: A.loggedIn, summary, globalGone, globalNew, rows }, null, 1));
console.log('logged in before/after:', B.loggedIn, A.loggedIn, summary);
console.log('global gone:', JSON.stringify(globalGone).slice(0, 300));
console.log('global new :', JSON.stringify(globalNew).slice(0, 300));
for (const r of rows) if (!r.verdict.startsWith('same')) console.log(r.verdict.padEnd(8), r.route, `${r.before}->${r.after}`, r.h1b !== r.h1a ? `h1:"${(r.h1b||'').slice(0,25)}"->"${(r.h1a||'').slice(0,25)}"` : '', r.gone?.length ? '-' + JSON.stringify(r.gone.slice(0,2)).slice(0,90) : '', r.added?.length ? '+' + JSON.stringify(r.added.slice(0,2)).slice(0,90) : '');
