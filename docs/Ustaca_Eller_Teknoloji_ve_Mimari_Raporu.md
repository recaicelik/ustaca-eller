# "Ustaca Eller" — Teknoloji ve Mimari Seçim Raporu

**Açık uçlu teknoloji değerlendirmesi · iOS + Android · Kod yazımı öncesi fizibilite**
*8 Ağustos 2026 · Önceki pazar raporunun 7.5 numaralı fikri ve rakip analizi üzerine kurulu*

---

## Yönetici Özeti

Bu rapor "Ustaca Eller" fikri (2-6 yaş, açık uçlu dijital oyuncak, kesme/yapıştırma/boyama/inşa etme, IAP kapalı, tek gelir abonelik) için doğru teknoloji ve mimariyi, hiçbir framework'ü baştan zorunlu tutmadan değerlendiriyor.

**Karar: Unity 6, tüm uygulama için — sadece oyun sahneleri için değil.**

Gerekçe dört başlıkta toplanıyor:

1. **Bu ürün bir uygulama değil, bir oyun.** Serbest keşif sahnesi, kare başına yeniden çizim, çok parmaklı sürükleme ve kesme, onlarca eşzamanlı animasyonlu nesne, düşük gecikmeli çakışan ses. Bunlar uygulama framework'lerinin değil, oyun motorlarının problemleri.
2. **Kategorinin tamamı bunu doğruluyor.** Toca Boca ve Sago Mini — yani doğrudan taklit edilecek kalite çıtası — Unity ve C# ile yazılıyor; Toca Boca kendi render altyapısını bile Unity'nin `BatchRendererGroup` API'si üzerine kurmuş. Bu kategoride Unity dışında ölçeklenmiş bir örnek yok.
3. **Türkiye'de yetenek havuzu burada.** Peak (Zynga'ya 1,8 milyar $), Dream Games, Rollic gibi çıkışların yarattığı ekosistem sayesinde Unity Türkiye'de fiilen sektör standardı ve 2026'da Unity geliştiricisine talep zirvede. React Native ile Skia/worklet seviyesinde grafik yazabilen mühendisi Türkiye'de bulmak ise çok daha zor ve pahalı. Bu proje için **en kıt kaynak yetenek, en bol kaynak Unity yeteneği**.
4. **Ölçülen performans farkı ciddi.** Bağımsız bir kıyaslamada iPad Air 4 üzerinde kare düşürmeden taşınan varlık sayısı Unity'de ~2.000 iken Flame'de ~1.200, saf Flutter'da ~800. React Native + Skia tarafında ise giriş segmenti Android'de (OPPO A16) taban ~300 sprite — aynı testte iPhone 12 mini ~15.000. Türkiye'de Android payı iOS'un iki katından fazla ve cihaz tabanının ağırlığı tam olarak o giriş segmentinde.

**İkinci bulgu — asıl sert kısıt performans değil, uyumluluk.** Apple'ın Kids kategorisi üçüncü taraf analitiği tamamen yasaklıyor. Unity için bu somut bir tuzak yaratıyor: **Unity Analytics IP adresi, reklam ve cihaz tanımlayıcıları topluyor ve bu yüzden Kids kategorisinde ret sebebi olmuş durumda; dahası Unity IAP paketi varsayılan olarak Analytics olmadan açılamıyor.** Yani Unity'nin kendi satın alma çözümü bu projede kullanılamaz. Çözüm, ödeme tarafında RevenueCat'in Unity SDK'sını kullanmak ve `com.unity.services.*` paketlerinin tamamını projeden çıkarmak. Bu, ilk günden verilmesi gereken bir karar.

**Üçüncü bulgu — ciddi bir alternatif var: Godot 4.6.** Godot'nun 2026'daki en çok dile getirilen zayıflığı analitik/SDK ekosisteminin yokluğu. Ama bu üründe **üçüncü taraf analitik zaten yasak** — yani Godot'nun en büyük eksiği bu proje için hiç eksi değil. Ocak 2026'daki 4.6 sürümü StoreKit 2 ve Google Play Billing entegrasyonlarını resmî olarak getirdi, çökme oranları %4'ten %1'in altına indi ve lisans tamamen ücretsiz. Godot'yu ikinci sıraya düşüren tek şey teknik değil, insan: Türkiye'de Godot ile üretim yapmış ekip bulmak zor. **Ekip Unity ile kurulamıyorsa Godot gerçek bir B planı, ödün değil.**

**React Native bu ürün için önerilmiyor.** Yapılabilir — Skia + Reanimated worklet ile kurulabilecek bir yol var — ama motor katmanının (sprite atlası, sahne yığını, varlık hattı, ses senkronu, girdi soyutlaması) tamamını kendiniz yazmanız gerekir. Bu, bu projede kod ile içerik arasındaki dengeyi yanlış tarafa kaydırır: bu kategoride ürünü kazandıran şey üretim kalitesi ve içerik hızı, kod mimarisi değil. Motor yazmakla geçen her ay, sahne yapmakla geçmeyen bir aydır.

---

## 1. Teknik Gereksinim Profili

Teknoloji seçiminden önce, seçimin neye göre yapıldığını yazmak gerekiyor. Fikrin gerektirdikleri:

| Gereksinim | Ayrıntı | Zorluk |
|---|---|---|
| Serbest keşif sahnesi | Sabit akış yok; çocuk her nesneyle her sırada oynayabilir | Yüksek |
| Kare başına render | 60 FPS, sürekli, sahne boyunca | Yüksek |
| Çok parmaklı girdi | Aynı anda sürükleme + çizim; avuç içi teması, kaotik dokunuş | Orta |
| Geometri işlemleri | Kesme = gerçek şekil bölme; boyama = kalıcı tuval | Orta |
| Karakter animasyonu | Dokunuşa tepki veren, durum makineli, iskelet tabanlı | Yüksek |
| Ses | Onlarca kısa efekt eşzamanlı, düşük gecikme, ortam müziği | Orta |
| Varlık hacmi | Sahne başına yüzlerce sprite, atlas, ses, animasyon | Yüksek |
| Hedef cihaz | Giriş segmenti Android (Türkiye ağırlıklı) | **Belirleyici** |
| Uyumluluk | Üçüncü taraf analitik/reklam yok, çocuk verisi yok | **Belirleyici** |
| Gelir | Tek abonelik, IAP yok, Aile Paylaşımı | Düşük |
| İçerik hızı | Yeni sahne yayınlamak sürüm çıkarmayı gerektirmemeli | Yüksek |

