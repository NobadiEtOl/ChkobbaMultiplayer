mergeInto(LibraryManager.library, {
    CopyToClipboard: function(textPtr) {
        var text = UTF8ToString(textPtr);
        
        // Try the modern navigator.clipboard API
        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(text).then(function() {
                console.log("[ClipboardWebGL] Copied to clipboard successfully via navigator.clipboard: " + text);
            }).catch(function(err) {
                console.warn("[ClipboardWebGL] navigator.clipboard failed, attempting fallback: ", err);
                fallbackCopy(text);
            });
        } else {
            fallbackCopy(text);
        }

        function fallbackCopy(val) {
            var textArea = document.createElement("textarea");
            textArea.value = val;
            
            // Position off-screen to avoid visual disturbance
            textArea.style.position = "fixed";
            textArea.style.top = "0";
            textArea.style.left = "0";
            textArea.style.width = "2em";
            textArea.style.height = "2em";
            textArea.style.padding = "0";
            textArea.style.border = "none";
            textArea.style.outline = "none";
            textArea.style.boxShadow = "none";
            textArea.style.background = "transparent";
            
            document.body.appendChild(textArea);
            textArea.focus();
            textArea.select();

            try {
                var successful = document.execCommand('copy');
                if (successful) {
                    console.log("[ClipboardWebGL] Copied to clipboard successfully via fallback: " + val);
                } else {
                    console.error("[ClipboardWebGL] Fallback copy command was unsuccessful");
                }
            } catch (err) {
                console.error("[ClipboardWebGL] Fallback copy failed: ", err);
            }

            document.body.removeChild(textArea);
        }
    }
});
