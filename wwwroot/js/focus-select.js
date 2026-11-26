// Focus ô nhập → chọn hết nội dung để gõ đè (kể cả click chuột).
// MudNumericField (Blazor re-render sau focusin) dời caret về cuối → bọc lại sau 120ms.
// input type=number: Chrome ném InvalidStateError khi gọi select() → không bắt (không đưa vào TYPES).
(function () {
    var TYPES = ['text', 'search', 'tel', 'url', 'email', 'password'];
    // Drag chuột chọn vùng con: mousedown lần 2 trên ô đang focus thì giữ nguyên caret.
    var mouseDown = false, dragging = false, downOnFocused = false;
    document.addEventListener('mousedown', function (e) {
        mouseDown = true; dragging = false;
        downOnFocused = (document.activeElement === e.target);
    }, true);
    document.addEventListener('mousemove', function () {
        if (mouseDown) dragging = true;
    }, true);
    document.addEventListener('mouseup', function () {
        mouseDown = false;
    }, true);
    document.addEventListener('focusin', function (e) {
        var el = e.target;
        if (!el) return;
        if (el.tagName === 'TEXTAREA') {
            try { el.select(); } catch (_) { }
            return;
        }
        if (el.tagName !== 'INPUT' || TYPES.indexOf(el.type) < 0) return;
        // Click lần 2 trên ô đang focus + đang kéo chuột (drag chọn vùng) → giữ nguyên.
        if (downOnFocused && dragging) { downOnFocused = false; dragging = false; return; }
        downOnFocused = false; dragging = false;
        try { el.select(); } catch (_) { }
        // Chỉ re-select khi người dùng CHƯA gõ gì.
        // Dùng flag `typed` thay vì so-sánh value vì MudNumericField format lại
        // value khi focus (vd 200000 → 200.000) → so-sánh value luôn lệch.
        var typed = false;
        function markTyped() { typed = true; }
        el.addEventListener('input', markTyped, { once: true });
        el.addEventListener('keydown', markTyped, { once: true });
        setTimeout(function () {
            el.removeEventListener('input', markTyped);
            el.removeEventListener('keydown', markTyped);
            if (typed) return;                          // đã gõ → đừng chọn lại
            try { if (document.activeElement === el) el.select(); } catch (_) { }
        }, 120);
    });
})();
