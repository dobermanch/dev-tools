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
    downloadFile: function (fileName, text, contentType) {
        try {
            var blob = new Blob([text], { type: contentType || 'text/plain;charset=utf-8' });
            var url = URL.createObjectURL(blob);
            var link = document.createElement('a');
            link.href = url;
            link.download = fileName || 'download.txt';
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            // Revoked on the next tick: Safari needs the URL to outlive the click.
            setTimeout(function () { URL.revokeObjectURL(url); }, 0);
        } catch (e) {
            console.warn('downloadFile failed', e);
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
