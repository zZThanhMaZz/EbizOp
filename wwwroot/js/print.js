// 10-3: in chứng từ qua trình duyệt (window.open + print). Headless/E2E: print là no-op.
window.ebizPrint = {
    receipt: function (title, html) {
        const w = window.open("", "_blank", "width=380,height=640");
        if (!w) return false;
        w.document.write(`<!doctype html><html><head><title>${title}</title>
            <style>body{font-family:monospace;font-size:12px;width:300px;margin:8px}
            .row{display:flex;justify-content:space-between}.grand{border-top:1px dashed #000;font-weight:700;margin-top:4px;padding-top:4px}
            .center{text-align:center}</style></head><body>${html}<script>onload=()=>{print();close()}<\/script></body></html>`);
        w.document.close();
        return true;
    },
    // In tem mã vạch qua máy in tem (driver chọn trong hộp thoại print).
    // geo = {w,h,cols,gap} mm: w×h 1 tem, cols tem/hàng (1/2/3/4 — 1 hàng = 1 trang), gap dọc giữa 2 hàng.
    // Font scale theo min(w,h) nên tem 34×25 hay 50×30 đều vừa. Headless/E2E: print là no-op.
    // CSS tem theo geo — tách riêng để E2E assert được không qua popup
    labelsCss: function (geo) {
        const { w = 50, h = 30, cols = 1, gap = 2 } = geo || {};
        const pageW = w * cols + 2, pageH = h + gap; // khổ = hàng tem + lề an toàn 1mm×2
        const s = Math.min(w, h) / 30; // 50×30 → 1.0 = cỡ gốc
        const f = (v) => (Math.round(v * s * 10) / 10);
        return `@page{size:${pageW}mm ${pageH}mm;margin:0}
        body{margin:0;font-family:Arial,Helvetica,sans-serif;width:${pageW}mm}
        .lr{width:${pageW}mm;height:${pageH}mm;display:flex;justify-content:center;align-items:center;
        gap:2mm;page-break-after:always;overflow:hidden;box-sizing:border-box}
        .label{width:${w}mm;height:${h}mm;box-sizing:border-box;padding:${f(1.5)}mm;display:flex;flex-direction:column;
        align-items:center;justify-content:center;overflow:hidden;flex:none}
        .label.ghost{visibility:hidden}
        .nm{font-size:${f(8)}pt;font-weight:bold;text-align:center;line-height:1.1;max-height:${f(9)}mm;overflow:hidden}
        .pr{font-size:${f(10)}pt;font-weight:bold}
        .label svg{height:${f(11)}mm;width:auto;max-width:${w - 3}mm}
        .sku{font-size:${f(6)}pt}`;
    },
    labels: function (title, html, geo) {
        const w = window.open("", "_blank", "width=420,height=600");
        if (!w) return false;
        w.document.write(`<!doctype html><html><head><title>${title}</title>
            <style>${window.ebizPrint.labelsCss(geo)}</style></head><body>${html}<script>onload=()=>{print();close()}<\/script></body></html>`);
        w.document.close();
        return true;
    },
};
