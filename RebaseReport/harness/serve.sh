#!/bin/bash
# Usage: serve.sh <rev> <archivePath> <serverCsproj> <port> <scenario> <outDir> [arg]
# Exports one version, builds, runs it, plays one interact.mjs scenario, then stops and cleans up.
set -u
rev=$1; top=$2; proj=$3; port=$4; scen=$5; out=$6; arg=${7:-}
REPO=$(git -C "$(dirname "$0")" rev-parse --show-toplevel)
S=${REBASE_WORK:-$REPO/../rebase-evidence-work}
T=$S/trees/i-$port; rm -rf "$T"; mkdir -p "$T"
(cd $REPO && git archive "$rev" -- "$top" ':(exclude,glob)**/showcase/runs/**' ':(exclude)FreeA11yChecker/A11yAudit' | tar -x -C "$T")
dotnet build "$T/$proj" -c Debug -nologo -v q -p:EnableWindowsTargeting=true > "$T.build.log" 2>&1 || { echo "BUILD FAILED"; tail -5 "$T.build.log"; exit 1; }
dir=$(dirname "$T/$proj"); name=$(basename "$proj" .csproj)
( cd "$dir" && ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://127.0.0.1:$port DatabaseType=InMemory \
    ConnectionStrings__AppData=InMemory BackgroundService__Enabled=false LocalModeUrl=http://127.0.0.1:$port/ \
    Kestrel__Endpoints__Http__Url=http://127.0.0.1:$port exec dotnet "bin/Debug/net10.0/$name.dll" > "$T.server.log" 2>&1 ) &
pid=$!
for i in $(seq 1 90); do curl -s -o /dev/null http://127.0.0.1:$port/ && break; sleep 1; done; sleep 3
[ -n "$arg" ] && arg=${arg//@T@/$T}
node "$(dirname "$0")/interact.mjs" "$scen" "http://127.0.0.1:$port" "$out" "$arg"
kill $pid; sleep 2; kill -9 $pid 2>/dev/null; rm -rf "$T" "$T.build.log" "$T.server.log"
ls "$out"
