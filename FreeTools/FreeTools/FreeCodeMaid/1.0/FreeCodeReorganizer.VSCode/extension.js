// FreeCodeReorganizer — VS Code front-end (plain JS, zero npm dependencies).
//
// It does no C# parsing itself: it shells out to FreeCodeReorganizer.Cli (a small .NET app in
// ./engine) which wraps the SAME FreeCodeReorganizer.Core Roslyn engine the Visual Studio extension
// uses. So VS, the CLI, and VS Code all produce identical reorganizations.
//
// Two commands:
//   * Reorganize Document  — the active .cs editor (stdin -> stdout, replaces the buffer).
//   * Reorganize Workspace — every .cs file under each workspace folder (engine --dir mode).

const vscode = require('vscode');
const cp = require('child_process');
const path = require('path');

function getConfig() {
    return vscode.workspace.getConfiguration('freecodereorganizer');
}

/** Build the engine's config flags from settings. */
function buildConfigArgs(cfg) {
    return [
        '--sort-alphabetically', String(cfg.get('sortAlphabetically', true)),
        '--ignore-underscore', String(cfg.get('ignoreLeadingUnderscoreInSort', true)),
        '--group-by-visibility', String(cfg.get('groupByVisibility', false)),
        '--static-first', String(cfg.get('staticMembersFirst', false)),
        '--collapse-brace', String(cfg.get('collapseWrappedParameterBrace', true)),
        '--max-percent', String(cfg.get('maxPercentReordered', 35)),
    ];
}

/** Spawn the engine. If stdinText is provided, it's piped in and stdout is resolved as a string. */
function runEngine(context, cfg, engineArgs, stdinText) {
    return new Promise((resolve, reject) => {
        const dll = path.join(context.extensionPath, 'engine', 'FreeCodeReorganizer.Cli.dll');
        const dotnet = cfg.get('dotnetPath', 'dotnet');
        const child = cp.spawn(dotnet, [dll, ...engineArgs], {});
        let out = Buffer.alloc(0);
        let err = '';
        child.stdout.on('data', (d) => { out = Buffer.concat([out, d]); });
        child.stderr.on('data', (d) => { err += d.toString(); });
        child.on('error', (e) =>
            reject(new Error("Could not start '" + dotnet + "': " + e.message +
                '. Install the .NET SDK or set freecodereorganizer.dotnetPath.')));
        child.on('close', (code) =>
            code === 0 ? resolve(out.toString('utf8'))
                       : reject(new Error(err.trim() || ('engine exited with code ' + code))));
        if (stdinText !== undefined) {
            child.stdin.write(stdinText, 'utf8');
            child.stdin.end();
        }
    });
}

/** Run an arbitrary command (used for 'dotnet format'), resolving its combined output. */
function runProcess(cmd, cmdArgs, cwd) {
    return new Promise((resolve, reject) => {
        const child = cp.spawn(cmd, cmdArgs, { cwd });
        let out = '';
        child.stdout.on('data', (d) => { out += d.toString(); });
        child.stderr.on('data', (d) => { out += d.toString(); });
        child.on('error', reject);
        child.on('close', (code) => code === 0 ? resolve(out) : reject(new Error(out.trim() || ('exit ' + code))));
    });
}

async function reorganizeDocument(context) {
    const editor = vscode.window.activeTextEditor;
    if (!editor) {
        vscode.window.showWarningMessage('FreeCodeReorganizer: no active editor.');
        return;
    }
    if (editor.document.languageId !== 'csharp') {
        vscode.window.showWarningMessage('FreeCodeReorganizer only works on C# (.cs) files.');
        return;
    }

    const cfg = getConfig();

    // Optional: run VS Code's own editorconfig-aware formatter first (Ctrl+K Ctrl+D equivalent).
    if (cfg.get('runFormatFirst', false)) {
        try { await vscode.commands.executeCommand('editor.action.formatDocument'); } catch (e) { /* best effort */ }
    }

    const input = editor.document.getText();
    let output;
    try {
        output = await runEngine(context, cfg, buildConfigArgs(cfg), input);
    } catch (e) {
        vscode.window.showErrorMessage('FreeCodeReorganizer: ' + e.message);
        return;
    }

    if (output === input) {
        vscode.window.setStatusBarMessage('FreeCodeReorganizer: already organized — no changes.', 3000);
        return;
    }

    const doc = editor.document;
    const fullRange = new vscode.Range(doc.positionAt(0), doc.positionAt(input.length));
    await editor.edit((eb) => eb.replace(fullRange, output));
    vscode.window.setStatusBarMessage('FreeCodeReorganizer: document reorganized.', 3000);
}

async function reorganizeWorkspace(context) {
    const folders = vscode.workspace.workspaceFolders;
    if (!folders || folders.length === 0) {
        vscode.window.showWarningMessage('FreeCodeReorganizer: open a folder/workspace first.');
        return;
    }

    const choice = await vscode.window.showWarningMessage(
        'Reorganize EVERY .cs file in the workspace? This rewrites files on disk (generated files and bin/obj are skipped). Make sure your work is committed first.',
        { modal: true },
        'Reorganize All');
    if (choice !== 'Reorganize All') {
        return;
    }

    const cfg = getConfig();
    await vscode.workspace.saveAll(false);

    const channel = vscode.window.createOutputChannel('FreeCodeReorganizer');
    channel.show(true);

    await vscode.window.withProgress(
        { location: vscode.ProgressLocation.Notification, title: 'FreeCodeReorganizer: reorganizing workspace…', cancellable: false },
        async () => {
            for (const folder of folders) {
                const root = folder.uri.fsPath;

                if (cfg.get('runDotnetFormatOnWorkspace', false)) {
                    channel.appendLine('Running dotnet format in ' + root + ' …');
                    try {
                        channel.appendLine(await runProcess(cfg.get('dotnetPath', 'dotnet'), ['format'], root));
                    } catch (e) {
                        channel.appendLine('dotnet format skipped/failed: ' + e.message);
                    }
                }

                channel.appendLine('Reorganizing ' + root + ' …');
                try {
                    const out = await runEngine(context, cfg, [...buildConfigArgs(cfg), '--dir', root], undefined);
                    channel.appendLine(out.trimEnd());
                } catch (e) {
                    channel.appendLine('Error: ' + e.message);
                }
            }
        });

    vscode.window.showInformationMessage('FreeCodeReorganizer: workspace reorganize complete (see the FreeCodeReorganizer output panel).');
}

function activate(context) {
    context.subscriptions.push(
        vscode.commands.registerCommand('freecodereorganizer.reorganizeDocument', () => reorganizeDocument(context)),
        vscode.commands.registerCommand('freecodereorganizer.reorganizeWorkspace', () => reorganizeWorkspace(context)));
}

function deactivate() { }

module.exports = { activate, deactivate };
