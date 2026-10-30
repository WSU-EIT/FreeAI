/*
    Purpose: Defines the screen-corner positioning options for the ToastContainer component.
    Used by: ToastContainer.razor — pass as the Position parameter to choose where toasts stack.
    Accepted values: TopStart | TopCenter | TopEnd | MiddleStart | MiddleEnd | BottomStart | BottomCenter | BottomEnd
*/
namespace FreeBlazorExtended.Toast;

/// <summary>
/// Controls which screen corner the <c>ToastContainer</c> stacks toasts into.
/// Pass one of these values to <c>ToastContainer.Position</c>.
/// Defaults to <see cref="BottomEnd"/> when not specified.
/// </summary>
public enum ToastPosition
{
    TopStart,
    TopCenter,
    TopEnd,
    MiddleStart,
    MiddleEnd,
    BottomStart,
    BottomCenter,
    BottomEnd,
}
