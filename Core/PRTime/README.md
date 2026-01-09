# PRTime
PRTime — это модуль управления игровым временем, реализующий поведение, аналогичное UnityEngine.Time, но с поддержкой логической паузы SDK.

```csharp
// Общее время которое прошло с момента инициализации PRTime.
var time = PRTime.Instance.Time;
// Время прошедшее с последнего кадра, с учётом паузы логики.
var time = PRTime.Instance.DeltaTime;
// Время прошедшее с последнего кадра, без учёта паузы логики.
var time = PRTime.Instance.LastRawTime;
```
