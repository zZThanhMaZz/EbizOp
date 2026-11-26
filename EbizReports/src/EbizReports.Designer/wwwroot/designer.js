// EbizReports · Designer interactivity (ES module)
// Kéo-thả / resize do JS đảm nhiệm để mượt (không round-trip mỗi pointermove).
// Khi thả chuột, JS gửi toạ độ mm cuối cùng về .NET qua DotNetObjectReference.

let st = null;

export function init(rootEl, dotNetRef, opts) {
    dispose();
    const s = {
        root: rootEl,
        dotNet: dotNetRef,
        snap: (opts && opts.snap != null) ? opts.snap : 1,
        snapOn: (opts && opts.snapOn != null) ? opts.snapOn : true,
        guidesOn: (opts && opts.guidesOn != null) ? opts.guidesOn : true,
        pxPerMm: (opts && opts.pxPerMm) ? opts.pxPerMm : (96 / 25.4),
        drag: null,
        h: {},
    };
    s.h.pd = (e) => onDown(s, e);
    s.h.pm = (e) => onMove(s, e);
    s.h.pu = (e) => onUp(s, e);
    s.h.kd = (e) => onKey(s, e);
    s.h.cm = (e) => onContextMenu(s, e);
    rootEl.addEventListener('pointerdown', s.h.pd);
    window.addEventListener('pointermove', s.h.pm);
    window.addEventListener('pointerup', s.h.pu);
    window.addEventListener('pointercancel', s.h.pu);
    window.addEventListener('keydown', s.h.kd);
    rootEl.addEventListener('contextmenu', s.h.cm);
    st = s;
}

export function setSnap(mm, on, guidesOn) {
    if (!st) return;
    st.snap = mm;
    st.snapOn = !!on;
    if (guidesOn != null) st.guidesOn = !!guidesOn;
}

export function dispose() {
    const s = st;
    if (!s) return;
    s.root.removeEventListener('pointerdown', s.h.pd);
    window.removeEventListener('pointermove', s.h.pm);
    window.removeEventListener('pointerup', s.h.pu);
    window.removeEventListener('pointercancel', s.h.pu);
    window.removeEventListener('keydown', s.h.kd);
    s.root.removeEventListener('contextmenu', s.h.cm);
    try { s.dotNet.dispose(); } catch (_) { }
    st = null;
}

// ---------------------------------------------------------------- helpers

function computeScale(s) {
    const paper = s.root.querySelector('.rd-paper');
    if (!paper) return s.pxPerMm;
    const wmm = parseFloat(paper.dataset.wmm || '0');
    const rect = paper.getBoundingClientRect();
    if (wmm > 0 && rect.width > 0) return rect.width / wmm;
    return s.pxPerMm;
}

function geomOf(el) {
    return {
        x: parseFloat(el.dataset.x || '0'),
        y: parseFloat(el.dataset.y || '0'),
        w: parseFloat(el.dataset.w || '0'),
        h: parseFloat(el.dataset.h || '0'),
    };
}

function snapVal(s, v) {
    if (!s.snapOn || !s.snap || s.snap <= 0) return Math.round(v * 10) / 10;
    return Math.round(v / s.snap) * s.snap;
}

function applyGeom(el, d, x, y, w, h) {
    el.style.left = (x * d.scale) + 'px';
    el.style.top = (y * d.scale) + 'px';
    el.style.width = (w * d.scale) + 'px';
    el.style.height = (h * d.scale) + 'px';
}

// ---------------------------------------------------------------- guides

function guidesLayer(s) {
    const paper = s.root.querySelector('.rd-paper');
    if (!paper) return null;
    let layer = paper.querySelector('.rd-guides');
    if (!layer) {
        layer = document.createElement('div');
        layer.className = 'rd-guides';
        paper.appendChild(layer);
    }
    return layer;
}

function clearGuides(s) {
    const layer = s.root.querySelector('.rd-guides');
    if (layer) layer.innerHTML = '';
}

