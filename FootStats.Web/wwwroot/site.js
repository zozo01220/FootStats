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

/* Recadrage circulaire d'une photo (PhotoCropper.razor). Tout le pan/zoom se fait ici, en JS pur, sans aller-retour
   Blazor : seuls le choix du fichier et la validation finale (export du canvas) appellent .NET. L'état par canvas
   (image chargée, zoom, décalage) est gardé dans une WeakMap plutôt que sur le composant Blazor. */
footstats.cropper = (function () {
    var states = new WeakMap();

    // Ratio du masque circulaire (marge / côté), doit rester synchronisé avec la marge de .photo-cropper-mask
    // dans theme.css (18px sur une boîte de 220px) pour que le cercle CSS corresponde au cadrage exporté.
    var MASK_MARGIN_RATIO = 18 / 220;

    function circleRadius(canvas) {
        var size = canvas.width;
        return (size / 2) - (MASK_MARGIN_RATIO * size);
    }

    function baseDrawSize(canvas, state) {
        var r = circleRadius(canvas);
        var diameter = r * 2;
        var ratio = state.img.width / state.img.height;
        var w, h;
        if (ratio >= 1) { h = diameter; w = diameter * ratio; } else { w = diameter; h = diameter / ratio; }
        return { w: w * state.scale, h: h * state.scale };
    }

    function clamp(canvas, state) {
        var d = baseDrawSize(canvas, state);
        var r = circleRadius(canvas);
        var maxX = Math.max(0, d.w / 2 - r);
        var maxY = Math.max(0, d.h / 2 - r);
        state.offsetX = Math.min(maxX, Math.max(-maxX, state.offsetX));
        state.offsetY = Math.min(maxY, Math.max(-maxY, state.offsetY));
    }

    function draw(canvas) {
        var state = states.get(canvas);
        if (!state) return;
        var ctx = canvas.getContext('2d');
        var size = canvas.width;
        ctx.clearRect(0, 0, size, size);
        var d = baseDrawSize(canvas, state);
        var cx = size / 2 + state.offsetX;
        var cy = size / 2 + state.offsetY;
        ctx.drawImage(state.img, cx - d.w / 2, cy - d.h / 2, d.w, d.h);
    }

    function loadUrl(canvas, url, revokeAfter) {
        var img = new Image();
        img.onload = function () {
            states.set(canvas, { img: img, scale: 1.4, offsetX: 0, offsetY: 0 });
            draw(canvas);
            if (revokeAfter) URL.revokeObjectURL(url);
        };
        img.src = url;
    }

    return {
        loadFromFile: function (canvas, file) {
            loadUrl(canvas, URL.createObjectURL(file), true);
        },
        loadFromUrl: function (canvas, url) {
            loadUrl(canvas, url, false);
        },
        setZoom: function (canvas, value) {
            var state = states.get(canvas);
            if (!state) return;
            state.scale = value / 100;
            clamp(canvas, state);
            draw(canvas);
        },
        bindFileInput: function (input, canvas, dotNetRef) {
            input.addEventListener('change', function () {
                var file = input.files && input.files[0];
                if (!file) return;
                footstats.cropper.loadFromFile(canvas, file);
                dotNetRef.invokeMethodAsync('OnImageReady');
                input.value = '';
            });
        },
        bindZoomSlider: function (slider, canvas) {
            slider.addEventListener('input', function () {
                footstats.cropper.setZoom(canvas, slider.value);
            });
        },
        bindDrag: function (wrap, canvas) {
            var dragging = false;
            var start = { x: 0, y: 0, offX: 0, offY: 0 };

            function pos(e) {
                var rect = wrap.getBoundingClientRect();
                var clientX = e.touches ? e.touches[0].clientX : e.clientX;
                var clientY = e.touches ? e.touches[0].clientY : e.clientY;
                return {
                    x: (clientX - rect.left) * (canvas.width / rect.width),
                    y: (clientY - rect.top) * (canvas.height / rect.height)
                };
            }

            function down(e) {
                if (!states.get(canvas)) return;
                dragging = true;
                wrap.classList.add('dragging');
                var p = pos(e);
                var state = states.get(canvas);
                start = { x: p.x, y: p.y, offX: state.offsetX, offY: state.offsetY };
            }

            function move(e) {
                if (!dragging) return;
                e.preventDefault();
                var state = states.get(canvas);
                if (!state) return;
                var p = pos(e);
                state.offsetX = start.offX + (p.x - start.x);
                state.offsetY = start.offY + (p.y - start.y);
                clamp(canvas, state);
                draw(canvas);
            }

            function up() {
                dragging = false;
                wrap.classList.remove('dragging');
            }

            wrap.addEventListener('mousedown', down);
            window.addEventListener('mousemove', move);
            window.addEventListener('mouseup', up);
            wrap.addEventListener('touchstart', down, { passive: true });
            wrap.addEventListener('touchmove', move, { passive: false });
            wrap.addEventListener('touchend', up);
        },
        exportCrop: function (canvas) {
            // Renvoyer le Blob brut : appelée via JS.InvokeAsync<IJSStreamReference>, c'est Blazor qui le
            // convertit en flux. DotNet.createJSStreamReference ne sert qu'à passer un flux en ARGUMENT d'un
            // appel .NET déclenché depuis JS (invokeMethodAsync), pas comme valeur de retour ici.
            return new Promise(function (resolve, reject) {
                canvas.toBlob(function (blob) {
                    if (!blob) { reject('export-failed'); return; }
                    resolve(blob);
                }, 'image/png');
            });
        }
    };
})();

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
