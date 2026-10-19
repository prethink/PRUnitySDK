# Modules

Модули публичного SDK поверх ядра.

- [@DamageSystem](@DamageSystem/README.md) — урон, сопротивления и правила урона
- [DOTweenEffects](DOTweenEffects/README.md) — DOTween с логической паузой и `PRTimeScale`, готовые эффекты
- [GameSessions](GameSessions/README.md) — сессия карты из раундов с разными режимами и правилами
- [HitBox](HitBox/README.md) — коллайдеры-хитбоксы, которые передают попадание в `HealthComponent` сущности
- [Translate](Translate/README.md) — локализация: провайдеры, база переводов, живой перевод
- `Camera` — основа контроллеров камеры (`CameraControllerBase`)

Машина состояний (`StateManager`) живёт в проектном слое:
[PRUnitySDKPrivate/Modules/StateManager](../../PRUnitySDKPrivate/Modules/StateManager/README.md).
