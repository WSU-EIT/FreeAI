/*
    Purpose: Lightweight shared file contract for SelectFile and RenderFiles.
    Fits: Carries just enough metadata for display, thumbnails, and host-supplied file content.
*/
using System;

namespace FreeBlazorExtended.SelectFile;

/// <summary>
/// Lightweight, host-agnostic representation of a file shown by the
/// <c>SelectFile</c> and <c>RenderFiles</c> components.
///
/// Defined once here under <c>FreeBlazorExtended.SelectFile</c> and reused by
/// <c>FreeBlazorExtended.RenderFiles</c> via the <c>_Imports.razor</c> chain.
/// </summary>
public class FileItem
{
    /// <summary>Stable identifier for the file.</summary>
    public Guid FileId { get; set; }

    /// <summary>Display name (e.g. <c>"diagram.png"</c>).</summary>
    public string FileName { get; set; } = "";

    /// <summary>File extension including the leading dot (e.g. <c>".png"</c>).</summary>
    public string Extension { get; set; } = "";

    /// <summary>Size in bytes; used for the friendly tooltip.</summary>
    public long Bytes { get; set; }

    /// <summary>
    /// Optional inline image bytes. When set on an image-extension file,
    /// the component renders a <c>data:</c> URL — no network round-trip.
    /// </summary>
    public byte[]? Value { get; set; }

    /// <summary>
    /// Optional URL the host has already resolved (CDN, signed URL, etc.).
    /// Used when <see cref="Value"/> is null and the extension is an image.
    /// </summary>
    public string? RemoteImageUrl { get; set; }

    /// <summary>
    /// Optional FontAwesome class for non-image files
    /// (e.g. <c>"fa-solid fa-file-pdf"</c>). Falls back to <c>fa-solid fa-file</c>.
    /// </summary>
    public string? ThumbnailIcon { get; set; }
}
