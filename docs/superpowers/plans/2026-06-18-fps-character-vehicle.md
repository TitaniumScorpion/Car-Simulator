# FPS Character + Vehicle Enter/Exit System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** FPS perspektifinde yürüyen oyuncu, player arabasına yaklaşıp E'ye basınca TPS kameraya geçer ve arabayı sürer; tekrar E ile iner ve FPS moduna döner.

**Architecture:** 4 yeni script (VehicleInteraction, GameManager, FpsController, CameraManager) + NpcCarController'da 1 satır tag değişikliği. GameManager singleton, OnFoot/InCar durum makinesini yönetir ve FpsController/CarController'ı enable/disable eder. CameraManager her zaman Main Camera'ya sahiptir — FPS modunda `fpsHead` transform'unu her frame kopyalar, TPS modunda araba arkasındaki offset'e smooth lerp yapar.

**Tech Stack:** Unity URP, C#, Unity Input System (`Keyboard.current`, `Mouse.current`, `Gamepad.current`)

## Global Constraints

- Unity URP projesi — Cinemachine kullanılmaz
- Input: `Keyboard.current` / `Mouse.current` / `Gamepad.current` direkt kullanımı (CarController.cs ile tutarlı)
- `CarController.cs` değiştirilmez — sadece `enabled` toggle edilir
- E tuşu / Gamepad Y (buttonNorth) = Interact
- Player car tag: `"PlayerCar"` | FPS karakter tag: `"Player"`

---

### Task 1: VehicleInteraction

**Files:**
- Create: `Assets/Scripts/VehicleInteraction.cs`

**Interfaces:**
- Produces: `VehicleInteraction` component; `public Transform exitPoint`

- [ ] **Step 1: VehicleInteraction.cs oluştur**

```csharp
using UnityEngine;

public class VehicleInteraction : MonoBehaviour
{
    public Transform exitPoint;
}
```

- [ ] **Step 2: Unity Console'da derleme hatası yok — doğrula**

Unity Editor'ü aç (veya zaten açıksa kaydet). Console'da `VehicleInteraction` ile ilgili hata olmamalı.

---

### Task 2: GameManager

**Files:**
- Create: `Assets/Scripts/GameManager.cs`

**Interfaces:**
- Consumes: `FpsController` (enable/disable + transform), `CarController` (enable/disable + transform), `CameraManager.SwitchToTPS()`, `CameraManager.SwitchToFPS()`, `VehicleInteraction.exitPoint`
- Produces: `GameManager.Instance`, `GameManager.Instance.EnterCar()`, `GameManager.Instance.ExitCar()`, `GameManager.State`

- [ ] **Step 1: GameManager.cs oluştur**

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { OnFoot, InCar }
    public GameState State { get; private set; } = GameState.OnFoot;

    [Header("References")]
    public FpsController fpsController;
    public CarController carController;
    public CameraManager cameraManager;
    public VehicleInteraction vehicleInteraction;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (State != GameState.InCar) return;
        bool ePressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        bool yPressed = Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame;
        if (ePressed || yPressed)
            ExitCar();
    }

    public void EnterCar()
    {
        if (State == GameState.InCar) return;
        State = GameState.InCar;
        fpsController.enabled = false;
        carController.enabled = true;
        fpsController.transform.SetParent(carController.transform);
        fpsController.transform.localPosition = Vector3.zero;
        cameraManager.SwitchToTPS();
    }

    public void ExitCar()
    {
        if (State == GameState.OnFoot) return;
        State = GameState.OnFoot;
        carController.enabled = false;
        fpsController.transform.SetParent(null);
        fpsController.transform.position = vehicleInteraction.exitPoint.position;
        fpsController.transform.rotation = Quaternion.Euler(0f, carController.transform.eulerAngles.y, 0f);
        cameraManager.SwitchToFPS();
        fpsController.enabled = true;
    }
}
```

- [ ] **Step 2: Unity Console'da derleme hatası yok — doğrula**

---

### Task 3: FpsController

**Files:**
- Create: `Assets/Scripts/FpsController.cs`

**Interfaces:**
- Consumes: `GameManager.Instance.EnterCar()`, `VehicleInteraction` (OverlapSphere ile arama — `GetComponentInParent`)
- Produces: `FpsController` component; `public Transform fpsHead`

- [ ] **Step 1: FpsController.cs oluştur**

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FpsController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 1.8f;
    public float jumpHeight = 1.2f;
    public float gravity = -9.81f;

    [Header("Look")]
    public float mouseSensitivity = 0.15f;
    public Transform fpsHead;

    [Header("Interaction")]
    public float interactRange = 3f;

    private CharacterController cc;
    private float xRotation;
    private Vector3 velocity;
    private VehicleInteraction nearbyVehicle;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        velocity.y = 0f;
    }

    private void Update()
    {
        HandleLook();
        HandleMovement();
        DetectVehicle();
        HandleInteract();
    }

    private void HandleLook()
    {
        if (Mouse.current == null) return;
        Vector2 look = Mouse.current.delta.ReadValue();
        xRotation -= look.y * mouseSensitivity;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        fpsHead.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * look.x * mouseSensitivity);
    }

    private void HandleMovement()
    {
        if (cc.isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        Vector2 moveInput = Vector2.zero;
        bool sprinting = false;
        bool jump = false;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveInput.y += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveInput.y -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveInput.x += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveInput.x -= 1f;
            sprinting = Keyboard.current.leftShiftKey.isPressed;
            jump = Keyboard.current.spaceKey.wasPressedThisFrame;
        }

        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            if (stick.sqrMagnitude > 0.01f) moveInput = stick;
            if (Gamepad.current.leftStickButton.isPressed) sprinting = true;
            if (Gamepad.current.buttonSouth.wasPressedThisFrame) jump = true;
        }

        if (jump && cc.isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        float speed = moveSpeed * (sprinting ? sprintMultiplier : 1f);
        Vector3 moveDir = transform.right * moveInput.x + transform.forward * moveInput.y;
        cc.Move(moveDir * speed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);
    }

    private void DetectVehicle()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange);
        nearbyVehicle = null;
        foreach (var col in hits)
        {
            var vi = col.GetComponentInParent<VehicleInteraction>();
            if (vi != null) { nearbyVehicle = vi; break; }
        }
    }

    private void HandleInteract()
    {
        bool ePressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        bool yPressed = Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame;
        if ((ePressed || yPressed) && nearbyVehicle != null)
            GameManager.Instance.EnterCar();
    }

    private void OnGUI()
    {
        if (nearbyVehicle == null) return;
        GUI.Label(new Rect(Screen.width / 2f - 80, Screen.height * 0.75f, 160, 30), "[E] Araca Bin");
    }
}
```

