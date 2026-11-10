(() => {
    const status = document.getElementById('voice-preview-status');
    const player = new PcmPlayer(48000);
    let currentPreview = null;

    function resetPreview(message, stopAudio = false) {
        if (!currentPreview) return;

        const { button, request } = currentPreview;
        currentPreview = null;
        if (stopAudio) {
            request.abort();
            player.stop();
        }

        button.textContent = 'Nghe thử';
        button.classList.remove('active');
        status.textContent = message;
    }

    for (const button of document.querySelectorAll('[data-preview-url]')) {
        button.addEventListener('click', async () => {
            if (currentPreview?.button === button) {
                resetPreview('Đã dừng nghe thử.', true);
                return;
            }

            resetPreview('', true);
            const preview = { button, request: new AbortController() };
            currentPreview = preview;
            button.textContent = 'Dừng nghe';
            button.classList.add('active');
            status.textContent = `Đang tải giọng ${button.dataset.voiceName}…`;

            try {
                player.init();
                const response = await fetch(button.dataset.previewUrl, { signal: preview.request.signal });
                if (currentPreview !== preview) return;
                if (!response.ok || !response.body || !response.headers.get('content-type')?.startsWith('audio/pcm')) {
                    throw new Error('Không nhận được âm thanh.');
                }

                status.textContent = `Đang phát giọng ${button.dataset.voiceName}…`;
                await player.playStream(response, () => {
                    if (currentPreview === preview) resetPreview('Đã phát xong giọng mẫu.');
                });
            } catch {
                if (currentPreview !== preview) return;
                resetPreview('Không thể phát giọng mẫu. Kiểm tra kết nối với VieNeu-TTS.', true);
            }
        });
    }

    window.addEventListener('pagehide', () => resetPreview('', true));
})();
