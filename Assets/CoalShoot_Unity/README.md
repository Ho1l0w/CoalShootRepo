# CoalShoot — скрипты для Unity 2D

Перенос браузерного прототипа в Unity: управление с coyote time и буфером прыжка, пять видов оружия, парирование, супер-выстрелы, враги, босс с фазами, уголь, лавка, сохранения.

Все 66 файлов проверены парсером C# на синтаксис. Скомпилировать в Unity здесь было негде, поэтому при первом импорте возможны мелкие ошибки — пришлите текст из консоли, поправлю.

**Требования:** Unity 2022.3 LTS или Unity 6, пакет TextMeshPro (для UI). Пакет Input System необязателен: скрипт ввода работает и с новым Input System, и со старым Input Manager.

---

## Как всё устроено

```
                    ┌────────────────────┐
  клавиатура,  ───▶ │ PlayerInputReader  │  нажатия копятся в Update, забираются в FixedUpdate
  геймпад           └─────────┬──────────┘
                              ▼
                    ┌────────────────────┐   ProgressionService (сохранение, лавка, способности)
                    │ PlayerController   │◀── ApplyLoadout(): двойной прыжок, амулеты, оружие в слотах
                    └─┬──────┬──────┬────┘
                      ▼      ▼      ▼
            PlayerMotor2D  PlayerAim  WeaponController ──▶ Projectile (пул) ──▶ IDamageable
            (движение)     (8 направлений)   │                                    ├ Health (враги, босс)
                 ▲                           ▼                                    └ PlayerHealth
            PlayerParry ───────────────▶ SuperMeter (5 карт)
            (прыжок в воздухе у синего)

  EnemyBase ── Walker / Flyer / Turret / Hopper / ShieldBearer      ContactDamage (касание, прыжок сверху, рывок)
  BossController ── фазы ── BossAttack (ArcSpit, BulletRing, Slam, SpawnMinions)
  LevelManager ── игроки, чекпоинт, время, итог ──▶ GameEvents ──▶ UI (HUD, итоги, надписи)
```

Главные решения:

- **Движение своё, не на физике Unity.** Rigidbody2D с нулевой гравитацией: скорость считает `PlayerMotor2D`, столкновения решает физика. Так управление предсказуемое и всё «ощущение» живёт в одном ассете `MovementConfig`.
- **Снаряды двигаются сами и проверяют попадания кастами.** Быстрые пули не пролетают сквозь стены, а матрица коллизий не влияет на попадания. Все снаряды берутся из пула: в WebGL это убирает рывки сборщика мусора.
- **Урон через интерфейс `IDamageable`.** Снаряду всё равно, во что он попал: игрок, ползун или босс.
- **Геймплей не знает про UI.** Он поднимает `GameEvents`, интерфейс подписывается.
- **Прогресс — один сервис со списками в JSON.** Хранилище подменяется (`ISaveStorage`), так что облачные сохранения Яндекса подключаются без переписывания логики.

## Папки

| Папка | Что внутри |
|---|---|
| `Core` | HitStop, GamePause, CameraShake, CameraFollow2D, FX, GameEvents, совместимость Rigidbody2D для Unity 6 |
| `Input` | PlayerInputReader: раскладки для одного и двух игроков, геймпады |
| `Player` | MovementConfig, PlayerMotor2D, PlayerAim, PlayerController, PlayerHealth, PlayerParry, PlayerVisuals, PlayerGhost, PlayerRegistry |
| `Combat` | WeaponData, WeaponController, Projectile, ProjectilePool, Health, SuperMeter, Parryable, Shield, ContactDamage |
| `Enemies` | EnemyBase, Walker, Flyer, Turret, Hopper, ShieldBearer |
| `Boss` | BossController, BossAttack, четыре атаки, Shockwave |
| `Level` | LevelManager, Checkpoint, CoalPickup, AbilityPickup, LevelGoal, KillZone, MovingPlatform, CrumblePlatform, TutorialTrigger |
| `Progression` | SaveData, хранилище, LevelData, CharmData, Grading, ProgressionService |
| `UI` | HUD, полоска босса, крупные надписи, итоги, карта, лавка |

---

## Порядок внедрения

