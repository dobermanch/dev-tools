window.devTools = {
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