function paperSize(s) {
    const paper = s.root.querySelector('.rd-paper');
    const w = paper ? parseFloat(paper.dataset.wmm || '0') : 0;
    const h = paper ? parseFloat(paper.dataset.hmm || '0') : 0;
    return { w, h };
}

function sameBandItems(s, primaryEl) {
    const band = primaryEl && primaryEl.closest ? primaryEl.closest('.rd-band') : null;
    if (!band) return [];
    return Array.from(band.querySelectorAll('.rd-item'));
}

/**
 * Tìm đường dóng cho khối item đang kéo (theo hộp bao).
 * Trả về delta đã chỉnh (theo mm, cộng thêm vào dx/dy gốc) và các đường cần vẽ.
 */
function findGuides(s, d, dx, dy, scale) {
    const res = { dx, dy, hitX: false, hitY: false, v: [], h: [] };
    if (!s.guidesOn) return res;

    const { w: pw, h: ph } = paperSize(s);
    const TH = 6 / scale; // 6px quy ra mm

    // hộp bao của khối đang kéo tại vị trí thô
    let minX = Infinity, maxX = -Infinity, minY = Infinity, maxY = -Infinity;
    for (const it of d.items) {
        minX = Math.min(minX, it.x + dx);
        maxX = Math.max(maxX, it.x + dx + it.w);
        minY = Math.min(minY, it.y + dy);
        maxY = Math.max(maxY, it.y + dy + it.h);
    }
    const ax = [minX, (minX + maxX) / 2, maxX]; // mép trái / giữa / phải
    const ay = [minY, (minY + maxY) / 2, maxY];

    // đường đích dọc: item khác (mọi band) + mép/giữa trang
    const vTargets = [0, pw / 2, pw];
    const hTargets = [0, ph / 2, ph];
    const moving = d.items.map(i => i.el);
    for (const el of sameBandItems(s, d.primary)) {
        if (moving.indexOf(el) >= 0) continue;
        const g = geomOf(el);
        vTargets.push(g.x, g.x + g.w / 2, g.x + g.w);
        hTargets.push(g.y, g.y + g.h / 2, g.y + g.h);
    }

    // dọc
    let bestX = null;
    for (const a of ax) {
        for (const t of vTargets) {
            const diff = t - a;
            if (Math.abs(diff) <= TH && (bestX === null || Math.abs(diff) < Math.abs(bestX.diff))) {
                bestX = { diff, line: t };
            }
        }
    }
    if (bestX) {
        res.dx = dx + bestX.diff;
        res.hitX = true;
        res.v.push(bestX.line);
        // gom mọi đích trùng đường đã chọn
        for (const t of vTargets) {
            if (Math.abs(t - bestX.line) < 0.01) res.v.push(t);
        }
    }

    // ngang
    let bestY = null;
    for (const a of ay) {
        for (const t of hTargets) {
            const diff = t - a;
            if (Math.abs(diff) <= TH && (bestY === null || Math.abs(diff) < Math.abs(bestY.diff))) {
                bestY = { diff, line: t };
            }
        }
    }
    if (bestY) {
        res.dy = dy + bestY.diff;
        res.hitY = true;
        res.h.push(bestY.line);
        for (const t of hTargets) {
            if (Math.abs(t - bestY.line) < 0.01) res.h.push(t);
        }
    }

    return res;
}

function drawGuides(s, guide, scale) {
    const layer = guidesLayer(s);
    if (!layer) return;
    layer.innerHTML = '';
    for (const x of guide.v) {
        const div = document.createElement('div');
        div.className = 'rd-guide rd-guide-v';
        div.style.left = (x * scale) + 'px';
        layer.appendChild(div);
    }
    for (const y of guide.h) {
        const div = document.createElement('div');
        div.className = 'rd-guide rd-guide-h';
        div.style.top = (y * scale) + 'px';
        layer.appendChild(div);
    }
}