- [ ] **Step 2: Unity Console'da derleme hatası yok — doğrula**

---

### Task 4: CameraManager

**Files:**
- Create: `Assets/Scripts/CameraManager.cs`

**Interfaces:**
- Consumes: `public Transform fpsHead`, `public Transform carTarget`
- Produces: `CameraManager.SwitchToTPS()`, `CameraManager.SwitchToFPS()`

- [ ] **Step 1: CameraManager.cs oluştur**

```csharp
using System.Collections;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("References")]
    public Transform fpsHead;
    public Transform carTarget;

    [Header("TPS Settings")]
    public Vector3 tpsOffset = new Vector3(0f, 3f, -6f);
    public float followSpeed = 5f;
    public float rotateSpeed = 5f;

    [Header("Transition")]
    public float transitionDuration = 0.5f;

    private bool isTPS;
    private bool transitioning;

    private void Start()
    {
        transform.position = fpsHead.position;
        transform.rotation = fpsHead.rotation;
    }

    private void LateUpdate()
    {
        if (transitioning) return;

        if (isTPS)
        {
            Vector3 targetPos = carTarget.position + carTarget.rotation * tpsOffset;
            Quaternion targetRot = Quaternion.LookRotation(carTarget.forward, Vector3.up);
            transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = fpsHead.position;
            transform.rotation = fpsHead.rotation;
        }
    }

    public void SwitchToTPS()
    {
        StopAllCoroutines();
        StartCoroutine(TransitionTo(true));
    }

    public void SwitchToFPS()
    {
        StopAllCoroutines();
        StartCoroutine(TransitionTo(false));
    }

    private IEnumerator TransitionTo(bool toTPS)
    {
        transitioning = true;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);

            Vector3 endPos = toTPS
                ? carTarget.position + carTarget.rotation * tpsOffset
                : fpsHead.position;
            Quaternion endRot = toTPS
                ? Quaternion.LookRotation(carTarget.forward, Vector3.up)
                : fpsHead.rotation;

            transform.position = Vector3.Lerp(startPos, endPos, t);
            transform.rotation = Quaternion.Lerp(startRot, endRot, t);
            yield return null;
        }

        isTPS = toTPS;
        transitioning = false;
    }
}
```

- [ ] **Step 2: Unity Console'da derleme hatası yok — doğrula**

---

### Task 5: NpcCarController Tag Düzeltmesi

**Files:**
- Modify: `Assets/Scripts/NpcCarController.cs:49`

**Interfaces:**
- NPC araçlar artık `"PlayerCar"` tag'ini arar; FPS karakteri (`"Player"`) engel olarak algılanmaz.

- [ ] **Step 1: NpcCarController.cs satır 49'u değiştir**

Eski:
```csharp
GameObject player = GameObject.FindWithTag("Player");
```

Yeni:
```csharp
GameObject player = GameObject.FindWithTag("PlayerCar");
```

- [ ] **Step 2: Unity Console'da derleme hatası yok — doğrula**

---

### Task 6: Unity Editor'de Sahne Kurulumu

