#!/bin/bash
H=$(dirname "$0")
while read -r App Old Proj From To; do
  [ -z "$App" ] && continue
  echo "######## $App $(date +%T)"
  bash $H/run-app.sh "$App" "$Old" "$Proj" $From $To 2>&1 | grep -E '^(==|BUILD|logged|global)' 
done <<'LIST'
FreeGLBA FreeGLBA FreeGLBA/FreeGLBA/FreeGLBA.csproj
FreeAccessibilityChecker FreeA11yChecker FreeA11yChecker/FreeA11yChecker/FreeA11yChecker.csproj FreeA11yChecker FreeAccessibilityChecker
FreeServicesHub FreeServicesHub FreeServicesHub/FreeServicesHub/FreeServicesHub/FreeServicesHub.csproj
FreeManager FreeManager FreeManager/FreeManager/FreeManager.csproj
FreeExamples FreeTools/FreeExamples FreeTools/FreeExamples/FreeExamples/FreeExamples.csproj
FreeBlazorExample FreeBlazorExtended FreeBlazorExtended/FreeBlazorExample/FreeBlazorExample/FreeBlazorExample.csproj
FreePlugins FreePlugins FreePlugins/FreePluginsV1/FreePlugins/FreePlugins.csproj
FreeLLM FreeLLM FreeLLM/FreeLLM/FreeLLM.csproj
FreeSmartsheets FreeSmartsheets FreeSmartsheets/FreeSmartsheets/FreeSmartsheets/FreeSmartsheets.csproj
ChatWithAI ChatWithAI ChatWithAI/FreeAI/FreeAI.csproj FreeAI ChatWithAI
LIST
echo "######## done $(date +%T)"
