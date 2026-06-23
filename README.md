# Project — Steam Discounts Tracker

Мобильное приложение на **.NET MAUI** для отслеживания скидок в Steam. Показывает актуальные предложения по регионам, позволяет искать игры по всему каталогу, сортировать по цене и популярности, а также отображает игры, недоступные для покупки в России.

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)
![MAUI](https://img.shields.io/badge/MAUI-purple?logo=dotnet)
![Platform](https://img.shields.io/badge/platform-Android%20%7C%20iOS%20%7C%20Windows%20%7C%20macOS-lightgrey)
![License](https://img.shields.io/badge/license-MIT-green)

---

## Содержание

- [Возможности](#возможности)
- [Архитектура](#архитектура)
- [Структура проекта](#структура-проекта)
- [Технологии и пакеты](#технологии-и-пакеты)
- [Установка и запуск](#установка-и-запуск)
- [Как это работает](#как-это-работает)
- [Известные ограничения](#известные-ограничения)
- [Возможные улучшения](#возможные-улучшения)
- [Лицензия](#лицензия)

---

## Возможности

- 🔥 **Актуальные скидки Steam** — список игр со скидками, обновляемый напрямую со страницы поиска Steam
- 🌍 **Переключение регионов** — Россия, США, Украина — с реальным пересчётом цен в локальной валюте
- 🚫 **Заблокированные в России игры** — отдельная секция с играми, недоступными для покупки в РФ (проверяется напрямую через Steam API)
- 🔎 **Поиск по всему каталогу** — не только по играм со скидками, но и по полному каталогу Steam
- ↕️ **Сортировка** — по популярности (% скидки), по возрастанию и убыванию цены
- 🎮 **Страница деталей игры** — описание, жанры, скриншоты, трейлер, метакритик-рейтинг
- ✨ **Анимации** — подсветка карточки при нажатии, плавное появление страницы деталей, разворачивание секций
- 🎨 **Дизайн в стиле Steam** — тёмная палитра, фирменные цвета (`#1b2838`, `#66c0f4`, `#beee11`)

---
---

## Архитектура

Проект построен по паттерну **MVVM (Model-View-ViewModel)** с **Dependency Injection** через встроенный DI-контейнер .NET MAUI.

```
┌─────────────┐      Binding       ┌──────────────────┐      вызов метода      ┌────────────────────┐
│    View     │ ─────────────────▶ │    ViewModel      │ ─────────────────────▶ │      Service        │
│  (XAML)     │ ◀───────────────── │  (состояние UI)   │ ◀───────────────────── │ (HTTP / парсинг)     │
└─────────────┘   PropertyChanged  └──────────────────┘        List<SteamGame>  └────────────────────┘
                                                                                          │
                                                                                          ▼
                                                                                  store.steampowered.com
```

**Принцип:** View ничего не знает об источнике данных. ViewModel не делает HTTP-запросов напрямую — этим занимается Service. Это позволяет менять способ получения данных (например, если Steam изменит вёрстку сайта), не трогая ViewModel и View.

### Слои приложения

| Слой | Назначение | Файлы |
|---|---|---|
| **Model** | Структуры данных | `SteamGame.cs` |
| **Service** | HTTP-запросы, парсинг HTML/JSON | `SteamParserService.cs`, `BlockedGamesProvider.cs` |
| **ViewModel** | Состояние экрана, команды, бизнес-логика UI | `MainViewModel.cs`, `GameDetailsViewModel.cs` |
| **View** | Разметка экранов | `MainPage.xaml`, `GameDetailsPage.xaml` |
| **Converter** | Преобразование данных для биндингов | `Converters/*.cs` |
| **DI / Bootstrap** | Регистрация зависимостей, запуск приложения | `MauiProgram.cs`, `App.xaml.cs`, `AppShell.xaml.cs` |

### Dependency Injection

Все сервисы и ViewModel регистрируются в `MauiProgram.cs` и внедряются через конструкторы:

```csharp
builder.Services.AddSingleton(CreateHttpClient());
builder.Services.AddSingleton<ISteamParserService, SteamParserService>();
builder.Services.AddSingleton<IBlockedGamesProvider, BlockedGamesProvider>();

builder.Services.AddTransient<MainViewModel>();
builder.Services.AddTransient<MainPage>();
```

| Lifetime | Используется для | Почему |
|---|---|---|
| `Singleton` | `HttpClient`, сервисы | Не хранят состояние конкретного экрана — безопасно шарить на всё приложение, избегает socket exhaustion |
| `Transient` | ViewModel, страницы | Каждое открытие экрана получает чистое, изолированное состояние |

### Навигация

Используется `Navigation.PushAsync` вместо Shell-роутинга — страница деталей создаётся вручную с передачей конкретного объекта игры, который не может быть известен DI-контейнеру заранее:

```csharp
var detailsPage = new GameDetailsPage(game, region, _parserService);
await Navigation.PushAsync(detailsPage);
```

---

## Структура проекта

```
Project/
├── App.xaml                      # Глобальные ресурсы (конвертеры, цвета)
├── App.xaml.cs                   # Точка входа приложения
├── AppShell.xaml                 # Shell-контейнер навигации
├── AppShell.xaml.cs
├── MauiProgram.cs                # Конфигурация DI-контейнера
├── Project.csproj                # Зависимости и таргеты сборки
│
├── MainPage.xaml                 # Главный экран: список скидок
├── MainPage.xaml.cs
├── GameDetailsPage.xaml          # Экран деталей игры
├── GameDetailsPage.xaml.cs
│
├── Models/
│   └── SteamGame.cs              # Модель игры
│
├── Services/
│   ├── ISteamParserService.cs    # Контракт сервиса парсинга
│   ├── SteamParserService.cs     # HTML/JSON парсинг Steam
│   ├── IBlockedGamesProvider.cs  # Контракт провайдера блокировок
│   └── BlockedGamesProvider.cs   # Проверка доступности игр в РФ
│
├── ViewModels/
│   ├── MainViewModel.cs          # Логика главного экрана
│   └── GameDetailsViewModel.cs   # Логика экрана деталей
│
└── Converters/
    ├── InverseBoolConverter.cs
    ├── SelectionColorConverter.cs
    ├── SelectionBorderConverter.cs
    ├── StringNotEmptyConverter.cs
    └── CountGreaterThanZeroConverter.cs
```

---

## Технологии и пакеты

| Пакет | Версия | Назначение |
|---|---|---|
| [.NET MAUI](https://learn.microsoft.com/dotnet/maui/) | 9.0 | Кроссплатформенный UI-фреймворк |
| [HtmlAgilityPack](https://www.nuget.org/packages/HtmlAgilityPack) | 1.11.71 | Парсинг HTML страницы поиска Steam |
| [CommunityToolkit.Maui](https://www.nuget.org/packages/CommunityToolkit.Maui) | 14.2.0 | Расширения MAUI (конвертеры, поведения) |
| [CommunityToolkit.Maui.MediaElement](https://www.nuget.org/packages/CommunityToolkit.Maui.MediaElement) | 10.0.0 | Воспроизведение видео-трейлеров |

Источники данных:
- **HTML-скрейпинг** `store.steampowered.com/search/?specials=1` — список игр со скидками (у Steam нет публичного API для этого)
- **JSON API** `store.steampowered.com/api/appdetails` — официальный API для подробностей об одной игре (описание, жанры, скриншоты, трейлер, цена)

---

## Установка и запуск

### Требования

- [Visual Studio 2022](https://visualstudio.microsoft.com/) (17.8+) с установленной нагрузкой **.NET Multi-platform App UI development**
- .NET 9 SDK
- Для Android: Android SDK (устанавливается вместе с нагрузкой MAUI)
- Для iOS/macOS: Mac с Xcode (сборка под Windows для этих платформ не поддерживается)

### Шаги

1. Клонируйте репозиторий:
   ```bash
   git clone https://github.com/<your-username>/<repo-name>.git
   cd <repo-name>
   ```

2. Восстановите NuGet-пакеты:
   ```bash
   dotnet restore
   ```

3. Откройте `Project.csproj` (или `.sln`) в Visual Studio.

4. Выберите целевую платформу (Android Emulator / Windows Machine / etc.) в выпадающем списке рядом с кнопкой запуска.

5. Запустите приложение (`F5` или кнопка ▶️).

### Сборка из командной строки

```bash
dotnet build -t:Run -f net8.0-android
```

---

## Как это работает

### Получение списка скидок

`SteamParserService.GetDiscountsAsync` отправляет GET-запрос на страницу поиска Steam с фильтром `specials=1` и разбирает полученный HTML через `HtmlAgilityPack`, извлекая из DOM-узлов название, цену, процент скидки и ссылку на изображение:

```csharp
var nodes = doc.DocumentNode.SelectNodes("//a[contains(@class, 'search_result_row')]");
```

### Получение деталей игры

`SteamParserService.GetGameDetailsAsync` использует официальный JSON API Steam — надёжнее HTML-парсинга, но доступен только для одной игры за раз:

```
GET https://store.steampowered.com/api/appdetails?appids={id}&cc={region}&l=russian
```

### Региональные цены

Каждый регион (`RU`, `US`, `UA`) передаётся как параметр `cc=` в запросах к Steam — сервер Steam сам возвращает цены в локальной валюте и с учётом региональных скидок.

### Проверка доступности региона

`IsRegionAvailableAsync` делает тестовый запрос и проверяет HTTP-статус — Steam отдаёт `403 Forbidden` для полностью заблокированных регионов (например, Украина с 2022 года).

### Игры, заблокированные в России

`BlockedGamesProvider` хранит список из ~15 известных AppId (Hogwarts Legacy, GTA V, Baldur's Gate 3 и др.) и **в реальном времени** проверяет каждую игру через `appdetails?cc=RU`. Если Steam возвращает `success: false` — игра считается заблокированной и попадает в список.

> У Steam нет API для получения **полного** списка заблокированных в каком-либо регионе игр — единственный способ узнать это — спросить про конкретную игру. Поэтому список AppId статический (seed-список), а статус блокировки — динамический (проверяется при каждой загрузке).

### Реактивность интерфейса

- `ObservableCollection<SteamGame>` — автоматически уведомляет `CollectionView` об изменениях списка
- `INotifyPropertyChanged` на `SteamGame.IsSelected` — подсветка карточки при тапе без пересоздания списка
- Конвертеры (`SelectionColorConverter`, `InverseBoolConverter` и др.) — преображают `bool`/`string` в `Color`/`bool` видимости прямо в биндинге

### Анимации

- **Подсветка тапа** — `ScaleTo(0.97) → ScaleTo(1.0)` имитирует нажатие
- **Появление страницы деталей** — `FadeTo` + `TranslateTo` при `OnAppearing`
- **Разворачивание секции** — `FadeTo` + `TranslateTo` при раскрытии блока заблокированных игр

### Защита от лишних запросов

- **Debounce поиска** — `Task.Delay(450ms)` перед отправкой запроса, чтобы не дёргать Steam на каждое нажатие клавиши
- **CancellationToken** — отменяет предыдущий незавершённый запрос при смене региона или новом поисковом вводе, чтобы избежать гонки ответов

---
---

## Лицензия

Этот проект распространяется под лицензией MIT — см. файл [LICENSE](LICENSE).

Этот проект не связан с Valve Corporation или Steam. Все товарные знаки принадлежат их законным владельцам. Данные получены путём обращения к публично доступным страницам и API store.steampowered.com.
