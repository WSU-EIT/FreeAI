// FreeCodeReorganizer — VS Code front-end (plain JS, zero npm dependencies).
//
// It does no C# parsing itself: it shells out to FreeCodeReorganizer.Cli (a small .NET app in
// ./engine) which wraps the SAME FreeCodeReorganizer.Core engine the Visual Studio extension uses.
// So VS, the CLI, and VS Code all produce identical results.
//
// Commands:
//   * Reorganize Document / Workspace — member reorder + FreeCRM house style.
//   * Clean Up Document / Workspace   — .editorconfig cleanup via `dotnet format` (whitespace or full).
//   Reorganize can optionally run the cleanup first (runCleanupBeforeReorganize), so editorconfig
//   formatting is applied and the house style is layered on top.

const vscode = require('vscode');
const cp = require('child_process');
const path = require('path');

const SUPPORTED = ['.cs', '.razor', '.cshtml'];

function getConfig() {
    return vscode.workspace.getConfiguration('freecodereorganizer');
}

/** Build the engine's reorder-rule + exclusion flags from settings. */
function buildConfigArgs(cfg) {
    const args = [
        '--sort-alphabetically', String(cfg.get('sortAlphabetically', true)),
        '--ignore-underscore', String(cfg.get('ignoreLeadingUnderscoreInSort', true)),
        '--group-by-visibility', String(cfg.get('groupByVisibility', false)),
        '--static-first', String(cfg.get('staticMembersFirst', false)),
        '--collapse-brace', String(cfg.get('collapseWrappedParameterBrace', true)),
        '--razor-attrs', String(cfg.get('indentWrappedRazorAttributes', true)),
        '--respect-regions', String(cfg.get('respectRegions', true)),
        '--max-percent', String(cfg.get('maxPercentReordered', 35)),
        '--max-width', String(cfg.get('maxLineWidth', 120)),
    ];
    for (const p of cfg.get('excludeReorganize', [])) { args.push('--exclude-reorganize', String(p)); }
    for (const p of cfg.get('excludeCleanup', [])) { args.push('--exclude-cleanup', String(p)); }
    return args;
}

function cleanupLevel(cfg) {
    return cfg.get('fullCleanup', false) ? 'full' : 'whitespace';
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

function activeSupportedEditor() {
    const editor = vscode.window.activeTextEditor;
    if (!editor) {
        vscode.window.showWarningMessage('FreeCodeReorganizer: no active editor.');
        return null;
    }
    const ext = path.extname(editor.document.uri.fsPath).toLowerCase();
    if (!SUPPORTED.includes(ext)) {
        vscode.window.showWarningMessage('FreeCodeReorganizer works on .cs, .razor and .cshtml files.');
        return null;
    }
    return editor;
}

// Reorganize the active document. When cleanup-first is on, save + run the on-disk pipeline
// (cleanup then reorganize) and revert the buffer to the result; otherwise use the fast stdin path.
async function reorganizeDocument(context) {
    const editor = activeSupportedEditor();
    if (!editor) {
        return;
    }

    const cfg = getConfig();
    const fsPath = editor.document.uri.fsPath;
    const ext = path.extname(fsPath).toLowerCase();

    if (cfg.get('runCleanupBeforeReorganize', false)) {
        await runOnDiskThenRevert(context, cfg, editor,
            [...buildConfigArgs(cfg), '--file', fsPath, '--cleanup', cleanupLevel(cfg)],
            'cleaned + reorganized');
        return;
    }

    // Fast path: stream the buffer through the engine (works on unsaved edits too). --path lets the
    // engine apply per-path exclusions (e.g. don't reorder an EF model) to the piped buffer.
    const input = editor.document.getText();
    const args = [...buildConfigArgs(cfg), '--path', fsPath];
    if (ext !== '.cs') {
        args.push('--razor');
    }
    let output;
    try {
        output = await runEngine(context, cfg, args, input);
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

// Standalone .editorconfig cleanup of the active document (no member reorder).
async function cleanupDocument(context) {
    const editor = activeSupportedEditor();
    if (!editor) {
        return;
    }

    const cfg = getConfig();
    const fsPath = editor.document.uri.fsPath;
    await runOnDiskThenRevert(context, cfg, editor,
        [...buildConfigArgs(cfg), '--file', fsPath, '--cleanup', cleanupLevel(cfg), '--reorganize', 'false'],
        'cleaned (' + cleanupLevel(cfg) + ')');
}

// Save the document, run the engine against it on disk, then revert the buffer to the new contents.
async function runOnDiskThenRevert(context, cfg, editor, args, doneLabel) {
    try {
        await editor.document.save();
        await runEngine(context, cfg, args, undefined);
        await vscode.commands.executeCommand('workbench.action.files.revert');
        vscode.window.setStatusBarMessage('FreeCodeReorganizer: ' + doneLabel + '.', 3000);
    } catch (e) {
        vscode.window.showErrorMessage('FreeCodeReorganizer: ' + e.message);
    }
}

async function runWorkspace(context, withReorganize, withCleanup, confirmText) {
    const folders = vscode.workspace.workspaceFolders;
    if (!folders || folders.length === 0) {
        vscode.window.showWarningMessage('FreeCodeReorganizer: open a folder/workspace first.');
        return;
    }

    const choice = await vscode.window.showWarningMessage(confirmText, { modal: true }, 'Run');
    if (choice !== 'Run') {
        return;
    }

    const cfg = getConfig();
    await vscode.workspace.saveAll(false);

    const channel = vscode.window.createOutputChannel('FreeCodeReorganizer');
    channel.show(true);

    await vscode.window.withProgress(
        { location: vscode.ProgressLocation.Notification, title: 'FreeCodeReorganizer: working on the workspace…', cancellable: false },
        async () => {
            for (const folder of folders) {
                const root = folder.uri.fsPath;
                const args = [...buildConfigArgs(cfg), '--dir', root, '--reorganize', String(withReorganize)];
                if (withCleanup || (withReorganize && cfg.get('runCleanupBeforeReorganize', false))) {
                    args.push('--cleanup', cleanupLevel(cfg));
                }

                channel.appendLine('Processing ' + root + ' …');
                try {
                    channel.appendLine((await runEngine(context, cfg, args, undefined)).trimEnd());
                } catch (e) {
                    channel.appendLine('Error: ' + e.message);
                }
            }
        });

    vscode.window.showInformationMessage('FreeCodeReorganizer: workspace run complete (see the FreeCodeReorganizer output panel).');
}

function reorganizeWorkspace(context) {
    return runWorkspace(context, true, false,
        'Reorganize EVERY .cs/.razor file in the workspace? This rewrites files on disk (generated files and bin/obj are skipped). Commit your work first.');
}

function cleanupWorkspace(context) {
    return runWorkspace(context, false, true,
        'Run .editorconfig cleanup (dotnet format) on the whole workspace? This rewrites files on disk. Commit your work first.');
}

function activate(context) {
    context.subscriptions.push(
        vscode.commands.registerCommand('freecodereorganizer.reorganizeDocument', () => reorganizeDocument(context)),
        vscode.commands.registerCommand('freecodereorganizer.reorganizeWorkspace', () => reorganizeWorkspace(context)),
        vscode.commands.registerCommand('freecodereorganizer.cleanupDocument', () => cleanupDocument(context)),
        vscode.commands.registerCommand('freecodereorganizer.cleanupWorkspace', () => cleanupWorkspace(context)));
}

function deactivate() { }

module.exports = { activate, deactivate };
