(() => {
    const status = document.getElementById('voice-preview-status');
    const player = new PcmPlayer(48000);
    let currentPreview;

    for (const button of document.querySelectorAll('[data-preview-url]')) {
        button.addEventListener('click', async () => {
            currentPreview?.abort();
            player.stop();

            const preview = new AbortController();
            currentPreview = preview;
            status.textContent = `Đang tải giọng ${button.dataset.voiceName}…`;

            try {
                player.init();
                const response = await fetch(button.dataset.previewUrl, { signal: preview.signal });
                if (!response.ok || !response.headers.get('content-type')?.startsWith('audio/pcm')) {
                    throw new Error('Không nhận được âm thanh.');
                }

                status.textContent = `Đang phát giọng ${button.dataset.voiceName}…`;
                await player.playStream(response, () => {
                    if (currentPreview === preview) status.textContent = '';
                });
            } catch {
                if (currentPreview !== preview) return;

                currentPreview = null;
                player.stop();
                status.textContent = 'Không thể phát giọng mẫu. Kiểm tra kết nối với VieNeu-TTS.';
            }
        });
    }

    window.addEventListener('pagehide', () => {
        currentPreview?.abort();
        player.stop();
    });
})();