Не собирайте всё сразу. Каждый шаг можно проверить отдельно:

1. Слои → игрок → тестовая комната. Добейтесь, чтобы прыжок и бег ощущались как в прототипе.
2. Оружие и снаряды.
3. Враги и шипы.
4. ProgressionService, уголь, чекпоинты, дверь, карта и лавка.
5. Босс.

## 1. Слои и физика

Project Settings → Tags and Layers, добавьте слои:

| Слой | Что на нём |
|---|---|
| Ground | Тайлмап земли, движущиеся и рассыпающиеся платформы |
| OneWay | Платформы, сквозь которые можно запрыгнуть снизу (PlatformEffector2D) |
| Player | Корень игрока |
| Enemy | Корень врагов и босса, щиты |
| Hazard | Триггеры урона: ContactDamage врагов, шипы, ударные волны |
| EnemyProjectile | Вражеские снаряды |
| PlayerProjectile | Снаряды игрока |
| Parry | Звёзды для парирования, призрак друга |

Project Settings → Physics 2D → Layer Collision Matrix. Оставьте включёнными только пары:

- Player ↔ Ground, Player ↔ OneWay, Player ↔ Hazard, Player ↔ Default (триггеры уровня).
- Enemy ↔ Default (KillZone).

Остальные пары с Player, Enemy и снарядами выключите. Попадания снарядов и парирование ищутся запросами по маскам, матрица на них не влияет.

## 2. Масштаб и камера

Числа в скриптах пересчитаны из прототипа при **1 тайл = 1 юнит**: персонаж ростом около 1.1 юнита, прыжок на 4 тайла. Если тайлы рисуете по 10 px, ставьте Pixels Per Unit = 10. При 16 px на тайл — PPU = 16, числа те же.

Пересчёт из прототипа: px/кадр × 6 = юнит/с, px/кадр² × 360 = юнит/с², кадры / 60 = секунды.

Камера:

```
CameraRig     — CameraFollow2D (границы уровня в minBounds/maxBounds)
 └ Shake      — CameraShake
    └ Main Camera — Orthographic, Size ≈ 9; по желанию Pixel Perfect Camera
```

## 3. Префаб игрока

```
Player (слой Player)
 ├ Rigidbody2D        Dynamic, Gravity Scale 0, Freeze Rotation Z, Interpolate, Continuous
 ├ BoxCollider2D      ≈ 0.6 × 1.1, Physics Material 2D с Friction 0
 ├ PlayerController   Visual Root → Visual, Ghost Prefab → призрак
 ├ PlayerMotor2D      Config → MovementConfig; Ground Mask = Ground + OneWay; Solid Mask = Ground
 ├ PlayerInputReader
 ├ PlayerAim
 ├ PlayerHealth
 ├ PlayerParry        Parry Mask = EnemyProjectile + Parry
 ├ SuperMeter
 ├ WeaponController   Aim Pivot → AimPivot; Default Loadout → Перо, Клякстрел
 ├ PlayerVisuals      Body → Visual; Sprite → спрайт
 ├ AimPivot           пустой объект на уровне груди, НЕ внутри Visual
 └ Visual             спрайт с пивотом внизу по центру (сжатие идёт от ног)
```

Сделайте два префаба: **Квадрат** (`MovementConfig` по умолчанию) и **Клякса** (копия конфига: Run Speed 12.6, Jump Height 3.8, Can Glide включён, коллайдер ≈ 0.6 × 0.9).

Призрак друга: спрайт + CircleCollider2D (isTrigger) на слое Parry + `Parryable` + `PlayerGhost`.

## 4. Ощущение управления

| Параметр | Прототип | В Unity | Что даёт |
|---|---|---|---|
| Coyote time | 10 кадров | 0.17 с | Прыжок срабатывает после схода с края |
| Буфер прыжка | 12 кадров | 0.2 с | Нажатие до приземления не теряется |
| Буфер рывка | 10 кадров | 0.17 с | То же для рывка |
| Зависание в верхней точке | гравитация ×0.5 | Apex Gravity Mult 0.5 | Легче прицелиться и попасть на платформу |
| Падение тяжелее подъёма | ×1.47 | Fall Gravity Mult | Прыжок не «ватный» |
| Обрезание прыжка | ×0.45 | Jump Cut Mult | Тап — низкий прыжок, удержание — высокий |
| Разворот | разгон ×1.7 | Turn Boost | Смена направления без скольжения |
| Коррекция углов | 3 px | 0.3 юнита | Задел потолок углом — соскользнул и полетел дальше |

