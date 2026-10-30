/*
    Purpose: JavaScript interop helpers for the Signature component.
    Fits: Boots jSignature, clears the pad, and relays signature updates back to the Blazor component.
*/
let dotNetHelper;

export function ClearSignature(elementId) {
    $("#" + elementId).jSignature("clear");
}

export function SetDotNetHelper(value) {
    dotNetHelper = value;
}

export function SetupSignature(elementId, value) {
    let signatureElement = document.getElementById(elementId);
    if (signatureElement != undefined && signatureElement != null && !signatureElement.classList.contains("signature-added")) {
        signatureElement.classList.add("signature-added");

        $("#" + elementId).jSignature({ signatureLine: true });

        if (value != undefined && value != null && value != "") {
            $("#" + elementId).jSignature("setData", value, "base30");
        }

        $("#" + elementId).bind("change", function () {
            try {
                let hasSignature = $("#" + elementId).jSignature("getData", "native");

                if (hasSignature != null && hasSignature.length > 0) {
                    let values = $("#" + elementId).jSignature("getData", "base30");
                    let value = "";
                    if (values != null && values.length > 1) {
                        value = values[0] + "," + values[1];
                    }

                    if (dotNetHelper) {
                        dotNetHelper.invokeMethodAsync("SignatureUpdated", value).catch(function () {});
                    }
                }
            } catch (e) {
                // Swallow errors from disposed component during navigation
            }
        });
    }
}
