# 코드 작성 규칙 (Coding Convention)

작성자: 남예성

---

## A. 네이밍 컨벤션 및 구조 (Naming & Structure)

### **1. 클래스 및 타입 (Types & Classes)**

- 객체의 설계도가 되는 큰 단위들은 모두 **PascalCase(첫 글자 대문자)**를 사용합니다.

| **종류** | **규칙** | **설명** | **올바른 예시** |
| --- | --- | --- | --- |
| **클래스 (Class)** | `PascalCase` | 명사 형태로 작성합니다. | `PlayerController`, `ScoreCalculator` |
| **구조체 (Struct)** | `PascalCase` | 메모리를 아끼기 위한 데이터 군집(ECS 등)에 자주 쓰입니다. 명사로 작성합니다. | `PlayerPerformanceData`, `MatchResult` |
| **인터페이스 (Interface)** | `I`+`PascalCase` | 반드시 대문자 **I**로 시작하여 클래스와 구분합니다. | `IDamageable`, `IInteractable` |
| **열거형 (Enum)** | `PascalCase` | 여러 개라도 **단수형 명사**를 사용합니다. | `WeaponType` (O), `WeaponTypes` (X) |

### **2. 변수 및 데이터 (Variables & Data)**

데이터를 저장하는 변수는 접근 권한(Public, Private)과 역할에 따라 형태를 명확히 구분합니다.

| **종류** | **규칙** | **설명** | **올바른 예시** |
| --- | --- | --- | --- |
| **일반 변수 & 매개변수** | `camelCase` | 함수 내부에서 잠깐 쓰다 버리는 지역 변수나 매개변수(전달받는 값)에 사용합니다. | `moveSpeed`, `ddDeltaValue` |
| **프라이빗 (Private) 멤버 변수** | `_camelCase` | 클래스 상단에 선언되어 전체에서 계속 기억하는 변수입니다. 반드시 앞에 언더바(`_`)를 붙입니다. | `_currentHealth`, `_mmrScore` |
| **프로퍼티 (Property)** | `PascalCase` | C#의 핵심 기능인 `get/set`을 사용하는 속성은 대문자로 시작합니다. | `CurrentAcs { get; private set; }` |
| **상수 (Const)** | `ALL_CAPS` | 값이 절대 변하지 않는 상수는 모두 대문자와 언더바로 적습니다. | `MAX_HEALTH`, `DEFAULT_TIME` |
| **UI 컴포넌트** | `접두어_이름` | 코드만 보고도 어떤 UI인지 직관적으로 알 수 있게 앞에 소문자 약어를 붙입니다. | `btn_Start`, `txt_PlayerName`, `img_Icon` |
- **UI 접두어 통일하기:**
    - UI 변수명은 팀원 간 약속이 다르면 헷갈리기 쉽습니다. 아래 표를 규칙으로 고정해 두세요.
        - Button = `btn_`
        - Text = `txt_`
        - Image = `img_`
        - Slider = `sld_`
        - InputField = `inp_`

### **3. 함수 및 이벤트 (Methods & Events)**

어떠한 행동이나 동작을 정의하는 부분입니다.

