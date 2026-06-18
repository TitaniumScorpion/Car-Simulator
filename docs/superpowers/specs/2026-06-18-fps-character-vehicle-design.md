# FPS Karakter + Araç Binme/İnme Sistemi

**Tarih:** 2026-06-18  
**Proje:** Car Simulator (Unity, URP)

---

## Genel Bakış

Oyuncu sahnede FPS perspektifinden yürür. Oyuncu arabasına (CarController) yaklaşıp E tuşuna bastığında TPS kameraya geçiş yaparak arabayı sürer. Tekrar E'ye basınca araçtan iner ve FPS moduna geri döner.

---

## Bileşenler

### Yeni Scriptler

| Script | Sorumluluk |
|--------|-----------|
| `GameManager.cs` | OnFoot / InCar mod geçişi; `OnEnterCar` / `OnExitCar` olayları |
| `FpsController.cs` | Yürüme, zıplama, fare bakışı, yakın araç tespiti |
| `CameraManager.cs` | FPS ↔ TPS kamera lerp geçişi |
| `VehicleInteraction.cs` | Araç üzerinde binme trigger alanı; enter noktası ve exit spawn noktası tanımlar |

### Mevcut Scriptlerde Değişiklik

- `CarController.cs` — değişiklik yok; `enabled = false/true` ile yönetilir.

---

## Durum Makinesi

```
enum GameState { OnFoot, InCar }
```

### OnFoot
- `FpsController` aktif
- `CarController` devre dışı
- FPS kamera aktif, cursor kilitli
- E tuşu: yakın araç varsa → `EnterCar()`

### InCar
- `FpsController` devre dışı
- `CarController` aktif
- TPS kamera aktif, araba yönüne smooth lerp
- Karakter objesi arabanın child'ı (konumunu takip eder)
- E tuşu → `ExitCar()`

---

## Olay Akışı

### EnterCar()
1. `FpsController.enabled = false`
2. `CarController.enabled = true`
3. Karakter transform → arabanın child'ı, `localPosition = (0,0,0)`
4. `CameraManager.SwitchToTPS()` — coroutine başlar
5. `GameManager.state = InCar`

### ExitCar()
1. `CarController.enabled = false`
2. Karakter transform → sahne root'una çıkarılır, sol kapı spawn noktasına yerleştirilir (`VehicleInteraction.exitPoint`)
3. `CameraManager.SwitchToFPS()` — coroutine başlar
4. `FpsController.enabled = true`
5. `GameManager.state = OnFoot`

---

## FPS Controller

```
FpsController
  ├── CharacterController (Unity component)
  ├── Camera (child, gözler hizasında)
  └── Script: FpsController.cs
```

- **Hareket:** `Move` action → `CharacterController.Move()`; yatay düzlem
- **Bakış:** `Look` action → Mouse X: karakter Y rotasyonu; Mouse Y: kamera X rotasyonu, ±80° clamp
- **Zıplama:** `Jump` action → `CharacterController` üzerinde dikey velocity
- **Koşma:** `Sprint` action → `moveSpeed * sprintMultiplier`
- **Araç tespiti:** Her frame `Physics.OverlapSphere(transform.position, interactRange)` — `VehicleInteraction` component'i olan collider aranır; bulunursa UI prompt gösterilir, E basılırsa `EnterCar()`
- **Cursor:** `OnFoot` → `Locked`; `InCar` → değişmez (zaten kilitli)

### Inspector Parametreleri
| Parametre | Varsayılan |
|-----------|-----------|
| `moveSpeed` | 5f |
| `sprintMultiplier` | 1.8f |
| `jumpHeight` | 1.2f |
| `gravity` | -9.81f |
| `interactRange` | 3f |
| `mouseSensitivity` | 2f |

---

## Camera Manager

Sahnede tek bir Main Camera vardır ve her zaman `CameraManager` tarafından yönetilir — hiçbir zaman başka bir objenin child'ı değildir.

### FPS Modu
- `FpsController` üzerinde görünmez bir `fpsHead` Transform child'ı (gözler hizasında) bulunur.
- Her frame: `camera.position = fpsHead.position`, `camera.rotation = fpsHead.rotation`.

### TPS Modu
- Kamera arabanın child'ı **değil**; her frame pozisyon hesaplanır:
  ```
  targetPos = car.position + car.rotation * offset   // offset: (0, 3, -6)
  targetRot = Quaternion.LookRotation(car.forward, Vector3.up)
  camera.position = Vector3.Lerp(camera.position, targetPos, followSpeed * dt)
  camera.rotation = Quaternion.Lerp(camera.rotation, targetRot, rotateSpeed * dt)
  ```

### Geçiş (Coroutine)
- `transitionDuration = 0.5f`
- FPS → TPS: kamera FPS pozisyonundan TPS hedefine `Vector3.Lerp` + `Quaternion.Lerp`
- TPS → FPS: kamera TPS pozisyonundan FPS pozisyonuna lerp
- Geçiş bitince kesin pozisyona oturt

### Inspector Parametreleri
| Parametre | Varsayılan |
|-----------|-----------|
| `tpsOffset` | (0, 3, -6) |
| `followSpeed` | 5f |
| `rotateSpeed` | 5f |
| `transitionDuration` | 0.5f |

---

## Vehicle Interaction

`VehicleInteraction.cs` araç prefab'ına eklenir:

```
PlayerCar
  └── VehicleInteraction.cs
        ├── exitPoint: Transform   // sol kapı yanı spawn noktası
        └── Collider (trigger, ~3m yarıçap)  // yakınlık tespiti için
```

- `exitPoint`: Inspector'dan araç prefab'ında konumlandırılan boş GameObject (sol kapı hizası)

---

## Input Mapping (Mevcut)

Mevcut `InputSystem_Actions.inputactions` kullanılır, yeni binding eklenmez:

| Eylem | OnFoot | InCar |
|-------|--------|-------|
| `Move` | Yürüme | CarController zaten okur |
| `Look` | FPS bakış | Kullanılmaz |
| `Jump` | Zıplama | Kullanılmaz |
| `Sprint` | Koşma | Kullanılmaz |
| `Interact` (E) | Araca binme | Araçtan inme |

---

## Sahne Kurulumu

### Player GameObject
```
Player (tag: "Player")
  ├── CharacterController
  ├── FpsController.cs
  └── FPSHead (child Transform, gözler hizasında — kamera referansı)
```

### Camera GameObject (bağımsız, sahne root'unda)
```
MainCamera (tag: "MainCamera")
  ├── Camera (Main Camera)
  └── CameraManager.cs
```

### PlayerCar GameObject
```
PlayerCar (tag: "PlayerCar")   ← yeni tag, NPC çakışmasını önler
  ├── CarController.cs  (başlangıçta disabled)
  └── VehicleInteraction.cs
        └── ExitPoint (boş child Transform)
```

### GameManager GameObject
```
GameManager
  └── GameManager.cs
        └── CameraManager.cs (aynı objede ya da child)
```

---

## NpcCarController Tag Güncellemesi

`NpcCarController.cs`'deki `GameObject.FindWithTag("Player")` çağrısı `"PlayerCar"` olarak değiştirilecek; NPC araçlar FPS karakteri değil, oyuncu arabasını engel olarak tanısın.

---

## Kapsam Dışı

- NPC araçlara binme
- Karakter animasyonları
- Araç içi kamera (sürücü bakış açısı)
- Çoklu araç desteği
