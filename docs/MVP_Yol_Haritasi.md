# Ustaca Eller — MVP Yol Haritası

*Sürüm 1 · 8 Ağustos 2026 · Dayanak: [Teknoloji ve Mimari Seçim Raporu](Ustaca_Eller_Teknoloji_ve_Mimari_Raporu.md)*

---

## MVP Tanımı

**3 sahne · 4 mekanik · ~15 aktivite · 1 karakter ailesi · tam çalışan abonelik ve ebeveyn bölümü.**

MVP'nin amacı özellik toplamak değil, **üç şeyi kanıtlamak**:

1. Giriş segmenti Android'de 60 FPS tutuyoruz.
2. Kids kategorisi + COPPA duvarından geçiyoruz.
3. Yeni sahne eklemek mühendis gerektirmiyor.

Üçü de kanıtlanmadan sahne sayısını artırmak, sonradan hepsini yeniden yapmak demek.

### Dil

Ürün **ilk günden çok dilli**. Bu bir özellik değil, mimari karar: sonradan eklemek her sahneyi ve her sanat varlığını yeniden elden geçirmek demek.

Kritik nokta şu: 2-6 yaş okumuyor. Ekranda metin neredeyse yok, dolayısıyla **yerelleştirilen asıl varlık seslendirme** — metin ise ağırlıklı olarak ebeveyn bölümünde. Bu yüzden üç kural manifest formatına gömüldü:

- Sahne manifestlerinde düz metin yok, yerelleştirme anahtarı var. Yeni dil hiçbir manifest'i değiştirmiyor.
- Sanat varlıklarına metin gömülmüyor. Görselin içine yazılan bir kelime, o görseli dil sayısı kadar çoğaltır.
- Ses tipli bir varlık (`voice`) dile göre `audio/<dil>/` altından çözülüyor; efekt ve ortam sesi ortak. Doğrulayıcı, efekt sesinin seslendirme yerine konmasını hata sayıyor — yoksa İngilizce kullanıcıya sessizce Türkçe klip gider.

**MVP'de yayınlanacak dil:** Türkçe (kaynak dil). İngilizce katalog baştan tutuluyor ve doğrulanıyor, ancak seslendirme maliyeti nedeniyle yayın kararı ayrı verilecek. Hat hazır olduğu için bu karar istenildiği zaman verilebilir; ek mühendislik gerektirmiyor.

**Kod dili:** tüm kaynak kod, yorumlar, değişken adları, şema açıklamaları ve araç çıktıları İngilizce. `docs/` altındaki araştırma raporları ve bu yol haritası Türkçe kalıyor — bunlar Word/PDF karşılıkları olan belge serisi, kaynak kod değil.

### Kapsam dışı (bilinçli olarak)

Bulut yedekleme · çocuk hesabı · ebeveyn paneli web sürümü · tablet'e özel yerleşim · çevrimiçi hiçbir sosyal özellik · IAP ile içerik satışı.

---

## Referans Cihaz ve Performans Bütçesi

Bu satır, projedeki en sık ihlal edilecek kural olduğu için en başa yazılıyor.

| | Değer |
|---|---|
| **Referans cihaz** | Giriş segmenti Android (Redmi / Oppo giriş modeli, 3-4 GB RAM) |
| Hedef kare süresi | 16,7 ms (60 FPS), %95'lik dilimde |
| Sahne başına aktif sprite | ≤ 180 |
| Sahne başına iskelet (Spine/Rive) | ≤ 6 eşzamanlı |
| Bellek tavanı | ≤ 400 MB |
| Soğuk açılış | ≤ 4 sn (splash kapalı) |
| İlk indirme (binary) | ≤ 150 MB |

> **"iPhone'da akıcı" bir kabul kriteri değildir.** Her mekanik ve her sahne, referans cihazda ölçülmeden "tamam" sayılmaz. Ölçüm otomatik olmalı — Faz 1'de CI'a bağlanıyor.

---

## Fazlar

### Faz 0 — Kurulum ve Teknik Doğrulama · 3 hafta

Amacı **kod üretmek değil, iki riski öldürmek**. Bu fazın çıktısı çöpe atılır.

