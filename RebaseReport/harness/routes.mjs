// Usage: node routes.mjs <appTreeDir> > routes.json
// Every static @page route in the tree's .razor files (no {params}), minus login/logout flow pages.
import fs from 'node:fs';
import path from 'node:path';

const skip = new Set(['/logout', '/processlogin', '/login', '/serverupdated', '/databaseoffline']);
const out = new Set();
function walk(d) {
  for (const e of fs.readdirSync(d, { withFileTypes: true })) {
    if (['bin', 'obj', 'node_modules', '.git'].includes(e.name)) continue;
    const p = path.join(d, e.name);
    if (e.isDirectory()) walk(p);
    else if (e.name.endsWith('.razor')) {
      for (const m of fs.readFileSync(p, 'utf8').matchAll(/^\s*@page\s+"([^"]+)"/gm)) {
        const r = m[1];
        if (r.includes('{') || skip.has(r.toLowerCase())) continue;
        out.add(r);
      }
    }
  }
}
const tenantOnly = new Set();
function walkTenant(d) {
  for (const e of fs.readdirSync(d, { withFileTypes: true })) {
    if (['bin', 'obj', 'node_modules', '.git'].includes(e.name)) continue;
    const p = path.join(d, e.name);
    if (e.isDirectory()) walkTenant(p);
    else if (e.name.endsWith('.razor')) {
      for (const m of fs.readFileSync(p, 'utf8').matchAll(/^\s*@page\s+"\/\{TenantCode\}(\/[^"{]+)"/gm)) tenantOnly.add(m[1]);
    }
  }
}
walk(process.argv[2]);
walkTenant(process.argv[2]);
// Pages that only exist under a tenant prefix are crawled under the seeded tenant "tenant1"
for (const r of tenantOnly) if (![...out].some(x => x.toLowerCase() === r.toLowerCase()) && !skip.has(r.toLowerCase())) out.add('/tenant1' + r);
console.log(JSON.stringify([...out].sort((a, b) => a.localeCompare(b)), null, 1));
