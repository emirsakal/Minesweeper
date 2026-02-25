# Minesweeper için Geliştirme Fikirleri

Bu dosya, mevcut projeyi (zorluk seviyeleri + bot) büyütmek için öneri backlog'u içerir.

## 1) Oynanış Özellikleri
- **Özel Oyun (Custom Mode):** Kullanıcı satır/sütun/mayın sayısını kendisi belirlesin.
- **Soru İşareti Bayrağı:** Klasik Minesweeper davranışı (kapalı -> bayrak -> soru işareti -> kapalı).
- **Chord / Hızlı Açma:** Açılmış sayılı hücrede, çevredeki bayrak sayısı sayı ile eşitse komşuları tek tıkla aç.
- **Güvenli İlk Tık Geliştirmesi:** İlk tıkta sadece 3x3 güvenli alan değil, başlangıçta daha akıcı oyun için "0" garantisi opsiyonu.
- **İpucu Sistemi:** Oyuncuya kesin güvenli bir hücreyi veya kesin mayını gösteren yardımcı mod.

## 2) Bot ve Analiz Özellikleri
- **Adım Adım Bot Açıklaması:** Bot hamleyi yapmadan önce "Neden bu hamle?" metni (Kural 1, subset, tank solver vb.).
- **Bot Hız Ayarı:** Yavaş/orta/hızlı + tek adım ilerleme.
- **Başarı İstatistiği:** Botun zorluk bazında kazanma oranı, ortalama süre, ortalama tahmin sayısı.
- **Tahmin Riski Overlay'i:** Açılmamış hücrelerde mayın olasılığı heatmap.
- **Bot Eğitim Modu:** Bot hamleyi önerir, ama uygulamaz; oyuncu uygular.

## 3) UI/UX İyileştirmeleri
- **Ayarlar Menüsü:** Ses, animasyon hızı, tema, dil, kamera ölçeği.
- **Klasik Tema + Modern Tema:** Renk setleri ve ikonlar arası geçiş.
- **Animasyon/Feedback Artırımı:** Hücre açılışında daha belirgin mikro animasyon + ses.
- **Mobil Uyum:** Uzun bas = bayrak, tek dokunma = açma; UI ölçeklendirme iyileştirmesi.
- **Erişilebilirlik:** Renk körlüğü dostu sayı renkleri ve yüksek kontrast modu.

## 4) İlerleme, Kayıt ve Rekabet
- **En İyi Süreler (Local Leaderboard):** Zorluk bazında top skor tablosu.
- **Profil ve İstatistikler:** Toplam oyun, kazanma yüzdesi, seri, ortalama süre.
- **Seed Sistemi:** Belirli seed ile aynı tahtayı tekrar oynama/paylaşma.
- **Günlük Challenge:** Her gün tek seed, herkese aynı harita.
- **Replay Sistemi:** Tüm tıkların kaydı ve tekrar izleme.

## 5) Teknik ve Mimari İyileştirmeler
- **GameConfig ScriptableObject:** Zorluklar ve oyun parametreleri data-driven olsun.
- **Board/Render Ayrımı:** Oyun mantığını ve görsel render katmanını ayırarak test edilebilirliği artır.
- **Input Katmanı Soyutlama:** Mouse, touch ve klavye girdisini aynı arayüz üzerinden yönet.
- **Object Pooling:** Hücre içi geçici objeler (edge/text) için bellek tahsisini azalt.
- **EditMode/PlayMode Testleri:** Mayın yerleşimi, kazanma/kaybetme, flood fill ve bot kuralları için test paketi.

## 6) Gelişmiş Modlar (Opsiyonel)
- **Hexagonal / Triangle Board:** Farklı komşuluk geometrileriyle varyant oyun.
- **No Guess Mode:** Teorik olarak tahminsiz çözülebilen board üretimi.
- **Co-op / Versus Modları:** Aynı board üzerinde sırayla veya eşzamanlı oynama.
- **Campaign/Puzzle Pack:** Önceden tasarlanmış öğretici seviyeler.
- **Permadeath Challenge:** Tek yanlışta run biten seri mod.

## Önerilen Öncelik Sırası (Hızlı Değer)
1. Özel Oyun + Leaderboard + Profil İstatistikleri
2. Chord + İpucu Sistemi
3. Bot açıklama modu + risk overlay
4. Tema/erişilebilirlik iyileştirmeleri
5. Replay + Daily Challenge
