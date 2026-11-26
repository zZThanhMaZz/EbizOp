// EbizReports Host · tiện ích tải file (sao lưu kho / xuất dữ liệu) bằng Blob phía trình duyệt.
window.ebReports = window.ebReports || {};

window.ebReports.downloadText = function (fileName, text, mime) {
    const type = mime || 'application/json;charset=utf-8';
    const blob = new Blob([text], { type });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    a.style.display = 'none';
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(function () { URL.revokeObjectURL(url); }, 1500);
};