// ---------------------------------------------------------------- pointer

function onContextMenu(s, e) {
    // Menu chuột phải tự dựng trong Blazor; chặn menu mặc định của trình duyệt.
    e.preventDefault();
    const t = e.target;
    const itemEl = (t && t.closest) ? t.closest('.rd-item') : null;
    const bandEl = (t && t.closest) ? t.closest('.rd-band') : null;
    const band = bandEl ? parseInt(bandEl.dataset.band, 10) : -1;
    const idx = itemEl ? parseInt(itemEl.dataset.idx, 10) : -1;

    const MW = 240, MH = 380;
    const x = Math.max(8, Math.min(e.clientX, window.innerWidth - MW));
    const y = Math.max(8, Math.min(e.clientY, window.innerHeight - MH));
    s.dotNet.invokeMethodAsync('OnContextMenu', band, idx, x, y);
}

function onDown(s, e) {
    if (e.button !== 0) return;
    const t = e.target;
    if (!t || !t.closest) return;

    // kéo giãn chiều cao band (grip nằm ở đáy band)
    const bandRz = t.closest('.rd-band-rz');
    if (bandRz && s.root.contains(bandRz)) {
        const bandEl = bandRz.closest('.rd-band');
        if (bandEl) {
            e.stopPropagation();
            e.preventDefault();
            startBandResize(s, e, bandEl);
            return;
        }
    }

    const itemEl = t.closest('.rd-item');
    if (!itemEl || !s.root.contains(itemEl)) return;

    e.stopPropagation();
    e.preventDefault();

    const rzEl = t.closest('.rd-rz');
    if (rzEl) {
        startResize(s, e, itemEl, rzEl.dataset.dir);
        return;
    }

    const band = parseInt(itemEl.dataset.band, 10);
    const idx = parseInt(itemEl.dataset.idx, 10);
    const additive = e.ctrlKey || e.metaKey || e.shiftKey;
    const cur = Array.from(s.root.querySelectorAll('.rd-item.sel'));
    const gid = itemEl.dataset.group || '';

    let next;
    let selChanged = false;
    let canDrag = true;

    if (additive) {
        if (cur.indexOf(itemEl) >= 0) {
            next = cur.filter(x => x !== itemEl);
            selChanged = true;
            canDrag = false;
        } else {
            next = cur.concat(itemEl);
            selChanged = true;
        }
    } else {
        // click thường vào item thuộc nhóm → chọn cả nhóm
        const groupEls = gid ? Array.from(s.root.querySelectorAll('.rd-item[data-group="' + gid + '"]')) : null;
        if (groupEls && groupEls.length > 1) {
            next = groupEls;
            selChanged = !(cur.length === groupEls.length && groupEls.every(x => cur.indexOf(x) >= 0));
        } else if (cur.length === 1 && cur[0] === itemEl) {
            next = cur;
        } else {
            next = [itemEl];
            selChanged = true;
        }
    }

    if (selChanged) s.dotNet.invokeMethodAsync('OnPointerSelect', band, idx, additive, gid);
    if (!canDrag) return;

    // item khoá: chỉ chọn, không kéo/resize
    if (itemEl.dataset.locked === '1') return;
    const movable = next.filter(el => el.dataset.locked !== '1');
    if (movable.length === 0) return;

    startMove(s, e, movable, itemEl);
}

function startMove(s, e, els, primaryEl) {
    const scale = computeScale(s);
    const items = els.map(el => {
        const g = geomOf(el);
        return { el, x: g.x, y: g.y, w: g.w, h: g.h };
    });
    s.drag = {
        mode: 'move', scale, sx: e.clientX, sy: e.clientY,
        items, primary: primaryEl, moved: false, pid: e.pointerId,
    };
    try { primaryEl.setPointerCapture(e.pointerId); } catch (_) { }
}

