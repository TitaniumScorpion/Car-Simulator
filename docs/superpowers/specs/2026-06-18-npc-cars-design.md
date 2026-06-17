# NPC Araba Sistemi — Tasarım Dokümanı

**Tarih:** 2026-06-18  
**Proje:** Car Simulator (Serious Game)  
**Teslim:** 2 gün  

---

## Genel Bakış

Oyuncunun WheelCollider tabanlı araba ile sürdüğü serious game'e NPC trafik araçları eklenir. NPC'ler gerçek trafik akışı oluşturur, trafik kurallarına uyar ve oyuncuyu defansif sürüş açısından test eder. Sistem Unity NavMesh + WheelCollider kombinasyonu kullanır.

---

## Eğitim Amaçları

- **Trafik kuralları:** NPC'ler kurallara uyar; oyuncu uymazsa çarpışma senaryoları oluşur
- **Defansif sürüş:** NPC'ler oyuncuyu fark edip yavaşlar, farkındalık testi sağlar
- **Trafik akışı:** Gerçekçi trafik ortamında görev tamamlama deneyimi

---

## Mimari

### Bileşenler

| Bileşen | Dosya | Açıklama |
|---|---|---|
| NPC Controller | `Assets/Scripts/NpcCarController.cs` | NavMesh → WheelCollider çevirici, orta zeka |
| Traffic Manager | `Assets/Scripts/NpcTrafficManager.cs` | NPC spawn, hedef atama, döngüsel trafik |
| NPC Prefab | `Assets/Prefabs/NpcCar.prefab` | WheelCollider + NavMeshAgent hazır prefab |

### Mevcut Dosyalar (Değişmez)

- `Assets/Scripts/CarController.cs` — oyuncu arabası, dokunulmaz

---

## NPC Car Prefab Hiyerarşisi

```
NpcCar (GameObject)
├── NavMeshAgent         (Component — updatePosition=false, updateRotation=false)
├── Rigidbody            (Component — mass: ~1000kg, drag: 0.05)
├── NpcCarController     (Component)
├── CarModel             (Tofaş mesh veya placeholder)
│   ├── Body
│   └── Wheels (visual transforms)
├── WheelColliders
│   ├── FrontLeftWheelCollider
│   ├── FrontRightWheelCollider
│   ├── RearLeftWheelCollider
│   └── RearRightWheelCollider
└── ForwardRaycastPoint  (önden raycast çıkış noktası, z+1.5m)
```

---

## Veri Akışı

### NpcCarController — FixedUpdate Döngüsü

```
1. NavMeshAgent.steeringTarget → hedef yön vektörü hesapla
2. transform.forward ile açı → steering açısı (maxSteeringAngle ile kısıtlı)
3. WheelCollider.steerAngle'a yaz (ön tekerlekler)

4. İstenen hız (targetSpeed) → mevcut hız karşılaştır
5. Farka göre motor torque veya brake torque hesapla
6. WheelCollider.motorTorque → arka tekerlekler (RWD, AI için daha stabil)
   WheelCollider.brakeTorque → dört tekerlek

7. agent.nextPosition = transform.position  ← NavMesh'i her frame güncelle
```

### NpcTrafficManager — Yaşam Döngüsü

```
Awake()   → NPC listesini başlat
Start()   → Her NPC için ilk hedef noktasını ata (NavMesh.SamplePosition)
Update()  → Her NPC hedefe ulaştı mı kontrol et
          → Ulaştıysa yeni rastgele hedef ata
```

---

## Orta Zeka Katmanları

### Katman 1: Mesafe Koruma
- `ForwardRaycastPoint`'ten ileriye `Physics.Raycast` atar
- Algılama mesafesi: **15m** (Inspector'dan ayarlanabilir)
- Durma mesafesi: **3m**
- Önünde araç/oyuncu varsa → `targetSpeed` kademeli azaltılır
- Durma mesafesindeyse → tam fren uygulanır

### Katman 2: Oyuncu Farkındalığı
- Her frame `Vector3.Distance(transform.position, playerCar.position)` kontrol edilir
- Oyuncu **10m** yakınındaysa → `targetSpeed *= 0.7f` (defansif yavaşlama)
- Oyuncu uzaklaşınca → normale döner

---

## NavMesh Kurulumu

- Paket: **Unity AI Navigation** (Package Manager'dan eklenir)
- `NavMeshSurface` component'i sahne düzlemine eklenir
- Bake parametreleri: Agent Radius **0.5**, Agent Height **1.5**, Max Slope **10°**
- Yol/şehir modeli eklenince sadece **Bake** butonu — script değişmez

---

## Konfigürasyon (Inspector)

### NpcCarController
```
motorForce        = 800f      // oyuncudan daha az güçlü
maxSteeringAngle  = 30f       // oyuncuyla aynı
brakeForce        = 2000f
targetSpeed       = 8f        // m/s (~29 km/h)
detectionRange    = 15f       // raycast mesafesi
stopDistance      = 3f        // durma mesafesi
playerDetectRange = 10f       // oyuncu farkındalık mesafesi
```

### NpcTrafficManager
```
npcPrefab         // NpcCar prefab referansı
spawnPoints[]     // başlangıç spawn noktaları (Transform[])
npcCount          = 3         // başlangıç değeri, artırılabilir
waypointRadius    = 5f        // NavMesh.SamplePosition arama yarıçapı
```

---

## Ölçeklenebilirlik

- `npcCount` Inspector'dan artırılır, başka değişiklik gerekmez
- Yol/şehir eklenince NavMesh rebake yeterli
- İleride object pooling için `NpcTrafficManager.SpawnNpc()` metodu ayrı tutulur
- Trafik ışığı entegrasyonu için `NpcCarController` public `ForceStop(bool)` metodu içerir (şimdi boş, ileriye dönük)

---

## Kapsam Dışı (Bu Sprint)

- Trafik ışığı sistemi
- Şerit değiştirme
- NPC'ler arası iletişim
- Ses efektleri
- Kaza senaryosu tetikleyicileri