| **종류** | **규칙** | **설명** | **올바른 예시** |
| --- | --- | --- | --- |
| **메서드 (함수)** | `PascalCase` | **동사로 시작**하여 어떤 동작을 하는지 명확히 합니다. | `CalculateScore()`, `TakeDamage()` |
| **이벤트 (Event)** | `On`  • `PascalCase` | 특정 상황이 발생했을 때 알려주는 이벤트나 콜백 함수는 'On'을 붙입니다. | `OnPlayerDeath`, `OnMatchEnd` |
| **부울 (Bool) 함수/변수** | `is`, `has`, `can` | 참/거짓을 반환하는 경우, 질문하는 형태의 접두어를 붙입니다. | `isGrounded`, `HasWeapon()`, `CanJump` |
- **단어 축약 금지 (Don't abbreviate):**
    - 변수 이름을 지을 때 혼자만 아는 줄임말을 쓰지 마세요. 자동완성이 잘 되기 때문에 길더라도 의미가 명확한 것이 좋습니다.
        - (X) `int pDmg;`
        - (O) `int playerDamage;`

### 4. 네임스페이스(Namespace) 사용

프로젝트가 커지고 에셋 스토어에서 여러 플러그인을 다운로드하다 보면, `Player`, `GameManager` 같은 흔한 클래스 이름이 충돌하여 오류가 납니다.

- **규칙:** 모든 작성 스크립트는 우리 게임만의 고유한 `namespace` 안에서 작성합니다.

```csharp
namespace MyAwesomeGame.Core
{
    public class GameManager : MonoBehaviour
    {
        // 내용
    }
}
```

---

## B. 코드 포맷팅 (Code Formatting)

### 1. 중괄호 `{}` 스타일

C#은 기본적으로 중괄호를 **새로운 줄(Allman 스타일)**에 작성하는 것이 표준입니다. 웹 개발에서 주로 쓰는 방식(같은 줄에 작성)과 다르므로 팀원들과 확실히 규칙을 정해야 코드가 깔끔해집니다.

- **C# 표준 (권장):**C#
    
    ```csharp
    if (isGrounded)
    {
        Jump();
    }
    ```
    
- **웹/JS 스타일 (비권장):**C#
    
    ```csharp
    if (isGrounded) {
        Jump();
    }
    ```
    

---

## C. 유니티 인스펙터 연동 및 접근 제어 (Inspector & Access Control)

### 1. 접근 제어자와 `[SerializeField]` 활용

유니티를 처음 다루는 개발자들이 가장 많이 하는 실수가 "에디터(인스펙터)에서 값을 수정하기 위해 변수를 무조건 `public`으로 선언하는 것"입니다. 이는 객체지향의 캡슐화를 깨뜨려 나중에 심각한 버그를 초래합니다.

- **규칙:** 다른 스크립트에서 직접 접근해야 하는 경우가 아니라면, 변수는 무조건 `private`으로 선언합니다.
- **인스펙터 노출:** 기획자나 아티스트가 유니티 에디터에서 값을 조절해야 하는 변수라면, `public` 대신 **`[SerializeField] private`을 사용합니다.

```csharp
// X 나쁜 예: 아무 스크립트에서나 접근 가능해짐
public float moveSpeed = 5f; 

// O 좋은 예: 외부 접근은 막고 유니티 에디터에서만 수정 가능
[SerializeField] private float _moveSpeed = 5f;
```

### 2. 툴팁(`[Tooltip]`) 속성 활용하기

- XML 주석은 **코드 상**에서 도움말을 주지만, 유니티 **인스펙터(Inspector) 창**에서 아티스트나 기획자에게 도움말을 주려면 `[Tooltip]` 속성을 함께 사용하는 것이 좋습니다.

```csharp
[Tooltip("플레이어의 이동 속도를 결정합니다. 기본값은 5입니다.")]
public float moveSpeed = 5f;
```

### 3. 인스펙터 꾸미기 속성 (`[Header]`, `[Range]`)

`[Tooltip]`과 더불어 아티스트와 기획자를 배려하는 최고의 기능입니다. 코드를 몰라도 에디터 창이 깔끔하게 정리되어 협업 능률이 크게 오릅니다.

- **`[Header("이름")]`**: 인스펙터 창에서 변수들을 그룹화하고 굵은 제목을 달아줍니다.
- **`[Range(min, max)]`**: 변수를 숫자 입력칸 대신 **슬라이더(Slider)** 형태로 만들어, 기획자가 안전한 범위 내에서만 값을 조절하게 강제할 수 있습니다.

```csharp
[Header("플레이어 이동 설정")]
[SerializeField] private float _walkSpeed = 5f;
[SerializeField] private float _runSpeed = 8f;

[Header("체력 설정")]
[Range(0, 100)] // 0~100 사이의 슬라이더 생성
[SerializeField] private int _maxHealth = 100;
```

---

## D. 최적화 코딩 규칙

단순한 코드 규칙을 넘어, 게임이 끊기지 않게(프레임 드랍 방지) 하기 위한 필수 룰입니다. 향후 ECS나 DOTS 같은 고성능 데이터 지향 아키텍처를 도입할 때를 대비해서라도 미리 습관을 들이는 것이 좋습니다.

### **1.** `Update()` 안에서 무거운 함수 호출 금지 (캐싱 필수)

- `GetComponent<T>()`나 `GameObject.Find()`는 맵 전체를 뒤지는 무거운 작업입니다.
- **규칙:** 반드시 `Awake()`나 `Start()`에서 변수에 한 번만 저장(캐싱)해 두고, `Update()`에서는 그 변수를 가져다 쓰기만 하세요.

### **2. 태그(Tag) 비교 시** `CompareTag` 사용

- (X) `if (gameObject.tag == "Player")`: 문자열을 새로 생성하여 메모리 쓰레기(Garbage)를 만듭니다.
- (O) `if (gameObject.CompareTag("Player"))`: 유니티가 내부적으로 최적화한 방식입니다.

### **3. 빈번한 생성(Instantiate)과 파괴(Destroy) 금지**

- 총알, 타격 이펙트, 데미지 텍스트 등 짧은 시간에 대량으로 나타났다 사라지는 오브젝트를 매번 만들고 부수면 엄청난 과부하가 발생합니다.
- **규칙:** 게임 시작 시 미리 100개를 만들어 창고에 숨겨두고, 필요할 때 꺼내 쓰고 다시 집어넣는 **'오브젝트 풀링(Object Pooling)'** 기법을 무조건 사용해야 합니다.

### **4.** `Update()` 내에서 문자열(String) 더하기 자제

- C#에서 문자열을 더할 때마다(`.ToString()` 포함) 새로운 메모리가 할당됩니다. 매 프레임마다 UI 텍스트를 업데이트하면 순식간에 쓰레기 메모리가 쌓여 프레임이 뚝 떨어집니다.
    - (X) `scoreText.text = "점수: " + currentScore.ToString();` (Update 안에서 매 프레임 실행)
    - (O) 값이 변했을 때만 UI를 갱신하거나, `StringBuilder`를 사용하세요.

### **5. 물리 연산과 일반 로직의 엄격한 분리**

- 유니티는 프레임이 그려지는 주기와 물리 엔진이 계산되는 주기가 다릅니다.
- **규칙:** 유저의 키보드/마우스 입력이나 타이머는 `Update()`에서 처리하고, `Rigidbody`에 힘을 가하거나 물리적인 충돌을 계산하는 코드는 무조건 `FixedUpdate()` 안에서 작성하세요.

### **6. LINQ 사용 자제**

- `using System.Linq;` 의 `Where`, `ToList()` 같은 기능은 코드를 짧게 만들어 주지만, 게임 플레이 도중(Hot Path)에 사용하면 보이지 않는 가비지를 대량으로 발생시킵니다.
- **규칙:** 게임 실행 중에는 전통적인 `for`문이나 `foreach`문을 사용하세요.

### **7. 레이캐스트(Raycast) 사용 시 LayerMask 지정**

- 허공에 광선을 쏴서 충돌체를 찾는 Raycast는 비용이 비쌉니다.
- **규칙:** 레이캐스트를 쏠 때 맵에 있는 모든 오브젝트와 연산하지 않도록, `LayerMask`를 파라미터로 넣어 '특정 레이어(예: Ground, Enemy)'와만 연산하도록 제한하세요.

### 8. 코루틴(Coroutine) 문자열 호출 금지

- 코루틴을 실행할 때 함수 이름을 문자열(String)로 넣는 방식은 유니티 초기 버전의 잔재이며, 오타가 나도 에러를 잡아주지 않고 성능에도 좋지 않습니다.
    - (X) `StartCoroutine("AttackRoutine");` (문자열 사용 시 가비지 발생 및 추적 어려움)
    - (O) `StartCoroutine(AttackRoutine());` (메서드를 직접 호출하는 방식 사용)

---

## E. 주석 및 문서화

### **1. XML 주석 (**`///`) 사용하기

- 클래스나 메서드 바로 윗줄에서 슬래시를 세 번(`///`) 치면 자동으로 요약(Summary) 주석이 생성됩니다.
- 이렇게 주석을 달아두면, 다른 개발자가 해당 함수에 마우스를 올렸을 때 설명이 툴팁으로 떠서 협업 속도가 엄청나게 빨라집니다.

```csharp
/// <summary>
/// 캐릭터의 체력을 관리하고 데미지 처리를 담당하는 클래스입니다.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    /// <summary> 현재 플레이어의 체력 값입니다. </summary>
    private float _currentHp;

    /// <summary>
    /// 외부에서 데미지를 입었을 때 체력을 깎는 함수입니다.
    /// </summary>
    /// <param name="damageAmount">입힐 데미지 양</param>
    /// <param name="attackerName">공격자의 이름 (로그 기록용)</param>
    /// <returns>데미지 처리 후 플레이어가 생존해 있는지 여부</returns>
    public bool TakeDamage(float damageAmount, string attackerName)
    {
        _currentHp -= damageAmount;
        Debug.Log($"{attackerName}에게 {damageAmount}만큼 데미지를 입었습니다.");

        return _currentHp > 0;
    }
}
```

---

참고 :

[마이크로소프트 C# 코드 컨벤션](https://learn.microsoft.com/ko-kr/dotnet/csharp/fundamentals/coding-style/coding-conventions), 

[구글 C# 코드 컨벤션](https://google.github.io/styleguide/csharp-style.html)

---