function startResize(s, e, el, dir) {
    const scale = computeScale(s);
    const g = geomOf(el);
    s.drag = {
        mode: 'resize', scale, sx: e.clientX, sy: e.clientY, dir,
        items: [{ el, x: g.x, y: g.y, w: g.w, h: g.h }],
        primary: el, moved: false, pid: e.pointerId,
    };
    try { el.setPointerCapture(e.pointerId); } catch (_) { }
}

function startBandResize(s, e, bandEl) {
    const scale = computeScale(s);
    const h0 = bandEl.getBoundingClientRect().height / scale;
    s.drag = {
        mode: 'bandh', scale, sy: e.clientY, h0, nh: h0,
        bandEl, bandIdx: parseInt(bandEl.dataset.band, 10),
        primary: bandEl, moved: false, pid: e.pointerId,
    };
    try { bandEl.setPointerCapture(e.pointerId); } catch (_) { }
}

function onMove(s, e) {
    const d = s.drag;
    if (!d) return;
    const dxPx = e.clientX - d.sx;
    const dyPx = e.clientY - d.sy;
    if (!d.moved && Math.abs(dxPx) < 3 && Math.abs(dyPx) < 3) return;
    d.moved = true;

    const dx = dxPx / d.scale;
    const dy = dyPx / d.scale;

    if (d.mode === 'bandh') {
        let nh = snapVal(s, d.h0 + dy);
        if (nh < 4) nh = 4;
        d.nh = nh;
        d.bandEl.style.height = (nh * d.scale) + 'px';
        return;
    }

    if (d.mode === 'move') {
        const p0 = d.items[0];
        let ndx = snapVal(s, p0.x + dx) - p0.x;
        let ndy = snapVal(s, p0.y + dy) - p0.y;

        // đường dóng ưu tiên hơn lưới khi nằm trong ngưỡng
        const g = findGuides(s, d, dx, dy, d.scale);
        if (g.hitX) ndx = g.dx;
        if (g.hitY) ndy = g.dy;
        if (g.v.length || g.h.length) drawGuides(s, g, d.scale);
        else clearGuides(s);

        for (const it of d.items) {
            let nx = it.x + ndx;
            let ny = it.y + ndy;
            if (nx < 0) nx = 0;
            if (ny < 0) ny = 0;
            it.nx = nx; it.ny = ny;
            applyGeom(it.el, d, nx, ny, it.w, it.h);
        }
    } else {
        clearGuides(s);
        const it = d.items[0];
        const dir = d.dir;
        let x = it.x, y = it.y, w = it.w, h = it.h;
        const MIN = 2;

        if (dir.indexOf('e') >= 0) w = snapVal(s, it.w + dx);
        if (dir.indexOf('s') >= 0) h = snapVal(s, it.h + dy);
        if (dir.indexOf('w') >= 0) {
            let nx = snapVal(s, it.x + dx);
            if (nx < 0) nx = 0;
            w = it.w + (it.x - nx);
            x = nx;
        }
        if (dir.indexOf('n') >= 0) {
            let ny = snapVal(s, it.y + dy);
            if (ny < 0) ny = 0;
            h = it.h + (it.y - ny);
            y = ny;
        }
        if (w < MIN) { if (dir.indexOf('w') >= 0) x = it.x + it.w - MIN; w = MIN; }
        if (h < MIN) { if (dir.indexOf('n') >= 0) y = it.y + it.h - MIN; h = MIN; }

        it.nx = x; it.ny = y; it.nw = w; it.nh = h;
        applyGeom(it.el, d, x, y, w, h);
    }
}

