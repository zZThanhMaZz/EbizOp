// Phase 7: tải CSV từ Blazor Server (blob + anchor click)
window.ebizDownload = {
    text: function (name, content) {
        ebizSave(name, new Blob(["﻿" + content], { type: "text/csv;charset=utf-8" }));
    },
    // base64 → ArrayBuffer (xlsx/pdf) — Blazor Server không streaming download trực tiếp
    bin: function (name, b64) {
        const u = atob(b64), buf = new Uint8Array(u.length);
        for (let i = 0; i < u.length; i++) buf[i] = u.charCodeAt(i);
        ebizSave(name, new Blob([buf]));
    },
};
function ebizSave(name, blob) {
    const a = document.createElement("a");
    a.href = URL.createObjectURL(blob);
    a.download = name;
    document.body.appendChild(a);
    a.click();
    setTimeout(() => { URL.revokeObjectURL(a.href); a.remove(); }, 2000);
}
