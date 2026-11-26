// Máy POS tự đăng ký — key GUID sống trong localStorage, không device code
window.ebizMachine = {
    getMachineKey() {
        let k = localStorage.getItem('ebizop.machineId');
        if (!k) {
            // crypto.randomUUID chỉ có trong secure context (https/localhost). Khách mở
            // từ điện thoại qua LAN http://<ip> là non-secure → không có hàm này, gọi ra
            // TypeError làm chết cả trang login. Tự dựng GUID v4 khi thiếu.
            k = (crypto.randomUUID && crypto.randomUUID())
                || 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, c => {
                    const r = Math.random() * 16 | 0;
                    return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16);
                });
            localStorage.setItem('ebizop.machineId', k);
        }
        return k;
    },
};

// Login qua fetch để browser nhận Set-Cookie (cookie auth + Blazor circuit)
window.ebizAuth = {
    async login(body) {
        const res = await fetch('/auth/login', {
            method: 'POST',
            headers: { 'content-type': 'application/json' },
            body: JSON.stringify(body),
        });
        return await res.json(); // { ok, error, failCount, lockSeconds, redirect }
    },
    async lockout(employeeId, machineKey) {
        const res = await fetch(`/auth/lockout?employeeId=${employeeId}&machineKey=${encodeURIComponent(machineKey)}`);
        return await res.json(); // { failCount, lockSeconds }
    },
    async logout() {
        await fetch('/auth/logout', { method: 'POST' });
    },
};