Coyote time и буфер прыжка подбирайте по ощущению: при 0.2–0.25 с игра прощает почти всё, при 0.1 с становится строгой.

## 5. Оружие

Create → CoalShoot → Weapon, пять ассетов:

| id | Название | Режим | Интервал | Скорость | Урон | Время жизни | Прочее | Супер | Цена |
|---|---|---|---|---|---|---|---|---|---|
| pen | Перо | Straight | 0.133 | 39 | 1 | 0.92 | — | PiercingStar: урон 2.2, скорость 25, 1.6 с | 0 |
| blot | Клякстрел | Spread | 0.3 | 30 | 1.15 | 0.4 | Pellets 3, Spread 12.6°, Recoil 6.6, Shake 0.1 | Radial: 10 шт., урон 2.6, скорость 26.4, 0.57 с | 0 |
| spark | Искра | Homing | 0.183 | 22.8 | 0.75 | 1.67 | Turn 450°/с | HomingSwarm: 6 шт., урон 1.7, скорость 20.4, 2.5 с | 4 |
| lob | Уголёк | Lob | 0.367 | 20.4 | 2.3 | 2.33 | Lob Up 14.4, Gravity 72, Bounces 1 | BigLob: урон 5, скорость 19.2, 3.3 с | 5 |
| charge | Заряд | Charge | 0.167 | 42 | 0.9 | 1.0 | Charge Time 0.67, Charged Damage 6 | Lance: урон 4.5, скорость 54, 0.67 с | 6 |

Префаб снаряда: Rigidbody2D (Kinematic) + CircleCollider2D (isTrigger) + SpriteRenderer + `Projectile`. В масках: Solid = Ground, Hits Players = Player, Hits Enemies = Enemy. Снаряды игрока на слое PlayerProjectile. Вражеские — на слое EnemyProjectile, с выключенным компонентом `Parryable`: снаряд сам включит его и перекрасит в синий, когда атака помечает выстрел как парируемый.

## 6. Враги

```
Enemy (слой Enemy)
 ├ Rigidbody2D (Kinematic), BoxCollider2D, Health, скрипт врага (Ground Mask = Ground)
 ├ Visual          спрайты, нарисованы смотрящими вправо
 │  └ Shield       только у щитоносца: BoxCollider2D (isTrigger, слой Enemy) + Shield (Facing Source → Visual)
 └ Contact         BoxCollider2D (isTrigger, слой Hazard) + ContactDamage
```

| Враг | Здоровье | Особенность |
|---|---|---|
| Ползун (Walker) | 3 | Патруль, разворот у края |
| Летун (Flyer) | 2 | Преследует в радиусе 11 |
| Турель (Turret) | 6 | Заряд 0.4 с, каждый 3-й выстрел синий. Потолочная — Visual со scale.y = −1 |
| Прыгун (Hopper) | 3 | Прыгает к игроку |
| Щитоносец (ShieldBearer) | 5 | Щит в лицо; разворачивается с задержкой 0.57 с. У ContactDamage укажите Shield |

Шипы: статичный коллайдер-триггер на слое Hazard + `ContactDamage` (Stompable выкл., Bounce Player Up вкл., Health пусто).

## 7. Уровень

На сцене уровня:

- `ProgressionService` (префаб, один и тот же на всех сценах).
- `LevelManager`: Level → ассет уровня, префабы Квадрата и Кляксы, Spawn Point.
- `KillZone`: длинный триггер под уровнем.
- `Checkpoint`, `CoalPickup` (id `1-0` … `1-4` — номер уровня и номер куска), `LevelGoal`, `TutorialTrigger`.
- На «Мостах» — `AbilityPickup` (двойной прыжок), а после него стена в 6 тайлов: без двойного прыжка дальше не пройти.
- Canvas: `HUDView` (Player Index 0 и 1), `AnnouncerUI`, `ResultsView`.

