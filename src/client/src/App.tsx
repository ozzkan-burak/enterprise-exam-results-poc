import { useState } from 'react';
import axios from 'axios';
import './App.css';

// Backend'den dönen veri tipleri
interface ExamResultResponse {
  source?: string;
  status?: string;
  message?: string;
  data?: {
    Score: number;
    Status: string;
    GeneratedAt: string;
  };
}

function App() {
  const [identityNo, setIdentityNo] = useState('');
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<ExamResultResponse | null>(null);
  const [logs, setLogs] = useState<string[]>([]);

  // Log ekleme yardımcısı
  const addLog = (msg: string) => setLogs((prev) => [msg, ...prev].slice(0, 5));

  const checkResult = async () => {
    if (!identityNo) return;

    setLoading(true);
    setResult(null);
    setLogs([]); // Logları temizle

    addLog('🚀 Sorgu başlatılıyor...');

    // Recursive Polling Fonksiyonu
    const poll = async () => {
      try {
        // Proxy sayesinde direkt /api yazıyoruz
        const response = await axios.get(
          `/api/result/check-status/${identityNo}`
        );
        const data = response.data;

        // 1. Durum: Sonuç Redis'ten geldi (SUCCESS)
        if (data.source && data.source.includes('Redis')) {
          setResult(data);
          setLoading(false); // Döngüyü bitir
          addLog("✅ Sonuç Cache'den alındı!");
        }
        // 2. Durum: Kuyrukta (QUEUED) -> Tekrar soracağız
        else if (data.status === 'QUEUED') {
          setResult(data); // "Sıraya alındı" mesajını göster
          addLog('⏳ Sırada... (RabbitMQ işliyor)');

          // 2 saniye sonra tekrar dene (Recursive Call)
          setTimeout(() => poll(), 2000);
        }
        // 3. Durum: Diğer (Hata vs.)
        else {
          setResult(data);
          setLoading(false);
        }
      } catch (error: any) {
        console.error(error);

        // 429 Too Many Requests (Time Slot Engeli)
        if (error.response && error.response.status === 429) {
          setResult({
            message: '⚠️ Trafik Kontrolü: Şu an sıranız değil!',
            status: 'BLOCKED',
          });
          addLog('⛔ Edge Katmanı tarafından engellendi.');
        } else {
          setResult({ message: 'Sunucu hatası oluştu.', status: 'ERROR' });
        }
        setLoading(false);
      }
    };

    // İlk tetikleme
    poll();
  };

  return (
    <>
      <h1>Sınav Sonuç Gateway</h1>
      <p>Yüksek Trafik Mimari POC (React + .NET + RabbitMQ + Redis)</p>

      <div className="card">
        <input
          type="text"
          placeholder="TC Kimlik No (Örn: 11111111110)"
          value={identityNo}
          onChange={(e) => setIdentityNo(e.target.value)}
          maxLength={11}
        />

        <button onClick={checkResult} disabled={loading}>
          {loading ? 'Sorgulanıyor...' : 'Sonuç Sorgula'}
        </button>

        {/* LOADING ANIMASYONU */}
        {loading && <div className="loader"></div>}

        {/* SONUÇ ALANI */}
        {result && (
          <div className="result-box">
            {/* DURUM ROZETİ */}
            {result.status === 'QUEUED' && (
              <span className="status-badge status-queue">
                Kuyrukta Bekliyor
              </span>
            )}
            {result.source?.includes('Redis') && (
              <span className="status-badge status-success">Sonuç Hazır</span>
            )}
            {result.status === 'BLOCKED' && (
              <span className="status-badge status-error">Engellendi</span>
            )}

            {/* DETAY MESAJI */}
            <p>{result.message}</p>

            {/* GERÇEK VERİ (REDIS'TEN GELDİYSE) */}
            {result.data && (
              <div
                style={{
                  marginTop: '15px',
                  borderTop: '1px solid #555',
                  paddingTop: '10px',
                }}
              >
                <h2 style={{ color: '#2ecc71' }}>Puan: {result.data.Score}</h2>
                <p>Durum: {result.data.Status}</p>
                <small>
                  Oluşturulma:{' '}
                  {new Date(result.data.GeneratedAt).toLocaleTimeString()}
                </small>
              </div>
            )}

            {/* DEBUG BİLGİSİ */}
            {result.source && (
              <small
                style={{ display: 'block', marginTop: '10px', color: '#aaa' }}
              >
                Kaynak: {result.source}
              </small>
            )}
          </div>
        )}
      </div>

      {/* LOG PANELİ (Mimariyi izlemek için) */}
      <div style={{ marginTop: '2rem', color: '#666', fontSize: '0.8rem' }}>
        {logs.map((log, i) => (
          <div key={i}>{log}</div>
        ))}
      </div>
    </>
  );
}

export default App;
