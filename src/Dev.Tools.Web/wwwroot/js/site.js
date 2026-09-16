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
    // Ctrl+K, or Cmd+K on a Mac, wherever you are in the app. Registered once by the layout; the browser's
    // own use of the combination is suppressed, since this is the app's search.
    registerCommandShortcut: function (dotNetRef, methodName) {
        window.devTools.unregisterCommandShortcut();
        window.devTools._commandHandler = function (e) {
            if ((e.metaKey || e.ctrlKey) && (e.key === 'k' || e.key === 'K')) {
                e.preventDefault();
                dotNetRef.invokeMethodAsync(methodName);
            }
        };
        document.addEventListener('keydown', window.devTools._commandHandler);
        // Answers whether to label the shortcut Cmd or Ctrl.
        return /mac|iphone|ipad/.test((navigator.userAgentData && navigator.userAgentData.platform || navigator.platform || '').toLowerCase());
    },
    unregisterCommandShortcut: function () {
        if (window.devTools._commandHandler) {
            document.removeEventListener('keydown', window.devTools._commandHandler);
            window.devTools._commandHandler = null;
        }
    },
    // Returns what happened, so the caller knows whether to say "copied": 'shared' when the system sheet
    // took it, 'cancelled' when the person dismissed that sheet, 'copied' or 'failed' otherwise.
    share: async function (title, url) {
        if (navigator.share) {
            try {
                await navigator.share({ title: title, url: url });
                return 'shared';
            } catch (e) {
                if (e && e.name === 'AbortError') return 'cancelled';
            }
        }
        try {
            await navigator.clipboard.writeText(url);
            return 'copied';
        } catch (e) {
            console.warn('share failed', e);
            return 'failed';
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