Son iki satır özellikle önemli: bu ürünün ticari başarısı sürüm sonrası içerik akışına bağlı (Sago Mini ve Toca Boca'nın tekil uygulamalardan abonelik paketine geçmesinin sebebi tam olarak bu). Yani seçilecek teknoloji, "ilk sürümü çıkarabilir miyiz" sorusundan çok "üçüncü yılda ayda iki sahne yayınlayabilir miyiz" sorusuna göre değerlendirilmeli.

---

## 2. Aday Teknolojiler ve Karşılaştırma

### 2.1 Ölçülmüş veriler

Bağımsız bir kıyaslama (Filip Hracek, eski Flutter ekibi üyesi), aynı test sahnesini dört ortamda çalıştırmış. iOS tarafı (iPad Air 4. nesil):

| Motor | 60 FPS'te taşınan varlık | Açılış süresi | iOS bellek (1.000 varlık) | iOS CPU |
|---|---|---|---|---|
| **Unity** | ~2.000 | ~2,9 sn (zorunlu splash dâhil) | 140-160 MB | 700-800 Mc |
| **Flame (Flutter)** | ~1.200 | ~0,8 sn | 90-110 MB | 600-700 Mc |
| **Flutter (saf)** | ~800 | ~0,75 sn | 80-100 MB | 420-500 Mc |
| **Godot** | Güvenilmez sonuç (yazar yapılandırma hatası olabileceğini belirtiyor) | ~1,5 sn | 150-170 MB | 800-900 Mc |

React Native + Skia ayrı bir kaynakta ölçülmüş (Samsung Galaxy A54): hafif yükte 120 FPS, orta yükte 117 FPS, ağır yükte (50 nesne + 300 mermi) 48 FPS. Cihazlar arası taban farkı ise dramatik: **giriş segmenti Android'de ~300 sprite, iPhone 12 mini'de ~15.000.**

> **Bu sayıları nasıl okumalı:** Her iki kaynak da ikincil, kontrollü laboratuvar ölçümü değil; yazarlar bunu açıkça söylüyor. Kesin rakam olarak değil, büyüklük mertebesi olarak alınmalı. Yine de yön nettir ve muhafazakâr bir bütçe kurmak için yeterlidir.

### 2.2 Bütünsel karşılaştırma

| Kriter | Unity 6 | Godot 4.6 | React Native + Skia | Flutter + Flame | Native (SpriteKit + Compose) |
|---|---|---|---|---|---|
| 2D oyun motoru olgunluğu | Tam | Tam | **Yok — kendiniz yazarsınız** | Kısmi | Platform başına ayrı |
| Ölçülen performans | En yüksek | İyi (4.6 ile stabil) | Giriş segmenti Android'de riskli | Orta-iyi | Yüksek |
| Açılış süresi | En yavaş (~2,9 sn) | ~1,5 sn | Hızlı | En hızlı | En hızlı |
| Uygulama boyutu | En büyük | Orta | Küçük | Küçük | Küçük |
| 2D animasyon hattı | Spine, Rive, yerleşik 2D Animation | Spine (eklenti), yerleşik | Rive | Rive, Spine (kısıtlı) | Yerleşik |
| Varlık/içerik sistemi | Addressables (olgun) | Resource/PCK | **Kendiniz kurarsınız** | Kısmi | Kendiniz kurarsınız |
| Abonelik | RevenueCat Unity SDK | StoreKit 2 + Play Billing (4.6, resmî) | RevenueCat | RevenueCat | Yerleşik |
| Kids/COPPA uyumu | **Dikkat: Analytics ve IAP tuzağı** | Doğal olarak temiz (SDK ekosistemi zaten yok) | Temiz | Temiz | En temiz |
| Kod OTA güncellemesi | Yok (içerik OTA var) | Yok | **Var (EAS Update)** | Yok | Yok |
| Türkiye'de yetenek | **Bol** | Az | Orta (grafik seviyesinde az) | Az | Orta |
| Lisans maliyeti | 200.000 $ altı ücretsiz; üstü 2.310 $/koltuk/yıl | Ücretsiz (MIT) | Ücretsiz | Ücretsiz | Ücretsiz |
| Kategoride kanıt | **Toca Boca, Sago Mini** | Brotato, Rift Riff, Kamaeru (premium) | Yok | Yok | Yok |

### 2.3 Elenenler ve neden

**Native (Swift/SpriteKit + Kotlin/Compose) — elendi.** Teknik olarak en temiz ve en hızlı sonuç, ama iki ayrı kod tabanı demek. Bu projede en pahalı iş kod değil, sanat varlıklarının entegrasyonu ve sahne kurgusu — onu iki kez yapmak kabul edilemez. Küçük bir ekip için ölümcül.

**Flutter + Flame — elendi.** 2D için makul ve düşük segment cihazlarda hafif olması gerçek bir avantaj. Ancak Godot'nun sunduğu her şeyi daha zayıf bir editör ve daha ince bir ekosistemle sunuyor; Godot varken tercih sebebi kalmıyor. Kategoride kanıtı da yok.

**React Native + Skia — önerilmiyor (ayrıntı aşağıda).**

---

## 3. React Native Neden Birinci Tercih Değil

Bu, ilk yönelimin tersine bir sonuç olduğu için gerekçesini açıkça yazmak gerekiyor.

React Native ile bu ürün **yapılabilir**. Yol şudur: `@shopify/react-native-skia` ile GPU üzerinde çizim, Reanimated 4 worklet'leri ile UI thread'de kare döngüsü, Gesture Handler ile girdi, Rive ile karakterler, `react-native-audio-api` ile ses. Bu yığın gerçek ve çalışıyor.

Sorun şu ki React Native ekosisteminde 2026 itibarıyla **bir oyun motoru yok**. Dört kısmi seçenek var: `react-native-game-engine` (2020'den beri bakımsız, her varlık bir View, ~50 varlıkta kare düşüyor), Phaser'ı WebView içinde çalıştırmak (Android WebView'da masaüstü tarayıcıya göre 5-10 kat yavaş ve React Native durumuyla hiç konuşmuyor), `expo-gl`/WebGPU (ham grafik yüzeyi — motorun tamamı size kalıyor) ve Skia + Reanimated. Sonuncusu tek ciddi yol ama o da **sadece bir render katmanı**: sprite sheet, tilemap, sahne yığını, ses senkronu, girdi soyutlaması ve varlık hattı yok.

Yani React Native'i seçmek, aslında **"bu ürün için küçük bir oyun motoru yazacağız"** demektir. Bunun üç bedeli var:

- **Zaman kod tarafına kayar.** Bu kategoride ürünü kazandıran şey sahne derinliği, karakter cazibesi ve üretim kalitesi. Motor altyapısına harcanan her ay, doğrudan içerikten çalınan bir aydır.
- **Yetenek riski.** Türkiye'de Unity mühendisi bol; Skia/worklet seviyesinde grafik yazabilen React Native mühendisi nadir. Tek kişiye bağımlılık (bus factor) gerçek bir risk hâline gelir.
- **Performans tabanı.** Giriş segmenti Android'de ölçülen ~300 sprite tavanı, hedef pazarın tam kalbinde bir sınır. Sahne tasarımını en baştan bu sınırın altına sıkıştırmak zorunda kalırsınız.

Buna karşılık React Native'in bu projede tek gerçek üstünlüğü var ve küçümsenmemeli: **EAS Update ile mağaza incelemesi olmadan kod güncellemesi.** Unity ve Godot'da içerik uzaktan güncellenebilir ama kod güncellemesi her zaman mağaza sürümü gerektirir. Bu, kritik bir hata düzeltmesinde 1-3 günlük fark demek. Yine de bu tek avantaj, motor yazma maliyetini karşılamıyor.

**Sonuç:** React Native, arkasında hâlihazırda güçlü bir React Native ekibi olan ve oyun tarafını basit tutmayı göze alan bir şirket için savunulabilir. Sıfırdan kurulan, kalite çıtası Toca Boca olan bir ürün için değil.

---

## 4. Karar: Unity 6 — ve Godot Ne Zaman Doğru Olur

### Unity 6 lehine

- Kategorinin kalite çıtasını belirleyen iki ürün de bu motorla yazılmış; taklit edilecek görsel dil ve etkileşim hissi bu motorda doğal olarak elde ediliyor.
- Ölçümlerde en yüksek varlık kapasitesi.
- Addressables ile olgun, uzaktan güncellenebilir içerik sistemi — 1. bölümdeki "üçüncü yılda ayda iki sahne" gereksiniminin doğrudan karşılığı.
- Spine, Rive, parçacık sistemleri, 2D ışık — sanat ekibinin tanıdığı araçların tamamı destekli.
- Türkiye'de yetenek havuzu; işe alım ve devretme riski en düşük.
- Lisans: yıllık toplam gelir + yatırım 200.000 $ altındaysa Unity Personal ücretsiz. Üstünde Pro gerekiyor: 12 Ocak 2026'dan itibaren koltuk başına yıllık 2.310 $ (aylık 210 $). 25 milyon $ üstünde Enterprise. Tartışmalı **Runtime Fee kalıcı olarak iptal edildi**, artık kurulum başına ücret yok. Unity 6 ile açılış logosu (splash) ücretsiz sürümde bile kapatılabiliyor — premium marka algısı için önemli bir ayrıntı.

### Unity aleyhine — ve nasıl yönetilir

| Sorun | Yönetimi |
|---|---|
| Açılış süresi ~2,9 sn (en yavaş) | Splash kapat, sahne yükleme asenkron, açılışta hafif bir "atölye kapısı" sahnesi ile algılanan süreyi kısalt |
| Uygulama boyutu en büyük | Engine code stripping + Managed Stripping "High", .NET Standard 2.0; çekirdek binary'de 1-2 sahne, gerisi Addressables ile CDN'den |
| Bellek kullanımı yüksek (140-160 MB) | Giriş segmenti cihazlarda bellek bütçesi tanımla, atlas çözünürlüklerini cihaz sınıfına göre ayır |
| **Kids kategorisi tuzakları** | 6. bölüm — bu en kritik olanı |

### Godot 4.6 ne zaman doğru olur

Godot'yu ciddiye almak gerekiyor, çünkü bu ürünün profili Godot'nun bilinen zayıflıklarının çoğunu ıskalıyor:

- Godot'nun en büyük eksiği olarak gösterilen şey, **analitik ve pazarlama SDK ekosisteminin yokluğu** (GameAnalytics, Amplitude, Adjust, AppsFlyer için bakımlı Godot bağlayıcısı yok). Bu üründe o SDK'ların hiçbiri zaten kullanılamaz — Apple Kids kategorisi yasaklıyor. Yani eksik, eksik değil.
- İkinci eksiği "canlı servis (live service) için uygun değil" — yılda 20+ etkinlik, kohort analizi, ücretli analitik gerektiren oyunlar. Bu ürün canlı servis değil, abonelikli premium bir oyuncak.
- 4.6 (Ocak 2026) ile **StoreKit 2 ve Google Play Billing artık Foundation tarafından bakılan resmî eklentiler**; eskiden bakımsız topluluk çözümlerine mahkûm olan alan kapandı.
- Çökme oranları 4.5.2 ve 4.6 düzeltmeleriyle ~%4'ten %1'in altına indi (Google Play'in görünürlük eşiği %1,09) — Mali ve Adreno GPU sürücü sorunları ve Vulkan hataları giderildi. Yani düşük segment Android tarafında ciddi bir iyileşme var.
- Lisans tamamen ücretsiz, gelir eşiği yok, koltuk ücreti yok.
- Uygulama boyutu ve açılış süresi Unity'den belirgin daha iyi.

Godot'yu ikinci sıraya düşüren tek belirleyici: **ekip.** Türkiye'de Godot ile üretim yapmış, iş çıkarmış ekip bulmak zor ve işe alım riski yüksek. Kurucu ekipte zaten Godot yetkinliği varsa bu argüman düşer ve Godot **tercih edilmesi gereken seçenek** hâline gelir — daha ucuz, daha hafif, uyumluluk açısından daha temiz.

**Karar kuralı:** Ekip Unity ile kurulabiliyorsa Unity. Kurulamıyorsa veya kurucu ekipte Godot deneyimi varsa Godot. React Native veya Flutter, ikisi de mümkün değilse bile doğru cevap değil — o durumda çözüm teknoloji değiştirmek değil, işe alımı çözmek.

---

## 5. Önerilen Mimari

Unity içinde tek uygulama, dört net katman:

```
┌──────────────────────────────────────────────────────────────┐
│  KABUK (Shell)                                                │
│  Açılış · Sahne seçimi · Ebeveyn Bölümü · Ödeme duvarı        │
│  Ayarlar · İçerik indirme · Yerel çocuk profilleri            │
│  Unity UI Toolkit · Ayrı Unity sahnesi · Oyun döngüsü hafif   │
└───────────────────────────┬──────────────────────────────────┘
                            │ SceneDescriptor (id, sürüm, varlık grubu)
                            ▼
┌──────────────────────────────────────────────────────────────┐
│  SAHNE ÇALIŞTIRICI (Scene Runtime)                            │
│  Sahneyi kod olarak değil, VERİ olarak yükler                 │
│  Nesneler · Etkileşim bölgeleri · Tetikleyiciler · Ses eşlemesi│
└───────────────────────────┬──────────────────────────────────┘
                            │
              ┌─────────────┴─────────────┐
              ▼                           ▼
┌──────────────────────────┐  ┌───────────────────────────────┐
│  MEKANİK MODÜLLERİ        │  │  ORTAK SERVİSLER              │
│  Kes · Yapıştır · Boya    │  │  Kayıt · Ses yöneticisi       │
│  İnşa et                  │  │  Yetki (entitlement)          │
│  Her biri bağımsız,       │  │  Anonim telemetri             │
│  sahneye takılabilir      │  │  İçerik indirici              │
└──────────────────────────┘  └───────────────────────────────┘
```

### Bu ayrımın üç faydası

**1. Sahne = veri, kod değil.** Her sahne bir manifest:

```
content/scenes/kitchen/
  manifest.json    → nesneler, konumlar, etkileşim bölgeleri,
                     hangi mekanik nerede aktif, ses eşlemesi
  atlas_*.png      → sprite atlasları (az sayıda doku, az çizim çağrısı)
  characters.riv   → Spine/Rive iskelet ve durum makineleri
  audio/           → efekt + ortam (dilden bağımsız)
  audio/<dil>/     → seslendirme (dile göre çözülür)
```

Manifest'te düz metin bulunmaz; başlıklar ve ebeveyn bölümü metinleri `content/i18n` altındaki kataloglarda anahtarla tutulur. Böylece yeni bir dil hiçbir sahneye dokunmadan eklenir.

Sahne çalıştırıcı bu manifesti yorumlar. Sonucu: **yeni sahne eklemek için C# yazmak gerekmez.** Tasarımcı ve sanatçı, mühendisi beklemeden içerik üretebilir. Bu, üçüncü yıldaki içerik hızını belirleyen tek mimari karardır.

**2. Mekanikler bağımsız modüller.** Kesme mekaniği mutfakta da atölyede de aynı kodla çalışır, sadece parametreleri değişir. Yeni bir mekanik eklemek, mevcut sahneleri bozmadan yapılır.

**3. Kabuk oyundan ayrık.** Ödeme duvarı, ebeveyn bölümü ve ayarlar, oyun döngüsünden bağımsız bir sahnede. Uyumluluk denetimi (ne zaman ebeveyn kapısı çıkar, ne zaman satın alma gösterilir) tek bir yerde toplanır ve denetlenebilir olur.

### Dört mekaniğin teknik karşılığı

**Kesme.** Şekil bölme, çizilen kesme çizgisinin poligon üzerinde boolean işlemi (difference) olarak uygulanmasıyla. Unity tarafında `Mesh` üzerinde poligon kesme veya hazır 2D kesme kütüphaneleri kullanılabilir; kesilen parça bağımsız bir nesne olarak yaşamaya devam eder. Sahte kesme animasyonu değil, gerçek geometri — çünkü çocuk aynı şekli tekrar tekrar farklı yerlerden kesmeyi dener ve sahte olan ilk denemede belli olur.

**Boyama.** `RenderTexture` üzerine kalıcı fırça darbeleri; belirli aralıklarla tek dokuya "pişirme" (bake), böylece binlerce darbe biriktiğinde performans düşmez. Kova/dolgu boyama, önceden hazırlanmış bölge maskeleri üzerinden — gerçek flood-fill'den hem çok daha ucuz hem de 4 yaşındaki bir çocuk için çok daha öngörülebilir.

**Yapıştırma / sürükle-bırak.** Yapışma (snap) bölgeleri ve yumuşak kilitlenme + haptik + ses üçlüsü. Basit ama üründe en çok tekrarlanacak etkileşim, dolayısıyla en çok cilalanması gereken yer.

**İnşa etme.** **Tam fizik motoru kullanmayın.** 2-6 yaş için gerçekçi fizik (devrilen, yuvarlanan bloklar) hem hayal kırıklığı yaratır hem de giriş segmenti Android'de en pahalı hesaptır. Izgara/yapışma tabanlı yerleştirme + karakteristik "oturma" animasyonu hem daha ucuz hem yaş grubu için daha tatmin edici. Gerçek fizik istenen özel sahnelerde cisim sayısı 30'un altında tutulmalı.

### Karakter animasyonu: Spine mi, Rive mı?

Bu, sanat ekibinin hızını doğrudan belirleyen seçim.

| | Spine | Rive | Unity 2D Animation |
|---|---|---|---|
| Güçlü yanı | Oyun endüstrisi standardı iskelet animasyonu, mesh deformasyon | **Görsel durum makinesi** — etkileşimli tepkiler kodsuz | Unity içinde kalır, ek araç yok |
| Zayıf yanı | Etkileşimli durum mantığı kodda yazılır | İskelet derinliği Spine kadar değil | Araç ve iş akışı en zayıfı |
| Lisans | Professional 369 $ (tek seferlik), Essential 69 $ | 0 $ lisans; ekip planları ~32 $/koltuk/ay | Ücretsiz |

**Öneri: ikisi birlikte.** Karakterlerin dokunmaya verdiği tepkiler (boşta / dokunuldu / mutlu / şaşkın) **Rive durum makineleriyle**; ana karakter animasyonları ve mesh deformasyon gerektiren işler **Spine ile**. Rive'ın kazandırdığı şey şu: animatör, "bu karaktere dokununca ne olacağını" mühendise anlatmak yerine doğrudan kendisi tanımlar. Duolingo'nun yaptığı da bu. Bir dijital oyuncakta yüzlerce böyle küçük tepki olacağı için, bu tek başına aylar kazandırır.

---

## 6. Uyumluluk Mimarisi — En Kritik Bölüm

Bu bir kontrol listesi değil; ilk gün verilmezse sonradan çok pahalıya mal olan mimari kararlar.

### 6.1 Unity'ye özgü tuzak: Analytics ve IAP

Apple'ın kuralı net: **Kids kategorisindeki uygulamalar üçüncü taraf analitik veya reklam içeremez; hiçbir kişisel veri veya cihaz bilgisi üçüncü taraflara gidemez.**

Unity'de bu üç somut soruna dönüşüyor:

1. **Unity Analytics** anonimleştirilmiş kullanıcı kimliği üretiyor ve IP adresi, reklam tanımlayıcıları ve cihaz tanımlayıcıları topluyor. Apple, Unity Analytics'in cihaz adı bilgisine erişmesi gerekçesiyle uygulamaları reddetmiş durumda. → **Projede hiç bulunmamalı.**
2. **Unity IAP paketi varsayılan olarak Analytics olmadan açılamıyor** ve Apple, pakette çağrılmayan/kullanılmayan kodun varlığından bile rahatsız oluyor. Bu, Kids kategorisinde bilinen bir engel. → **Unity IAP kullanılmayacak.**
3. **Donanım istatistikleri (HW Stats)** de ayrıca kapatılmalı; varsayılan açık gelir.

**Somut karar:** `com.unity.services.*` altındaki paketlerin tamamı projeden çıkarılır. Ödeme tarafında **RevenueCat'in Unity SDK'sı** kullanılır — RevenueCat StoreKit ve Google Play Billing'in üzerine bir sarmalayıcı, kendi başına analitik veya reklam servisi değil ve çok sayıda çocuk uygulamasında kullanılmış durumda. RevenueCat, geçmişte Kids kategorisi retleri üzerine iOS SDK'sında `ASIdentifierManager` ve `advertisingIdentifier` referanslarını kaldırdı ve AppTrackingTransparency çağrılarını gizledi — **bu yüzden güncel sürüm kullanmak şart, eski sürüm doğrudan ret sebebi.**

### 6.2 Telemetri: birinci taraf, anonim, toplu

Yasaklı olanlar: Firebase Analytics, Google Analytics, Amplitude, Mixpanel, AppsFlyer, Adjust, Branch, Meta SDK, tüm reklam ağları, Unity Analytics.

Yerine:
- Kendi endpoint'iniz ve kendi veritabanınız (ya da şirket içi kurulabilen bir çözüm).
- Hiçbir cihaz tanımlayıcısı yok: IDFA/AAID yok, `identifierForVendor` yok, parmak izi yok.
- Sadece toplu sayaçlar: "3 numaralı sahne açıldı", "boyama tamamlandı". Kim olduğu değil, kaç kez olduğu.
- Çökme raporlama: barındırılan servisler (Sentry vb.) cihaz bilgisi gönderdiği için riskli. Güvenli yol kendi sunucunuzda barındırılan bir kurulum ve tüm tanımlayıcıların temizlenmesi.

### 6.3 COPPA: en önemli tek karar — çocuk hesabı olmasın

COPPA'nın 2025 revizyonu 22 Nisan 2026'dan beri tam yürürlükte: biyometrik tanımlayıcılar artık kişisel veri, çocuk verisinin yapay zekâ eğitiminde kullanılması ayrı ve açık ebeveyn onayı gerektiriyor, doğrulanmış ebeveyn onayı için sekiz yöntem tanımlı, ihlal başına ceza 53.088 $'a kadar.

Çocuktan hiçbir kişisel veri toplanmazsa, doğrulanmış ebeveyn onayı yükümlülüğünün büyük kısmı **hiç doğmaz**. Pratikte:

- Kayıt yok, giriş yok, e-posta yok, isim yok, doğum tarihi yok. Sadece kimliğe bağlanmayan yaş **bandı** seçimi.
- Mikrofon ve kamera erişimi yok (biyometrik risk).
- Çocuğun eserleri **cihazda kalır.** Bulut yedekleme cazip görünür ama içerik toplamaktır; v1'de yapılmamalı.
- Paylaşım ("resmini gönder") ebeveyn kapısının arkasında ve cihazın kendi paylaşım sayfası üzerinden — sizin sunucunuza hiçbir şey gitmez.

Bu yaklaşım aynı zamanda backend'i neredeyse tamamen ortadan kaldırıyor: hem hukuki hem mühendislik kazancı.

### 6.4 Ebeveyn kapısı

Uygulamadan çıkan bağlantılar, izin istekleri ve satın alma teklifleri ebeveyn kapısının arkasında olmak zorunda.

Yaygın hata: "üç sayısına dokun" gibi 5 yaşındaki bir çocuğun çözebileceği kapılar — hem reddedilir hem işlevsizdir. Doğru desen okuma-yazma ve çok adım gerektirir: "Bu yılın kaç olduğunu yazın" veya "İki noktayı aynı anda basılı tutup birleştirin" + zamanlayıcı. Kapı ekranında hiçbir ödül, hiçbir animasyon olmamalı — çocuk için sıkıcı olmalı.

### 6.5 Karanlık desen yasağı — teknik karşılığı

Bedeli ağır (FTC'nin Epic Games'e 520 milyon $ cezası). Uygulanması gerekenler:

- **Geri dönüş bildirimi yok.** Bildirim altyapısı sadece ebeveyne ve sadece ebeveyn bölümünden açılabilir olmalı.
- **Çıkışta duygusal manipülasyon yok.** "Çıkmak istediğine emin misin?" ekranı olmasın; çıkış anında çalışsın.
- **Sayaç, seri (streak), günlük ödül yok.**
- Oturum uzunluğu birincil KPI olmamalı. "Ekranda geçirilen süre" bu üründe başarı değil, uyarı işareti.

### 6.6 Mağaza programları

- Apple: App Store Connect'te "Made for Kids" ve yaş bandı (5 ve altı) seçilir.
- Google Play: Aileler politikası kapsamında hedef kitle **yayından önce** beyan edilir.
- **Teacher Approved** (Google Play): öğretmen ve çocuk gelişimi uzmanlarından oluşan jüri değerlendirmesi. İsteğe bağlı ama başvurulmalı — rozet, ebeveyn güveni için ücretsiz pazarlama değeri taşıyor.

---

## 7. Abonelik Kurgusu

"IAP kapalı" ifadesi teknik olarak şu demek: tüketilebilir ve içerik açan IAP yok. Otomatik yenilenen abonelik mağaza terminolojisinde teknik olarak bir IAP'tir ve gereklidir.

- Tek abonelik grubu, iki plan: **aylık + yıllık** (yıllık belirgin indirimli).
- Rakip analizindeki kanıtlanmış bant **7-10 $/ay** (PAW Patrol Academy 7,99 $/ay – 49,99 $/yıl; Pango Kids aylık/yıllık/ömür boyu).
- **Türkiye fiyatı ayrı belirlenmeli.** Küresel 7,99 $ Türkiye için doğru fiyat değil; mağazaların ülke bazlı fiyat kademeleri kullanılmalı. RevenueCat üzerinden yönetilebilir bir yapılandırma kararı, mühendislik işi değil.
- **Aile Paylaşımı açık** — hedef kitle tam olarak aile.
- Deneme süresi rakiplerde 3-7 gün. Deneme sonu bildirimi manipülatif dil içermemeli.
- Ödeme akışının tamamı ebeveyn kapısının arkasında.
- Yerel çoklu çocuk profili (aynı cihazda kardeşler) — hesap değil, sadece cihazda tutulan tercih seti.

---

## 8. Veri, İçerik ve Dağıtım

### Yerel kayıt (local-first)

Çocuğun eserleri, sahne durumu ve tercihler tamamen cihazda. **Her etkileşim bitiminde kalıcılaştırılmalı; "kaydet" düğmesi diye bir şey olmamalı.** Uygulama her an arka plana atılabilir (telefon çalar, ebeveyn cihazı alır) ve 4 yaşındaki bir çocuk için kayıp iş, ürünün terk edilme sebebidir.

### İçerik dağıtımı

**Unity Addressables + kendi CDN'iniz.** Uygulama küçük bir çekirdekle (1-2 sahne) kurulur; diğer sahneler ebeveynin göreceği bir "İndir" akışıyla iner.

> **Neden bu önemli:** Türkiye'deki hedef cihaz profilinde depolama sınırlı ve veri paketi maliyetli. 800 MB'lık bir ilk indirme, kurulum hunisinde ciddi kayıp demek. App Store'un hücresel indirme sınırı da hesaba katılmalı.

Unity Cloud Content Delivery yerine **kendi CDN'inizi kullanın** — amaç, Unity Gaming Services paketlerinin projeye hiç girmemesi (6.1'deki tuzak).

> **Sınır:** Addressables ile **içerik** uzaktan güncellenir, **kod** güncellenmez. Kritik bir hata düzeltmesi her zaman mağaza sürümü gerektirir. Bu, React Native'in EAS Update'e karşı elindeki tek gerçek üstünlüktü ve Unity'yi seçerken bilinçli olarak kabul ediliyor. Karşı önlem: sağlam bir uzaktan yapılandırma (remote config) katmanı — bozulan bir özelliği kod göndermeden kapatabilmek.

### Backend: mümkün olduğunca az

| Bileşen | Ne yapar |
|---|---|
| RevenueCat | Abonelik yetkisi, deneme, iade, çapraz platform |
| Küçük bir API | Sahne kataloğu, CDN imzalı URL'leri, uzaktan yapılandırma |
| Anonim telemetri toplayıcı | Toplu olay sayaçları, cihaz kimliği yok |
| Nesne depolama + CDN | Sahne paketleri |

Teknoloji açısından sıkıcı olan doğru. Bu katman ürünün farklılaşma noktası değil; oraya mühendislik harcamayın.

---

## 9. Riskler

| Risk | Olasılık | Azaltma |
|---|---|---|
| **Giriş segmenti Android performansı** | Yüksek | İlk günden bir referans cihaz ilan edin (Redmi/Oppo giriş modeli). Sahne başına kare süresi bütçesi tanımlayın ve CI'da ölçün. "iPhone'da akıcı" bir kabul kriteri değildir. |
| **Kids kategorisi reddi** | Yüksek | Bağımlılık listesini bir uyumluluk belgesi gibi yönetin. Her yeni paket için "ne topluyor, nereye gönderiyor" sorusu. UGS paketlerinin projeye girmediğini CI'da kontrol edin. |
| **İçerik üretim hızı** | Yüksek | Veri odaklı sahne manifesti + Rive durum makineleri. Bunlar tam olarak bu riski azaltmak için var. |
| **Uygulama boyutu / ilk indirme** | Orta | Çekirdek binary küçük, sahneler Addressables ile talebe bağlı. |
| Kod OTA güncellemesi yokluğu | Orta | Güçlü uzaktan yapılandırma + özellik bayrakları (feature flag). |
| Unity lisans maliyeti | Düşük | 200.000 $ gelir eşiğine kadar ücretsiz; o noktada koltuk maliyeti zaten karşılanabilir. |
| Anahtar kişiye bağımlılık | Orta | Unity seçimi bunu zaten azaltıyor (yetenek havuzu geniş). |

---

## 10. Ekip ve Aşamalandırma

### MVP kapsamı

Önceki rapor "tek yaş bandı, tek beceri, 10-20 aktivite" öneriyordu. Teknik karşılığı:

- **3 sahne** (ev / atölye / mutfak), her biri derin ve keşfedilebilir
- **4 mekanik** (kes, yapıştır, boya, inşa et), sahnelere yayılmış ~15 aktivite
- **1 karakter ailesi**, Rive durum makineleriyle tepkili
- Kabuk tam çalışır: ebeveyn bölümü, kapı, abonelik, indirme
- Türkçe birinci dil; metin yok denecek kadar az, seslendirme esas

### Ekip

| Rol | Yük |
|---|---|
| Unity mühendisi (sahne çalıştırıcı, mekanikler) | 1 tam |
| Unity mühendisi (kabuk, abonelik, altyapı, uyumluluk) | 1 tam |
| Oyun/etkileşim tasarımcısı | 1 tam |
| İllüstratör + animatör (Spine/Rive) | 1 tam — **uzun vadede darboğaz** |
| Ses tasarımcısı | Yarı zamanlı |
| Çocuk gelişimi uzmanı / pedagog | Danışman |

Önceki rapor bunu ayrıca vurguluyordu: ekipte gerçek bir çocuk gelişimi uzmanı olması hem Apple/Google hem ebeveynler için aranan bir sinyal ve Teacher Approved başvurusunda somut fayda sağlıyor.

### Sıralama

1. **Teknik doğrulama (spike) — 2-3 hafta, kod atılacak.** Tek sahne, tek mekanik (kesme), referans giriş segmenti Android cihazda kare süresi ve bellek ölçümü. Ayrıca **boş bir Unity projesinin UGS'siz, RevenueCat'li hâliyle TestFlight'a çıkması** — uyumluluk yolunun açık olduğunu ilk haftalarda kanıtlar, altıncı ayda değil.
2. Sahne çalıştırıcı + manifest formatı.
3. Dört mekaniğin çekirdek uygulaması.
4. Kabuk: ebeveyn bölümü, kapı, abonelik, indirme yöneticisi.
5. İçerik üretimi (2. adımdan itibaren paralel).
6. Uyumluluk denetimi, mağaza beyanları, Teacher Approved başvurusu.

**1. adımı atlamayın.** Bu raporun tüm performans öngörüleri ikincil kaynaklardan geliyor; kendi sahnenizle kendi hedef cihazınızda ölçtüğünüz tek bir sayı, buradaki tüm tabloların toplamından değerlidir.

---

## 11. Özet Karar Tablosu

| Karar | Seçim |
|---|---|
| Motor | **Unity 6** (2D). Ekip Unity ile kurulamıyorsa Godot 4.6. |
| Dil | C# |
| Mimari | Kabuk / Sahne çalıştırıcı / Mekanik modülleri / Ortak servisler |
| Sahne modeli | **Veri odaklı manifest** — yeni sahne kod gerektirmez |
| Karakter animasyonu | Rive (etkileşimli durum makineleri) + Spine (iskelet/mesh) |
| Fizik | Yok / minimal; ızgara ve yapışma tabanlı yerleştirme |
| Abonelik | **RevenueCat Unity SDK.** Unity IAP kullanılmayacak. |
| Analitik | **Birinci taraf, anonim, toplu.** `com.unity.services.*` projede yok. |
| Çökme raporu | Kendi sunucunuzda, tanımlayıcılar temizlenmiş |
| Hesap | **Yok.** Çocuk verisi toplanmıyor. |
| Kayıt | Cihazda; her etkileşim sonunda otomatik |
| İçerik dağıtımı | Unity Addressables + kendi CDN'iniz (Unity CCD değil) |
| Fiyat | 7-10 $/ay bandı; **TR fiyatı ayrı kademede** |
| Referans cihaz | **Giriş segmenti Android** — iPhone değil |
| Kabul kriteri | Referans cihazda sahne başına kare süresi bütçesi, CI'da ölçülür |

---

## Kaynaklar

**Motor karşılaştırması ve ölçümler**
- Benchmarking Flutter, Flame, Unity and Godot — Filip Hracek — https://filiph.net/text/benchmarking-flutter-flame-unity-godot.html
- The React Native game engine gap in 2026 — https://grzegorzotto.dev/blog/the-react-native-game-engine-gap
- Best mobile game engines in 2026: Unity, Unreal, Godot & more — https://appradar.com/blog/mobile-game-engines-development-platforms
- Best Mobile Game Engines in 2026: Studio-Tested Comparison — https://sunstrikestudios.com/en/blog/the_best_mobile_game_engines_in_2025/
- Flutter Flame — 2026 Guide & Unity Comparison — https://dianapps.com/blog/what-is-flutter-flame/

**Unity**
- How Toca Boca Built a High-performance Scalable Rendering Backend — Unity — https://unity.com/blog/how-toca-boca-built-a-high-performance-scalable-rendering-backend
- Toca Life Series done in Unity — Unity Forum — https://forum.unity.com/threads/toca-life-series-by-toca-boca-done-in-unity.488438/
- Unity Developer @ Toca Boca — https://gamejobs.co/Unity-Developer-at-Toca-Boca
- Unity Game Developer @ Sago Mini — https://gamejobs.co/Unity-Game-Developer-at-Sago-Mini-7771
- Unity Pricing Updates — https://unity.com/products/pricing-updates
- Unity Announces Its Upcoming 2026 Price Changes — 80.lv — https://80.lv/articles/unity-announces-its-upcoming-2026-price-changes
- Unity scraps Runtime Fee but raises prices — CG Channel — https://www.cgchannel.com/2024/09/unity-scraps-controversial-runtime-fee-but-raises-prices/
- Optimizing Unity's Build Size for Mobile Games — https://busybytes.com/blog/optimize-build-size-mobile-games-unity/

**Unity + Kids kategorisi uyumluluk tuzakları**
- Unity Manual — COPPA Compliance — https://docs.unity3d.com/2022.1/Documentation/Manual/UnityAnalyticsCOPPA.html
- Unity Support — Google Designed for Families / Apple Kids Category gereksinimleri — https://support.unity.com/hc/en-us/articles/14315043608212
- Unity Discussions — Kids' app rejected by Apple because of Analytics — https://discussions.unity.com/t/kids-app-is-rejected-by-apple-because-of-analytics/774297
- Unity Discussions — Unity IAP unsupported for Kids Apps on iOS — https://discussions.unity.com/t/unity-iap-unsupported-for-kids-apps-on-ios-at-the-moment/798281
- Unity Discussions — App for Kids: how to totally disable HW stats and Analytics — https://discussions.unity.com/t/app-for-kids-how-to-totally-disable-hw-stats-and-analytics/892637
- Unity Discussions — Our Experience w/ an Apple Kids App — https://discussions.unity.com/t/our-experience-w-an-apple-kids-app/806150

**Godot**
- Godot Mobile in 2026: Ready for Premium, Not Live Service — Ziva — https://ziva.sh/blogs/godot-mobile
- Godot Mobile update, April 2026 — Godot Engine — https://godotengine.org/article/godot-mobile-update-apr-2026/
- Godot Engine Review: Ready for Pro-Level Mobile? — https://genieee.com/godot-engine-review-is-it-ready-for-pro-level-mobile-game-development/

**Animasyon hattı**
- Unity 2D Animation vs Spine — RetroStyle Games — https://retrostylegames.com/blog/unity-2d-animation-vs-spine/
- Rive Pricing 2026 — https://www.spotsaas.com/product/rive/pricing
- Rive — resmî site ve runtime'lar — https://rive.app/

**Abonelik**
- RevenueCat — Unity SDK kurulumu — https://www.revenuecat.com/docs/getting-started/installation/unity
- RevenueCat — Unity in-app purchases — https://www.revenuecat.com/platform/unity-in-app-purchases/
- RevenueCat — Kids Category App Review soruları — https://community.revenuecat.com/sdks-51/how-should-i-answer-app-review-questions-about-the-kids-category-3041
- RevenueCat — AdSupport kaynaklı Kids Category reddi — https://community.revenuecat.com/sdks-51/we-rejected-depends-on-adsupport-guideline-1-3-safety-kids-category-729
- RevenueCat — Çocuk uygulamalarında monetizasyon — https://www.revenuecat.com/blog/growth/whats-the-best-way-to-monetize-kids-apps/

**Mağaza kuralları ve mevzuat**
- Apple — App Updates in the Kids Category — https://developer.apple.com/news/?id=091202019a
- Apple — App Review Guidelines — https://developer.apple.com/app-store/review/guidelines/
- Kids Category Data Restrictions — https://conductatlas.com/platform/apple/apple-app-store-review-guidelines/kids-category-data-restrictions/
- Apple Developer Forums — Third party analytics in the Kids Category — https://developer.apple.com/forums/thread/131840
- Google Play — Families Policies — https://support.google.com/googleplay/android-developer/answer/9893335
- Android Developers — Teacher Approved programı — https://android-developers.googleblog.com/2022/11/helping-kids-and-families-find-high-quality-apps-for-kids.html
- FTC — Kids' Privacy (COPPA) — https://www.ftc.gov/news-events/topics/protecting-consumer-privacy-security/kids-privacy-coppa
- COPPA Compliance 2025 Practical Guide — https://blog.promise.legal/startup-central/coppa-compliance-in-2025-a-practical-guide-for-tech-edtech-and-kids-apps/

**Türkiye pazarı ve yetenek**
- Kasım 2025: Türkiye'de En Çok Kullanılan İşletim Sistemleri — Webtekno — https://www.webtekno.com/kasim-2025-turkiye-en-cok-kullanilan-isletim-sistemleri-h206371.html
- Türkiye'de çalışan sayısına göre en büyük oyun şirketleri — Mobidictum — https://mobidictum.com/tr/turkiye-en-buyuk-oyun-sirketleri-2025/
- Unicorn olma potansiyeli taşıyan en yeni yerli oyun şirketleri — Para Dergi — https://www.paradergi.com.tr/girisimcilik/2026/06/22/unicorn-olma-potansiyeli-tasiyan-en-yeni-yerli-oyun-sirketleri
- Türkiye ve Dünyada Unity Developer Maaşları 2026 — https://www.ucuncubinyil.com/turkiye-ve-dunyada-unity-developer-maaslari-2026-guncel-veriler

**Referans: React Native yığını (kayıt için)**
- React Native Skia — https://shopify.github.io/react-native-skia/
- Reanimated — https://docs.swmansion.com/react-native-reanimated/
- Expo SDK 55 changelog — https://expo.dev/changelog/sdk-55
