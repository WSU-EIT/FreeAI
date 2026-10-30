/*
    Purpose: JavaScript layout helper for the InfoTip component.
    Fits: Computes whether the popover should align right based on current viewport space.
*/
export function shouldAlignRight(element, popoverWidth) {
    const rect = element.getBoundingClientRect();
    return (window.innerWidth - rect.left) < (popoverWidth + 16);
}
