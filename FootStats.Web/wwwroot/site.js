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