| İş | Çıktı |
|---|---|
| Unity Hub + Unity 6 LTS kurulumu, lisans kararı (Personal / Pro) | Çalışan editör |
| Referans cihaz(lar)ın satın alınması | En az 1 giriş segmenti Android + 1 eski iPhone |
| **Spike A — Performans:** tek sahne, kesme mekaniği, 180 sprite + 4 iskelet | Referans cihazda ölçülmüş kare süresi grafiği |
| **Spike B — Uyumluluk:** boş Unity projesi, `com.unity.services.*` yok, RevenueCat entegre, Kids kategorisi beyanıyla TestFlight | Apple'dan geçmiş bir build |

**Çıkış kriteri (ikisi de zorunlu):**
- Referans cihazda 180 sprite + 4 iskelet, kesme aktifken %95'lik dilimde ≤ 16,7 ms.
- TestFlight'ta "Made for Kids / 5 ve altı" beyanıyla dağıtılabilen bir build.

> **Spike B neden bu kadar erken?** Kids kategorisi retleri genelde altıncı ayda, ilk gerçek gönderimde ortaya çıkıyor ve o noktada ödeme mimarisini değiştirmek çok pahalı. Boş bir projeyle bunu ilk üç haftada öğrenmek neredeyse bedava.

---

### Faz 1 — Çekirdek · 5 hafta

