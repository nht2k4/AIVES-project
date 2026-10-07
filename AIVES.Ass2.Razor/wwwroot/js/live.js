// Cập nhật trực tiếp bằng SignalR. Vùng nào cần tự làm mới thì có id và data-live="<topic>", ví dụ:
//   <div id="exam-list" data-live="exams"> ... </div>
// Khi server gửi sự kiện "changed" cùng topic: tải lại trang này ở chế độ nền, thay nội dung của đúng vùng đó, hiện thông báo nhỏ.
(() => {
    const regions = [...document.querySelectorAll('[data-live][id]')];
    if (!regions.length || !window.signalR) return;

    const toast = message => {
        const box = document.createElement('div');
        box.className = 'alert alert-info shadow position-fixed bottom-0 end-0 m-3';
        box.setAttribute('role', 'status');
        box.textContent = message;
        document.body.append(box);
        setTimeout(() => box.remove(), 4000);
    };

    const connection = new signalR.HubConnectionBuilder().withUrl('/hubs/live').withAutomaticReconnect().build();

    connection.on('changed', async (topic, message) => {
        const hits = regions.filter(r => r.dataset.live === topic);
        if (!hits.length) return;

        const response = await fetch(location.href, { headers: { 'X-Requested-With': 'live' } });
        if (!response.ok) return;
        const fresh = new DOMParser().parseFromString(await response.text(), 'text/html');
        for (const region of hits) {
            const next = fresh.getElementById(region.id);
            if (next) region.innerHTML = next.innerHTML;
        }
        toast(message);
    });

    connection.start().catch(err => console.warn('Không kết nối được SignalR:', err));
})();
