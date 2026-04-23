(function () {
    const antiForgery = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';

    const selectAll     = document.getElementById('selectAll');
    const btnBlock      = document.getElementById('btnBlock');
    const btnUnblock    = document.getElementById('btnUnblock');
    const btnDelete     = document.getElementById('btnDelete');
    const btnDelUnver   = document.getElementById('btnDeleteUnverified');
    const filterInput   = document.getElementById('filterInput');

    function initMiniCharts() {
        document.querySelectorAll('.activity-sparkline').forEach(el => {
            const row = el.closest('tr');
            const raw = row?.dataset.activity || '';
            const data = raw.split(',').map(s => parseInt(s || '0'));
            renderSparkline(el, data, row.classList.contains('blocked-row'));
        });
    }

  
    function generateActivityData() {
        const data = [];
        for (let i = 0; i < 8; i++) data.push(Math.floor(Math.random() * 5) + 1);
        return data;
    }

    function buildLabels(lastLoginTicks) {
        const labels = [];
        const now = new Date();
        let last = null;
        try { last = lastLoginTicks && lastLoginTicks !== '0' ? new Date(parseInt(lastLoginTicks) / 10000) : null; } catch (e) { last = null; }
        for (let i = 7; i >= 0; i--) {
            const d = last ? new Date(now - i * 24 * 60 * 60 * 1000) : new Date(now - i * 24 * 60 * 60 * 1000);
            labels.push((d.getMonth() + 1) + '/' + d.getDate());
        }
        return labels;
    }

    function renderSparkline(container, data, faded) {
        if (!container) return;
        const w = Math.max(80, container.clientWidth || 100);
        const h = Math.max(18, container.clientHeight || 22);
        const padding = 2;
        const barCount = Math.max(1, data.length);
        const gap = 3;
        const barWidth = Math.max(2, Math.floor((w - padding * 2 - gap * (barCount - 1)) / barCount));
        const max = Math.max(1, ...data);

        const svgNS = 'http://www.w3.org/2000/svg';
        const svg = document.createElementNS(svgNS, 'svg');
        svg.setAttribute('width', w);
        svg.setAttribute('height', h);

        data.forEach((v, i) => {
            const bw = barWidth;
            const x = padding + i * (bw + gap);
            const barH = Math.round((v / max) * (h - padding * 2));
            const y = h - padding - barH;
            const rect = document.createElementNS(svgNS, 'rect');
            rect.setAttribute('x', x);
            rect.setAttribute('y', y);
            rect.setAttribute('width', bw);
            rect.setAttribute('height', barH);
            rect.setAttribute('fill', faded ? '#9aa3ad' : '#6b9bff');
            rect.setAttribute('data-value', v);
            rect.setAttribute('data-idx', i);
            rect.style.rx = '3';
            rect.style.ry = '3';
            rect.addEventListener('mouseenter', (ev) => showSparkTooltip(ev, v, i));
            rect.addEventListener('mouseleave', hideSparkTooltip);
            svg.appendChild(rect);
        });

        container.innerHTML = '';
        container.appendChild(svg);
    }

    let _sparkTooltip = null;
    function showSparkTooltip(ev, value, idx) {
        if (!_sparkTooltip) {
            _sparkTooltip = document.createElement('div');
            _sparkTooltip.style.position = 'fixed';
            _sparkTooltip.style.padding = '6px 8px';
            _sparkTooltip.style.background = 'rgba(0,0,0,0.8)';
            _sparkTooltip.style.color = '#fff';
            _sparkTooltip.style.fontSize = '12px';
            _sparkTooltip.style.borderRadius = '4px';
            _sparkTooltip.style.pointerEvents = 'none';
            document.body.appendChild(_sparkTooltip);
        }
        _sparkTooltip.textContent = `${new Date().toLocaleDateString()} ${new Date().toLocaleTimeString()} — ${value} action${value === 1 ? '' : 's'}`;
        _sparkTooltip.style.display = 'block';
        _sparkTooltip.style.left = (ev.clientX + 12) + 'px';
        _sparkTooltip.style.top = (ev.clientY + 12) + 'px';
    }
    function hideSparkTooltip() { if (_sparkTooltip) _sparkTooltip.style.display = 'none'; }

    function getChecked() {
        return [...document.querySelectorAll('.row-check:checked')].map(cb => cb.value);
    }

    function updateToolbar() {
        const ids = getChecked();
        const any = ids.length > 0;
        [btnBlock, btnUnblock, btnDelete, btnDelUnver].forEach(b => b.disabled = !any);

        const visibleBoxes = [...document.querySelectorAll('#tableBody tr:not([style*="display: none"]) .row-check')];
        const checkedVisible = visibleBoxes.filter(cb => cb.checked);
        selectAll.indeterminate = checkedVisible.length > 0 && checkedVisible.length < visibleBoxes.length;
        selectAll.checked = visibleBoxes.length > 0 && checkedVisible.length === visibleBoxes.length;
    }

    selectAll.addEventListener('change', function () {
        document.querySelectorAll('#tableBody tr:not([style*="display: none"]) .row-check')
            .forEach(cb => cb.checked = this.checked);
        updateToolbar();
    });
    document.querySelectorAll('.row-check').forEach(cb => cb.addEventListener('change', updateToolbar));

    filterInput.addEventListener('input', function () {
        const q = this.value.toLowerCase();
        document.querySelectorAll('#tableBody tr').forEach(row => {
            const match = (row.dataset.name + ' ' + row.dataset.email + ' ' + row.dataset.status).includes(q);
            row.style.display = match ? '' : 'none';
            if (!match) row.querySelector('.row-check').checked = false;
        });
        updateToolbar();
    });

    let sortCol = 'lastlogin', sortDir = -1;
    document.querySelectorAll('.sortable').forEach(th => {
        th.addEventListener('click', function () {
            const col = this.dataset.col;
            if (sortCol === col) sortDir *= -1; else { sortCol = col; sortDir = 1; }
            document.querySelectorAll('.sortable .sort-icon').forEach(i => i.textContent = '⇅');
            this.querySelector('.sort-icon').textContent = sortDir === 1 ? '↑' : '↓';
            const tbody = document.getElementById('tableBody');
            [...tbody.querySelectorAll('tr')]
                .sort((a, b) => {
                    let av = a.dataset[col] ?? '', bv = b.dataset[col] ?? '';
                    if (col === 'lastlogin') return (parseInt(av) - parseInt(bv)) * sortDir;
                    return av.localeCompare(bv) * sortDir;
                })
                .forEach(r => tbody.appendChild(r));
        });
    });

    const toastEl  = document.getElementById('statusToast');
    const toastMsg = document.getElementById('toastMessage');
    const bsToast  = new bootstrap.Toast(toastEl, { delay: 3500 });

    function showToast(message, isError = false) {
        toastEl.className = `toast align-items-center border-0 text-bg-${isError ? 'danger' : 'success'}`;
        toastMsg.textContent = message;
        bsToast.show();
    }

    const confirmModal = new bootstrap.Modal(document.getElementById('confirmModal'));
    const confirmTitle = document.getElementById('confirmTitle');
    const confirmMsg   = document.getElementById('confirmMessage');
    const confirmOk    = document.getElementById('confirmOk');
    let pendingAction  = null;

    function askConfirm(title, message, onOk) {
        confirmTitle.textContent = title;
        confirmMsg.textContent   = message;
        pendingAction = onOk;
        confirmModal.show();
    }
    confirmOk.addEventListener('click', () => {
        confirmModal.hide();
        if (pendingAction) { pendingAction(); pendingAction = null; }
    });

    async function doAction(url, ids) {
        try {
            const res = await fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': antiForgery,
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: JSON.stringify({ ids })
            });

            if (res.status === 401 || res.redirected) {
                window.location.href = '/Account/Login';
                return;
            }

            const data = await res.json();

            if (data.redirectUrl) {
                window.location.href = data.redirectUrl;
                return;
            }
            if (data.success) {
                showToast(data.message);
                setTimeout(() => location.reload(), 1200);
            } else {
                showToast(data.message, true);
            }
        } catch (e) {
            showToast('An error occurred. Please try again.', true);
        }
    }

    btnBlock.addEventListener('click', () =>
        doAction('/Users/Block', getChecked()));

    btnUnblock.addEventListener('click', () =>
        doAction('/Users/Unblock', getChecked()));

    btnDelete.addEventListener('click', () =>
        askConfirm('Delete Users', 'Delete selected users? This cannot be undone.', () =>
            doAction('/Users/Delete', getChecked())));

    btnDelUnver.addEventListener('click', () =>
        askConfirm('Delete Unverified', 'Delete unverified users from selection?', () =>
            doAction('/Users/DeleteUnverified', getChecked())));
    try {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', initMiniCharts);
        } else {
            initMiniCharts();
        }
    } catch (e) { console.warn('Sparkline init failed', e); }
})();