function onUp(s, e) {
    const d = s.drag;
    if (!d) return;
    s.drag = null;
    clearGuides(s);
    try { d.primary.releasePointerCapture(d.pid); } catch (_) { }
    if (!d.moved) return;

    if (d.mode === 'bandh') {
        s.dotNet.invokeMethodAsync('OnBandHeightCommitted', d.bandIdx, Math.round(d.nh * 10) / 10);
        return;
    }

    const payload = d.items.map(it => ({
        band: parseInt(it.el.dataset.band, 10),
        idx: parseInt(it.el.dataset.idx, 10),
        x: it.nx != null ? it.nx : it.x,
        y: it.ny != null ? it.ny : it.y,
        w: it.nw != null ? it.nw : it.w,
        h: it.nh != null ? it.nh : it.h,
    }));
    s.dotNet.invokeMethodAsync('OnGeometryCommitted', payload);
}

function onKey(s, e) {
    const ae = document.activeElement;
    if (ae) {
        const tag = ae.tagName;
        if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT' || ae.isContentEditable) return;
    }
    const ctrl = e.ctrlKey || e.metaKey;
    const key = (e.key || '').toLowerCase();

    if (ctrl && key === 'z' && !e.shiftKey) { e.preventDefault(); s.dotNet.invokeMethodAsync('OnUndo'); return; }
    if ((ctrl && key === 'y') || (ctrl && e.shiftKey && key === 'z')) { e.preventDefault(); s.dotNet.invokeMethodAsync('OnRedo'); return; }
    if (ctrl && key === 'd') { e.preventDefault(); s.dotNet.invokeMethodAsync('OnDuplicate'); return; }
    if (ctrl && key === 'c') { e.preventDefault(); s.dotNet.invokeMethodAsync('OnCopy'); return; }
    if (ctrl && key === 'x') { e.preventDefault(); s.dotNet.invokeMethodAsync('OnCut'); return; }
    if (ctrl && key === 'v') { e.preventDefault(); s.dotNet.invokeMethodAsync('OnPaste'); return; }
    if (ctrl && key === 'a') { e.preventDefault(); s.dotNet.invokeMethodAsync('OnSelectAll'); return; }
    if (ctrl && key === 'g' && !e.shiftKey) { e.preventDefault(); s.dotNet.invokeMethodAsync('OnGroup'); return; }
    if (ctrl && e.shiftKey && key === 'g') { e.preventDefault(); s.dotNet.invokeMethodAsync('OnUngroup'); return; }
    if (key === 'delete' || key === 'backspace') { e.preventDefault(); s.dotNet.invokeMethodAsync('OnDeleteSelected'); return; }
    if (key === 'escape') { s.dotNet.invokeMethodAsync('OnCloseContextMenu'); s.dotNet.invokeMethodAsync('OnClearSelection'); return; }

    const step = e.shiftKey ? 10 : 1;
    let dx = 0, dy = 0;
    if (key === 'arrowleft') dx = -step;
    else if (key === 'arrowright') dx = step;
    else if (key === 'arrowup') dy = -step;
    else if (key === 'arrowdown') dy = step;
    if (dx !== 0 || dy !== 0) {
        e.preventDefault();
        s.dotNet.invokeMethodAsync('OnNudge', dx, dy);
    }
}

// ---------------------------------------------------------------- misc

export function downloadText(name, text) {
    const blob = new Blob([text], { type: 'application/json;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = name;
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(url), 2000);
}

export function alertText(message) {
    window.alert(message);
}

/** Bề rộng hiển thị (px) của phần tử đầu tiên khớp selector — dùng để tính "vừa khổ". */
export function elementWidth(selector) {
    const el = document.querySelector(selector);
    return el ? el.getBoundingClientRect().width : 0;
}

export function showPdf(base64, frameId) {
    const bin = atob(base64);
    const bytes = new Uint8Array(bin.length);
    for (let i = 0; i < bin.length; i++) bytes[i] = bin.charCodeAt(i);
    const blob = new Blob([bytes], { type: 'application/pdf' });
    const url = URL.createObjectURL(blob);
    const frame = document.getElementById(frameId || 'rd-pdf');
    if (!frame) return;
    frame.style.display = 'block';
    frame.src = url + '#toolbar=1';
    frame.onload = () => setTimeout(() => URL.revokeObjectURL(url), 10000);
}