**Files:**
- `Assets/Scenes/SampleScene.unity` (Editor üzerinden değiştirilir)

Bu task tümüyle Unity Editor Inspector/Hierarchy'de yapılır — kod yazılmaz.

#### 6.1 "PlayerCar" Tag ekle ve araca ata

- [ ] Unity'de **Edit → Project Settings → Tags and Layers** aç
- [ ] **Tags** listesine yeni tag ekle: `PlayerCar`
- [ ] Hierarchy'de player arabasını seç (CarController olan)
- [ ] Inspector'da **Tag** → `PlayerCar` yap
- [ ] `CarController` component'ini Inspector'da **devre dışı bırak** (checkbox kaldır)

#### 6.2 VehicleInteraction ekle

- [ ] Player arabasını seç
- [ ] **Add Component → VehicleInteraction**
- [ ] Araca sağ tıkla → **Create Empty** → adını `ExitPoint` yap
- [ ] `ExitPoint`'i sol kapı hizasına taşı — arabanın local space'inde yaklaşık `(-2, 0, 0)`
- [ ] `VehicleInteraction` Inspector'unda `Exit Point` alanına `ExitPoint`'i sürükle

#### 6.3 Player GameObject oluştur

- [ ] Hierarchy'de sağ tıkla → **Create Empty** → adını `Player` yap
- [ ] **Tag** → `Player`
- [ ] **Position**: arabaya yakın ama içinde olmayan bir yer, örn. `(-3, 0, 0)` (y yere göre)
- [ ] **Add Component → Character Controller**
  - Center: `(0, 1, 0)` | Height: `2` | Radius: `0.4`
- [ ] **Add Component → FpsController**
- [ ] `Player`'a sağ tıkla → Create Empty → adını `FPSHead` yap
- [ ] `FPSHead` local position: `(0, 1.7, 0)` (göz hizası)
- [ ] `FpsController` Inspector'unda `Fps Head` alanına `FPSHead`'i sürükle

#### 6.4 Main Camera'yı bağımsız hale getir

- [ ] Hierarchy'de **Main Camera**'yı seç
- [ ] Eğer başka bir objenin child'ıysa sahne root'una sürükle (parent'ını kaldır)
- [ ] **Add Component → CameraManager**
- [ ] `Fps Head` → `FPSHead`'i sürükle
- [ ] `Car Target` → player arabasını sürükle
- [ ] Kamerada `AudioListener` varsa bırak; varsa sadece bir tane olduğundan emin ol

#### 6.5 GameManager oluştur

- [ ] Hierarchy'de sağ tıkla → **Create Empty** → adını `GameManager` yap
- [ ] **Add Component → GameManager**
- [ ] `Fps Controller` → `Player` objesini sürükle
- [ ] `Car Controller` → player arabasını sürükle
- [ ] `Camera Manager` → `Main Camera`'yı sürükle
- [ ] `Vehicle Interaction` → player arabasını sürükle (VehicleInteraction component'ini otomatik bulur)

#### 6.6 Play Mode Testi

- [ ] **Play** tuşuna bas
- [ ] Mouse ile FPS bakış çalışıyor — sağa/sola/yukarı/aşağı bak
- [ ] WASD ile karakter yürüyor — ileri/geri/yan
- [ ] Space ile zıplıyor
- [ ] Arabaya yaklaş (3m içi) — ekranda `[E] Araca Bin` görünüyor
- [ ] **E** tuşuna bas — kamera 0.5 saniyede TPS konumuna geçiyor
- [ ] WASD ile araba sürülüyor, TPS kamera arabanın arkasından takip ediyor
- [ ] **E** tuşuna bas — kamera FPS konumuna geri dönüyor, karakter sol kapı yanında çıkıyor
- [ ] Arabadan uzaklaşınca prompt kayboluyor

---

## Self-Review

1. ✅ `VehicleInteraction.exitPoint` Task 1'de tanımlandı, Task 2'de `ExitCar()` içinde kullanıldı
2. ✅ `GameManager.Instance.EnterCar()` Task 2'de tanımlandı, Task 3 `HandleInteract()` içinde çağrıldı
3. ✅ `CameraManager.SwitchToTPS()` / `SwitchToFPS()` Task 4'te tanımlandı, Task 2'de çağrıldı
4. ✅ `fpsHead` Transform Task 3'te `public Transform fpsHead`, Task 4'te `public Transform fpsHead` — her ikisi de Inspector'dan atanıyor
5. ✅ NpcCarController tag düzeltmesi `"PlayerCar"` — Task 6.1'de eklenen tag ile eşleşiyor
6. ✅ Placeholder yok — tüm code block'lar eksiksiz
7. ✅ `GetComponentInParent<VehicleInteraction>()` — araba child collider'larında da çalışır
8. ⚠️ **Edge case**: Araba hareket halindeyken E'ye basılırsa `carController.enabled = false` anında motor torku keser; araba momentumla durur. Bu kabul edilebilir davranış.