| İş | Çıktı |
|---|---|
| **Sahne manifest formatı v1** | `content/schema/scene.schema.json` — **tamam** |
| **Manifest doğrulayıcı** | `tools/validate-scenes.mjs` — **tamam**, `npm run validate:scenes` |
| **Yerelleştirme katalogları + doğrulayıcı** | `content/i18n/`, `tools/validate-i18n.mjs` — **tamam**, `npm run validate:i18n` |
| **Uyumluluk kapısı** | `tools/check-compliance.mjs` — **tamam**, `npm run compliance` |
| **Kapıların testleri + CI** | `tools/test.mjs`, `.github/workflows/ci.yml` — **tamam**, `npm test` (13 test) |
| Örnek sahne (Mutfak, 4 mekaniği de kullanan) | `content/scenes/kitchen/manifest.json` — **tamam** |
| Yerelleştirme çalışma zamanı (anahtar çözümleme, dil geri dönüşü) | C#, Unity |
| Sahne çalıştırıcı (manifest → çalışan sahne) | C#, Unity |
| Kayıt sistemi (otomatik, her etkileşim sonunda) | C#, Unity |
| Ses yöneticisi (havuzlu kısa efektler + ortam) | C#, Unity |
| Performans ölçüm koşumu (CI'da referans cihaz) | Otomatik rapor |

**Çıkış kriteri:** Manifest dosyası dışında **hiçbir C# değişikliği yapmadan** ikinci bir sahne açılabiliyor.

---

### Faz 2 — Mekanikler · 6 hafta

Sıralama kolaydan zora ve riskliyi öne alarak:

| Sıra | Mekanik | Neden bu sırada | Ana teknik iş |
|---|---|---|---|
| 1 | **Yapıştır** (sürükle-bırak-yapış) | En basit, en çok tekrarlanan; etkileşim hissinin referansı burada kurulur | Yapışma bölgeleri, yumuşak kilit, haptik + ses |
| 2 | **Boya** | Orta zorluk, bellek riski var | `RenderTexture` + periyodik bake, bölge maskeli dolgu |
| 3 | **Kes** | En riskli — gerçek geometri bölme | Poligon boolean, parça yaşam döngüsü, min parça alanı |
| 4 | **İnşa et** | Fizik kullanılmayacağı için düşündüğünden basit | Izgara + yapışma + "oturma" animasyonu |

Her mekanik kendi izole test sahnesiyle gelir ve referans cihazda ayrı ölçülür.

**Çıkış kriteri:** Dört mekanik de tek bir sahnede birlikte aktifken performans bütçesi içinde.

---

### Faz 3 — Kabuk · 4 hafta

| İş | Not |
|---|---|
| Sahne seçim ekranı | Metinsiz, ikon + seslendirme |
| Ebeveyn kapısı | Çok adımlı, okuma-yazma gerektiren; ödülsüz ve sıkıcı |
| Ebeveyn bölümü | Ayarlar, indirme yönetimi, abonelik durumu |
| Ödeme duvarı + RevenueCat | Aylık + yıllık, Aile Paylaşımı açık, TR fiyatı ayrı kademe |
| İçerik indirme yöneticisi | Addressables + kendi CDN |
| Dil seçimi | Ebeveyn bölümünde; cihaz dilinden başlar, elle değiştirilebilir |
| Yerel çocuk profilleri | Hesap değil; cihazda tutulan tercih seti |
| Anonim telemetri istemcisi | Birinci taraf, cihaz kimliği yok |

**Çıkış kriteri:** Sandbox hesabıyla uçtan uca satın alma, iptal, geri yükleme ve Aile Paylaşımı akışı çalışıyor.

---

### Faz 4 — İçerik · Faz 1'den itibaren paralel, ~10 hafta

Mühendislikten bağımsız ilerler — mimarinin bütün amacı bu.

| İş | Hedef |
|---|---|
| Karakter ailesi tasarımı + Rive durum makineleri | 1 aile, ~4 karakter, her biri 6-8 tepki durumu |
| Sahne 1: **Mutfak** | ~5 aktivite |
| Sahne 2: **Atölye** | ~5 aktivite |
| Sahne 3: **Ev** | ~5 aktivite |
| Ses tasarımı (efekt + ortam) | Dilden bağımsız, tüm etkileşimler |
| Türkçe seslendirme | Nesne adları, karakter tepkileri, ebeveyn bölümü |
| İngilizce seslendirme | **Opsiyonel** — hat hazır, yayın kararına bağlı |

**Çıkış kriteri:** Üç sahne de manifest doğrulayıcısından geçiyor ve bütçe içinde.

---

### Faz 5 — Cila, Uyumluluk ve Yayın · 4 hafta

| İş |
|---|
| Cihaz matrisi testi (en az 6 Android + 3 iOS, en eskisi 5 yaşında) |
| Uyumluluk denetimi: bağımlılık listesi, ağ trafiği kaydı, gizlilik beyanı |
| App Store Connect: "Made for Kids", yaş bandı 5 ve altı, gizlilik etiketleri |
| Play Console: Aileler politikası beyanı, hedef kitle |
| **Teacher Approved** başvurusu |
| Pedagog incelemesi ve düzeltmeler |
| Kapalı test → aşamalı yayın |

---

## Zaman Çizelgesi

```
Hafta    1  3  5  7  9  11 13 15 17 19 21 23
Faz 0    ███
Faz 1       █████
Faz 2            ██████
Faz 3                   ████
Faz 4       ██████████████████        (paralel)
Faz 5                          ████
```

**Toplam ≈ 22 hafta (5-6 ay)**, tam kadro çalıştığı varsayımıyla. En olası kayma kaynağı içerik üretimi (Faz 4), mühendislik değil.

---

## Ekip

| Rol | Yük | Ne zaman lazım |
|---|---|---|
| Unity mühendisi — sahne çalıştırıcı, mekanikler | 1 tam | Faz 0'dan |
| Unity mühendisi — kabuk, abonelik, altyapı, uyumluluk | 1 tam | Faz 0'dan |
| Oyun/etkileşim tasarımcısı | 1 tam | Faz 0'dan |
| İllüstratör + animatör (Rive/Spine) | 1 tam | Faz 1'den — **darboğaz** |
| Ses tasarımcısı | Yarı zamanlı | Faz 2'den |
| Çocuk gelişimi uzmanı / pedagog | Danışman | Faz 0 ve Faz 5 |

---

## Değişmez Kurallar

Bunlar tartışmaya açık değil; her biri ya yasal bir zorunluluk ya da geri dönüşü pahalı bir mimari karar.

1. **`com.unity.services.*` projeye girmez.** Unity Analytics ve Unity IAP, Kids kategorisinde ret sebebi. Ödeme RevenueCat ile. → `tools/check-compliance.mjs` bunu CI'da kontrol eder.
2. **Çocuk hesabı yok.** Kayıt, e-posta, isim, doğum tarihi, mikrofon, kamera yok. Çocuğun eserleri cihazda kalır.
3. **Üçüncü taraf analitik yok.** Telemetri birinci taraf, anonim ve toplu.
4. **Satın alma, dış bağlantı ve izin istekleri ebeveyn kapısının arkasında.**
5. **Karanlık desen yok.** Geri dönüş bildirimi, seri (streak), günlük ödül, çıkışta duygusal manipülasyon — hiçbiri.
6. **Sahne = veri.** Yeni sahne eklemek C# değişikliği gerektiriyorsa mimari bozulmuş demektir.
7. **Kabul ölçümü referans cihazda yapılır.**

---

## Şu Anki Durum

*13 Ağustos 2026 · commit `762e533`*

| Faz | Durum |
|---|---|
| **Faz 0** | **Açık.** Araçlar ve zincir hazır, ama iki çıkış kriterinin ikisi de karşılanmadı |
| Faz 1 | Büyük ölçüde tamam — kayıt sistemi ve ses yöneticisi eksik |
| Faz 2 | Karar mantığı yazıldı ve test edildi; **dokunma girdisi yok**, yani hiçbir mekanik oynanabilir değil |
| Faz 3 | Başlanmadı — kabuk klasörlerinde henüz kod yok |
| Faz 4 | Plan dışı erken başladı: Mutfak sahnesi çizildi. Ses, seslendirme ve animasyon yok |
| Faz 5 | Başlanmadı |

### Kurulu ve çalışır durumda

Node araç zinciri · `git-lfs` (depoda etkin) · .NET 10 SDK · Unity Hub · Unity 6 LTS 6000.0.81f1 (iOS + Android modülleri) · Xcode 26.6 + iOS 26.5 simülatör platformu · `librsvg` (çizim hattı) · uçtan uca iOS simülatör derleme zinciri (`npm run run:ios`)

### Tamamlananlar

- **İçerik hattı:** manifest şeması, sahne doğrulayıcı, yerelleştirme katalogları ve doğrulayıcısı, Kids/COPPA uyumluluk kapısı, kapıların kendi testleri, CI
- **Oyun çekirdeği** (`core/UstacaEller.Core`): kesme geometrisi, ardışık kesim yönetimi, yapışma çözümü, ızgara yerleştirme, yerelleştirme çözümü — motordan bağımsız, 54 birim testi
- **Sahne çalıştırıcı:** manifest → GameObject, kamera oturtma, letterbox
- **Sanat hattı:** vektör kaynaktan sprite üretimi; Mutfak sahnesi çizildi (17 sprite + karakter)
- **82 otomatik test** (54 çekirdek + 14 kapı + 14 Unity), üçü de yeşil

### Faz 0'ı kapatmak için eksikler

Çıkış kriteri iki maddeydi; ikisi de açık.

**1 — Referans cihazda performans ölçümü**

| Eksik | Kim yapar |
|---|---|
| Giriş segmenti Android cihaz | **Sen** — satın alma |
| Ölçüm sahnesi: 180 sprite + 4 iskelet, kesme aktif | Ben |
| Kare süresi ölçüm koşumu ve raporu | Ben |

> İskelet (Rive/Spine) henüz projede yok. Ölçümün sprite kısmı cihaz gelir gelmez yapılabilir; iskelet kısmı animasyon hattı kurulana kadar bekler. Kriterin bu yarısını ayırmak gerekebilir.

**2 — Kids kategorisi beyanıyla TestFlight'a çıkmış bir build**

| Eksik | Kim yapar |
|---|---|
| Apple Developer Program üyeliği (99 $/yıl) | **Sen** — kimlik ve ödeme gerektiriyor |
| İmzalama sertifikası ve provisioning profili | **Sen** açtıktan sonra ben |
| RevenueCat hesabı | **Sen** |
| RevenueCat Unity SDK entegrasyonu | Ben |
| App Store Connect kaydı, "Made for Kids", yaş bandı 5 ve altı | **Sen** açtıktan sonra ben |
| Gizlilik politikası metni ve URL'si (çocuk uygulamalarında zorunlu) | Ben yazarım, yayınlaması sende |
| Ebeveyn kapısı ve ödeme duvarı ekranları | Ben |

### Sıradaki kararlar

1. **IP kararı.** Şu an sahnede sıfırdan çizdiğim özgün bir karakter var — rakip analizinin "lisans sürtünmesi taşımayan" üçüncü yolu. Bunu kalıcı seçim yapıyor muyuz, yoksa TRT veya bağımsız bir yerli karakterle görüşecek miyiz? Faz 2 bitmeden netleşmeli.
2. **İllüstratör.** Mevcut çizimler benim elimden çıktı ve tutarlı, ama bu kategoride ürünü kazandıran üretim kalitesi için profesyonel bir illüstratör gerekiyor.
3. **Ses tasarımcısı ve Türkçe seslendirme** — Faz 2 sonrası.
