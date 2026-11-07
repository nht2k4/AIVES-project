// Phát âm thanh PCM s16le, 48 kHz mono theo luồng bằng Web Audio API
class PcmPlayer {
    constructor(sampleRate = 48000) {
        this.sampleRate = sampleRate;
        this.audioCtx = null;
        this.nextStartTime = 0;
        this.activeSources = [];
    }

    init() {
        if (!this.audioCtx) {
            const AudioContext = window.AudioContext || window.webkitAudioContext;
            this.audioCtx = new AudioContext({ sampleRate: this.sampleRate });
        }
        if (this.audioCtx.state === 'suspended') {
            this.audioCtx.resume();
        }
    }

    feed(int16Chunk) {
        this.init();
        if (!int16Chunk || int16Chunk.length === 0) return;

        const float32 = new Float32Array(int16Chunk.length);
        for (let i = 0; i < int16Chunk.length; i++) {
            float32[i] = int16Chunk[i] / 32768.0;
        }

        const buffer = this.audioCtx.createBuffer(1, float32.length, this.sampleRate);
        buffer.getChannelData(0).set(float32);

        const source = this.audioCtx.createBufferSource();
        source.buffer = buffer;
        source.connect(this.audioCtx.destination);

        const currentTime = this.audioCtx.currentTime;
        if (this.nextStartTime < currentTime) {
            this.nextStartTime = currentTime + 0.05;
        }

        source.start(this.nextStartTime);
        this.nextStartTime += buffer.duration;
        this.activeSources.push(source);

        source.onended = () => {
            const idx = this.activeSources.indexOf(source);
            if (idx > -1) this.activeSources.splice(idx, 1);
        };
    }

    stop() {
        for (const s of this.activeSources) {
            try { s.stop(); } catch (e) {}
        }
        this.activeSources = [];
        this.nextStartTime = 0;
    }

    async playStream(response, onFinished) {
        this.init();
        this.stop();
        const reader = response.body.getReader();
        let leftover = null;

        try {
            while (true) {
                const { done, value } = await reader.read();
                if (done) break;

                let data = value;
                if (leftover && leftover.length > 0) {
                    const combined = new Uint8Array(leftover.length + data.length);
                    combined.set(leftover, 0);
                    combined.set(data, leftover.length);
                    data = combined;
                    leftover = null;
                }

                const remainder = data.length % 2;
                if (remainder > 0) {
                    leftover = data.slice(data.length - remainder);
                    data = data.slice(0, data.length - remainder);
                }

                if (data.length > 0) {
                    const int16Array = new Int16Array(data.buffer, data.byteOffset, data.length / 2);
                    this.feed(int16Array);
                }
            }
        } finally {
            if (onFinished) {
                const remainingTime = Math.max(0, (this.nextStartTime - (this.audioCtx ? this.audioCtx.currentTime : 0)) * 1000);
                setTimeout(onFinished, remainingTime + 100);
            }
        }
    }
}

window.PcmPlayer = PcmPlayer;
