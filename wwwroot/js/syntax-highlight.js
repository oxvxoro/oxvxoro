window.syntaxHighlight = {
    highlight: function (element) {
        if (typeof Prism === "undefined") {
            return;
        }

        try {
            if (element) {
                Prism.highlightAllUnder(element);
            } else {
                Prism.highlightAll();
            }
        } catch (e) {
            console.error("syntaxHighlight.highlight failed", e);
        }
    }
};
