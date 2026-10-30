#!/bin/bash
# Usage: run-app.sh <App> <oldArchivePath> <oldServerCsproj> [nameFrom nameTo]
# Builds, runs and captures the app at 096393aa (before) and HEAD (after), then compares.
# Paths are repo-relative. One app at a time; trees are deleted afterwards (disk is tight).
set -u
App="$1"; OldTop="$2"; OldProj="$3"; MapFrom="${4:-}"; MapTo="${5:-}"
REPO=$(git -C "$(dirname "$0")" rev-parse --show-toplevel)
S=${REBASE_WORK:-$REPO/../rebase-evidence-work}
H=$(cd "$(dirname "$0")" && pwd); EV=$S/ev/$App
rm -rf "$EV"; mkdir -p "$EV"

run_version() {
  local ver=$1 rev=$2 top=$3 proj=$4 port=$5
  local T=$S/trees/$ver; rm -rf "$T"; mkdir -p "$T"
  (cd $REPO && git archive "$rev" -- "$top" ':(exclude,glob)**/showcase/runs/**' ':(exclude)FreeA11yChecker/A11yAudit' | tar -x -C "$T")
  node $H/routes.mjs "$T/$top" > "$EV/routes-$ver.json"
  echo "== $App $ver: build $proj"
  if ! dotnet build "$T/$proj" -c Debug -nologo -v q -p:EnableWindowsTargeting=true > "$EV/build-$ver.log" 2>&1; then
    echo "BUILD FAILED ($ver)"; grep -m5 ' error ' "$EV/build-$ver.log"; return 1
  fi
  grep -E '^\s+[0-9]+ (Warning|Error)' "$EV/build-$ver.log" | tr -s ' ' > "$EV/build-$ver.summary"
  local dir; dir=$(dirname "$T/$proj"); local name; name=$(basename "$proj" .csproj)
  ( cd "$dir" && ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://127.0.0.1:$port \
      DatabaseType=InMemory ConnectionStrings__AppData=InMemory BackgroundService__Enabled=false \
      LocalModeUrl=http://127.0.0.1:$port/ Kestrel__Endpoints__Http__Url=http://127.0.0.1:$port \
      exec dotnet "bin/Debug/net10.0/$name.dll" > "$EV/server-$ver.log" 2>&1 ) &
  local pid=$!
  for i in $(seq 1 90); do curl -s -o /dev/null http://127.0.0.1:$port/ && break; sleep 1; done
  sleep 3
  echo "== $App $ver: capture (pid $pid)"
  node $H/capture.mjs "http://127.0.0.1:$port" "$EV/routes-union.json" "$EV/$ver" > "$EV/capture-$ver.log" 2>&1
  tail -3 "$EV/capture-$ver.log"
  kill $pid 2>/dev/null; sleep 2; kill -9 $pid 2>/dev/null
  rm -rf "$T"
}

# Route list = union of both versions' @page routes (so a route missing on one side shows up)
(cd $REPO && mkdir -p $S/trees/rt && rm -rf $S/trees/rt/* && \
  git archive 096393aa -- "$OldTop" ':(exclude,glob)**/*.png' ':(exclude,glob)**/*.jpg' ':(exclude)FreeA11yChecker/A11yAudit' | tar -x -C $S/trees/rt && \
  git archive HEAD -- "$App" ':(exclude,glob)**/*.png' | tar -x -C $S/trees/rt)
node -e '
const fs=require("fs"),[a,b]=[process.argv[1],process.argv[2]];
const r=new Set([...JSON.parse(fs.readFileSync(a)),...JSON.parse(fs.readFileSync(b))]);
const from=process.argv[3], to=process.argv[4];
const out=[...r].map(x=>from?x.split(from).join(to):x);
fs.writeFileSync(process.argv[5], JSON.stringify([...new Set(out)].sort(),null,1));
' <(node $H/routes.mjs "$S/trees/rt/$OldTop") <(node $H/routes.mjs "$S/trees/rt/$App") "$MapFrom" "$MapTo" "$EV/routes-union.json"
rm -rf $S/trees/rt/*
echo "== $App: $(node -e 'console.log(JSON.parse(require("fs").readFileSync(process.argv[1])).length)' $EV/routes-union.json) routes"

run_version before 096393aa "$OldTop" "$OldProj" 5401
run_version after HEAD "$App" "$App/$App/$App.csproj" 5402
node $H/compare.mjs "$EV/before" "$EV/after" "$EV" $MapFrom $MapTo | tee "$EV/compare.txt" | head -60