Ассеты уровней (Create → CoalShoot → Level):

| id | Название | Сцена | Par, с | Награда | Требует |
|---|---|---|---|---|---|
| 1 | Шахта | Level1 | 70 | 2 | — |
| 2 | Мосты | Level2 | 85 | 2 | Шахта |
| 3 | Цех | Level3 | 100 | 2 | Мосты |
| 4 | Чернильница | Boss | 80 | 5 | Цех (Is Boss) |

Амулеты (Create → CoalShoot → Charm): «Лишнее сердце» (ExtraHeart, 3), «Дымовая шашка» (SmokeDash, 3), «Крепкий кофе» (Coffee, 4). Угля всего 26 (15 кусков + 3×2 + 5) при ценах лавки 25 — почти всё можно купить к концу.

Расстановку уровней удобно переносить из прототипа: там уровни заданы в тайлах, по координатам `buildL1`–`buildL3`.

## 8. Босс

```
Boss (слой Enemy)
 ├ Health          Max 300, Destroy On Death ВЫКЛ
 ├ BossController  фазы ниже
 ├ BoxCollider2D
 ├ Contact         ContactDamage (Stompable выкл.), слой Hazard
 └ Attacks         дочерние объекты с ArcSpitAttack, BulletRingAttack, SlamAttack, SpawnMinionsAttack
```

| Фаза | Ends At Health | Атаки | Пауза |
|---|---|---|---|
| 1 | 0.666 | ArcSpit, SpawnMinions | 0.9 |
| 2 | 0.333 | ArcSpit и SpawnMinions — вторые компоненты с Count 4 и 3 | 0.9 |
| 3 | 0 | BulletRing, Slam | 0.67 |

Волну по полу и чернильный дождь из прототипа можно добавить во вторую фазу по образцу `SlamAttack` и `ArcSpitAttack`. В LevelManager сцены босса заполните поле Boss. Полоска здоровья — `BossBarView`.

Новая атака — класс от `BossAttack` с корутиной `Run`: предупреждение → действие → окно для ответа. Всё, что атака создаёт (волны, миньоны), передавайте в `boss.Track(...)`, тогда смена фазы это уберёт.

## 9. Карта и лавка

Сцена `Map`: `ProgressionService`, `WorldMapView` (узлы `LevelNodeView` по порядку), `ShopView` с префабом карточки `ShopItemView`. Узел недоступного уровня неактивен и пишет, что нужно пройти раньше.

---

## Под Яндекс Игры

**Сохранения.** В WebGL PlayerPrefs лежат в IndexedDB браузера и пропадают при очистке данных. Подключите облачные сохранения SDK своим адаптером и назначьте его до загрузки. Пример для PluginYG2 — сверьте имена с документацией вашей версии плагина:

```csharp
// В отдельном файле: поле в сохранениях плагина
namespace YG { public partial class SavesYG { public string coalshoot = ""; } }

public class YandexSaveStorage : CoalShoot.ISaveStorage
{
    public string Load() => YG.YG2.saves.coalshoot;
    public void Save(string json) { YG.YG2.saves.coalshoot = json; YG.YG2.SaveProgress(); }
}

// После инициализации SDK (событие получения данных плагина):
// ProgressionService.Instance.Storage = new YandexSaveStorage();
// ProgressionService.Instance.Load();
```

**Реклама и фокус.** При открытии рекламы и уходе со вкладки вызывайте `GamePause.SetPaused(true)`, после — `GamePause.SetPaused(false)`. Это останавливает время и звук; без этого модерация отклоняет игру. `HitStop` учитывает паузу и не снимет её во время рекламы.

**Язык.** Подписи в UI берите из локализации по `ysdk.environment.i18n.lang`, а не вписывайте в префабы. Это тоже частая причина отказа.

**Сборка.** WebGL, сжатие Brotli или Gzip с включённым Decompression Fallback, Strip Engine Code, текстуры в атласах. Пул снарядов уже есть; эффекты частиц тоже лучше пулить, если их станет много.

**Телефон.** Скрипт ввода читает клавиатуру и геймпад. Для сенсорного управления добавьте экранные кнопки (On-Screen Button из Input System), которые эмулируют клавиши или кнопки геймпада.
