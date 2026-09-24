window.footstats = {
    togglePassword: function (btn) {
        var input = btn.previousElementSibling;
        if (!input) return;
        var show = input.type === 'password';
        input.type = show ? 'text' : 'password';
        var onIcon = btn.querySelector('.eye-on');
        var offIcon = btn.querySelector('.eye-off');
        if (onIcon) onIcon.style.display = show ? 'none' : '';
        if (offIcon) offIcon.style.display = show ? '' : 'none';
        btn.setAttribute('aria-label', show ? 'Masquer le mot de passe' : 'Afficher le mot de passe');
    },
    bindDropZone: function (zone) {
        var input = zone.querySelector('input[type=file]');
        if (!input) return;

        ['dragenter', 'dragover'].forEach(function (evtName) {
            zone.addEventListener(evtName, function (e) {
                e.preventDefault();
                e.stopPropagation();
                zone.classList.add('dragging');
            });
        });

        ['dragleave', 'drop'].forEach(function (evtName) {
            zone.addEventListener(evtName, function (e) {
                e.preventDefault();
                e.stopPropagation();
                zone.classList.remove('dragging');
            });
        });

        zone.addEventListener('drop', function (e) {
            var files = e.dataTransfer && e.dataTransfer.files;
            if (files && files.length > 0) {
                input.files = files;
                input.dispatchEvent(new Event('change', { bubbles: true }));
            }
        });
    },
    getAverageColor: function (url) {
        return new Promise((resolve) => {
            const img = new Image();
            img.crossOrigin = "anonymous";
            img.onload = function () {
                try {
                    const size = 24;
                    const canvas = document.createElement('canvas');
                    canvas.width = size;
                    canvas.height = size;
                    const ctx = canvas.getContext('2d');
                    ctx.drawImage(img, 0, 0, size, size);
                    const data = ctx.getImageData(0, 0, size, size).data;
                    let r = 0, g = 0, b = 0, count = 0;
                    for (let i = 0; i < data.length; i += 4) {
                        if (data[i + 3] < 100) continue;
                        r += data[i]; g += data[i + 1]; b += data[i + 2];
                        count++;
                    }
                    if (count === 0) { resolve(null); return; }
                    r = Math.round(r / count); g = Math.round(g / count); b = Math.round(b / count);
                    resolve(`rgb(${r}, ${g}, ${b})`);
                } catch (e) {
                    resolve(null);
                }
            };
            img.onerror = function () { resolve(null); };
            img.src = url;
        });
    },
    isMobileDevice: function () {
        return /Android|iPhone|iPad|iPod|Mobile/i.test(navigator.userAgent);
    },
    openInNewTab: function (url) {
        window.open(url, '_blank');
    },
    copyText: function (text) {
        return navigator.clipboard.writeText(text).then(function () { return true; }).catch(function () { return false; });
    }
};

/* Détails du panneau d'erreur (#blazor-error-ui, cf. App.razor). Blazor affiche le panneau mais n'y écrit rien :
   le texte de l'exception .NET n'arrive au navigateur que par la console (et seulement si DetailedErrors est
   actif, sinon on reçoit l'identifiant d'erreur à recouper avec les logs serveur). On garde donc les derniers
   messages d'erreur pour les afficher dans le panneau. */
(function () {
    var MAX_ENTRIES = 4;
    var MAX_LENGTH = 2000;
    var entries = [];

    function asText(value) {
        if (value === null || value === undefined) return '';
        if (value.stack) return String(value.stack);
        if (typeof value === 'object') {
            try { return JSON.stringify(value); } catch (e) { return String(value); }
        }
        return String(value);
    }

    function record(text) {
        if (!text) return;
        if (text.length > MAX_LENGTH) text = text.slice(0, MAX_LENGTH) + ' […]';

        entries.push('[' + new Date().toLocaleTimeString() + '] ' + text);
        if (entries.length > MAX_ENTRIES) entries.shift();

        var target = document.getElementById('footstats-error-detail');
        if (target) target.textContent = entries.join('\n\n');
    }

    window.addEventListener('error', function (e) {
        var where = e.filename ? ' (' + e.filename + ':' + e.lineno + ')' : '';
        record(asText(e.error) || (e.message + where));
    });

    window.addEventListener('unhandledrejection', function (e) {
        record(asText(e.reason));
    });

    var originalConsoleError = console.error;
    console.error = function () {
        try {
            record(Array.prototype.map.call(arguments, asText).join(' '));
        } catch (e) { /* la capture ne doit jamais masquer l'erreur d'origine */ }
        originalConsoleError.apply(console, arguments);
    };

    document.addEventListener('click', function (e) {
        if (e.target.closest('#blazor-error-ui .errbox-reload')) {
            location.reload();
            return;
        }
        if (e.target.closest('#blazor-error-ui .errbox-dismiss')) {
            var ui = document.getElementById('blazor-error-ui');
            if (ui) ui.style.display = 'none';
        }
    });
})();
