window.devTools = {
    // The design tokens in css/tokens.css are dark by default and light under [data-theme="light"].
    // index.html stamps this before the first paint; the app calls it again whenever the theme changes.
    setTheme: function (isDarkMode) {
        try {
            document.documentElement.dataset.theme = isDarkMode ? 'dark' : 'light';
        } catch (e) {
            console.warn('setTheme failed', e);
        }
    },
    scrollToId: function (id, offset) {
        try {
            var el = document.getElementById(id);
            if (!el) return;
            var top = el.offsetTop - (offset || 0);
            window.scrollTo({ top: top, behavior: 'smooth' });
        } catch (e) {
            console.warn('scrollToId failed', e);
        }
    },
    copyToClipboard: async function (text) {
        if (!text) return;
        try {
            await navigator.clipboard.writeText(text);
        } catch (e) {
            var ta = document.createElement('textarea');
            ta.value = text;
            document.body.appendChild(ta);
            ta.select();
            document.execCommand('copy');
            document.body.removeChild(ta);
        }
    }
};
