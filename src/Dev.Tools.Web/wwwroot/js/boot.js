// Drives the boot screen in index.html while the WebAssembly runtime downloads.
//
// Two sources feed it: Blazor's own --blazor-load-percentage, which it sets on the document while
// loading and which accounts for the real download sizes, and the loadBootResource hook, which names
// each resource as it is requested. The hook never changes a request - it only reports.
window.devToolsBoot = (function () {
    var ARC_CIRCUMFERENCE = 376.99; // 2 * PI * r, r = 60 in the lockup SVG
    var LOG_LINES = 3;

    var root = document.documentElement;
    var el = {};
    var log = [];
    var current = null;
    var frame = null;
    var lastPct = -1;

    function find() {
        el.boot = document.getElementById('dt-boot');
        el.arc = document.getElementById('dt-boot-arc');
        el.fill = document.getElementById('dt-boot-fill');
        el.pct = document.getElementById('dt-boot-pct');
        el.phase = document.getElementById('dt-boot-phase');
        el.size = document.getElementById('dt-boot-size');
        el.log = document.getElementById('dt-boot-log');
    }

    // What a resource means to someone watching the screen, rather than its file name alone.
    function phaseOf(type, name) {
        switch (type) {
            case 'dotnetjs': return 'starting the .NET runtime';
            case 'dotnetwasm': return 'loading the .NET runtime';
            case 'assembly': return 'loading application code';
            case 'pdb': return 'loading debug symbols';
            case 'satelliteresource': return 'loading translations';
            case 'globalization': return 'loading globalization data';
            case 'manifest':
            case 'configuration': return 'reading the boot manifest';
            default: return 'loading ' + name;
        }
    }

    function renderLog() {
        if (!el.log) return;
        var lines = log.slice(-LOG_LINES).map(function (name) {
            return { mark: '✓', text: name, current: false };
        });
        if (current) {
            lines.push({ mark: '→', text: current, current: true });
        }
        el.log.textContent = '';
        lines.forEach(function (line) {
            var row = document.createElement('div');
            row.className = 'dt-boot-line' + (line.current ? ' dt-boot-line-current' : '');
            var mark = document.createElement('span');
            mark.className = 'dt-boot-mark-done';
            mark.textContent = line.mark;
            var text = document.createElement('span');
            text.textContent = line.text;
            row.appendChild(mark);
            row.appendChild(text);
            el.log.appendChild(row);
        });
    }

    function setProgress(pct) {
        if (pct === lastPct) return;
        lastPct = pct;
        if (el.arc) el.arc.style.strokeDasharray = (ARC_CIRCUMFERENCE * pct / 100).toFixed(1) + ' ' + ARC_CIRCUMFERENCE;
        if (el.fill) el.fill.style.width = pct + '%';
        if (el.pct) el.pct.textContent = Math.round(pct) + '%';
    }

    // Blazor writes the percentage as a CSS variable; read it each frame rather than guessing at sizes.
    function poll() {
        var raw = getComputedStyle(root).getPropertyValue('--blazor-load-percentage');
        var pct = parseFloat(raw);
        if (!isNaN(pct)) setProgress(Math.max(0, Math.min(100, pct)));
        frame = requestAnimationFrame(poll);
    }

    return {
        start: function () {
            find();
            if (!el.boot) return;
            renderLog();
            frame = requestAnimationFrame(poll);
        },

        onResource: function (type, name) {
            if (current) log.push(current);
            current = name;
            if (el.phase) el.phase.textContent = phaseOf(type, name);
            if (el.size) el.size.textContent = type;
            renderLog();
            // No return value: Blazor fetches the resource its own way.
        },

        done: function () {
            if (frame) cancelAnimationFrame(frame);
            setProgress(100);
            if (el.phase) el.phase.textContent = 'ready';
            if (el.size) el.size.textContent = '';
            // Blazor renders the app into #app, which removes this markup; hide it in case the root
            // component lives elsewhere one day.
            if (el.boot) el.boot.hidden = true;
        },

        fail: function (error) {
            if (frame) cancelAnimationFrame(frame);
            if (el.phase) el.phase.textContent = 'failed to start - reload to try again';
            if (el.size) el.size.textContent = '';
            console.error('Blazor failed to start', error);
        }
    };
})();
