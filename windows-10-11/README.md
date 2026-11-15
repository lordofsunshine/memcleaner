<div align="center">

<img src="https://i.postimg.cc/8Pr1ThnQ/image.png" alt="MemCleaner Banner" width="100%">

# 🧹 MemCleaner — Windows 10 / 11

**A modern Windows memory cleaner with automatic cleanup, real-time monitoring, and a lightweight interface.**

[English](#english) · [Русский](#russian)

<sub>Version 1.0.0 · Created by lordofsunshine · MIT License</sub>

</div>

---

<a id="english"></a>

## 🇬🇧 English

The full version of MemCleaner built with WPF and .NET 10. It features a dark interface with green accents, animated memory statistics and a chart, automatic cleanup, history, a global hotkey, system tray integration, and language switching.

## ⚙️ Requirements

- Windows 10 version 1809 or newer, or Windows 11;
- 64-bit operating system;
- Administrator privileges. The application requests them automatically on first launch;
- No separate .NET installation is required. The runtime is bundled with the application.

## 📥 Installation

Run the installer:

```text
installer/MemCleaner-Setup-1.0.0.0.exe
```

The installer places the application in:

```text
Program Files\memcleaner
```

It also creates shortcuts and a startup scheduled task, but the task is created only if you enable automatic startup in the application settings.

**Important:** Do not move `MemCleaner.exe` separately from the other application files. The WPF native libraries are stored alongside it, and the application will not start without them.

To create a portable copy, copy the entire installation directory.

## ✨ Features

- 📊 **Memory monitoring** — displays used and available memory, file cache, and committed memory.
- 🧹 **Memory cleanup** — frees process working sets, the standby list, modified pages, and the system file cache.
- ⚡ **Automatic cleanup** — supports cleanup based on a time interval, a memory usage threshold, or both conditions simultaneously.
- 📜 **Cleanup history** — records recent cleanup operations, including the time, trigger reason, and amount of memory freed.
- 🚫 **Process exclusions** — exclude processes that should not be affected by cleanup.
- 🚀 **Automatic startup** — launches with Windows through a scheduled task with the required privileges and immediately minimizes to the system tray.
- ⌨️ **Global hotkey** — start memory cleanup from any application using `Ctrl+Alt+M`.
- 🌐 **Language selection** — system language, English, or Russian. By default, the application uses the system language: Russian for CIS countries and English for other regions.

## 🌐 Localization

All interface texts are stored in separate JSON files:

```text
src/MemCleaner/Localization/Lang/en.json
src/MemCleaner/Localization/Lang/ru.json
```

Both files are embedded into the application as resources.

The language selector is available in the settings. The selected language is saved and applied immediately, without restarting the application.

If a language file is missing or corrupted, the application automatically falls back to English.

## 🔨 Building from Source

**Requirements:**

- .NET SDK 10;
- Windows.

### Publishing the Application

```powershell
dotnet publish source/src/MemCleaner/MemCleaner.csproj -c Release -o source/publish
```

The output consists of a single `MemCleaner.exe` and four native libraries stored alongside it.

Publishing settings are configured in `MemCleaner.csproj`:

- Self-contained deployment;
- Single-file publishing;
- Native libraries are not extracted to a temporary directory.

### Building the Installer

The installer is built using Inno Setup 6 from the project directory:

```powershell
iscc source/installer/memcleaner.iss
```

The resulting installer will appear in the `installer` directory next to this README.

## 📁 Project Structure

| Directory | Description |
|:----------|:------------|
| `source/src/MemCleaner` | Application source: services, models, ViewModels, controls, themes, and localization. |
| `source/installer` | Installer project. |
| `source/tools/make-icon.ps1` | Builds the application icon and image from the original logo. |
| `installer` | Compiled installer. |

## ⚠️ Limitations

- Memory cleanup does not increase physical RAM. It only frees memory pages that Windows was keeping available for reuse. As a result, some data may need to be read from disk again after cleanup.
- The amount of freed memory is estimated from the difference in available memory and is therefore approximate.
- The application is not signed with a code-signing certificate. SmartScreen may display a warning on the first launch.

## 📄 License

MIT License.

**Author:** lordofsunshine.

The full license text is available in the `LICENSE` file.

---

<a id="russian"></a>

<details>
<summary><strong>🇷🇺 Русская версия</strong></summary>

# 🧹 MemCleaner для Windows 10 и 11

Полная версия программы на WPF и .NET 10. Тёмный интерфейс с зелёным акцентом, плавные цифры и диаграмма, автоматическая очистка, журнал, горячая клавиша, трей и переключение языка.

## ⚙️ Требования

- Windows 10 версии 1809 или новее, Windows 11;
- 64-битная система;
- права администратора. Программа запросит их сама при первом запуске;
- установленный .NET не нужен, среда выполнения уже включена в приложение.

## 📥 Установка

Запустите установщик:

```text
installer/MemCleaner-Setup-1.0.0.0.exe
```

Установщик помещает программу в:

```text
Program Files\memcleaner
```

Также он создаёт ярлыки и задачу автозапуска, но задача создаётся только в том случае, если вы включите автозапуск в настройках программы.

**Важно:** нельзя переносить `MemCleaner.exe` отдельно от остальных файлов. Рядом с ним находятся нативные библиотеки WPF, без которых программа не запустится.

Если нужна портативная копия, скопируйте всю папку установки.

## ✨ Возможности

- 📊 **Мониторинг памяти** — показывает занятую и свободную память, файловый кэш и выделенную память.
- 🧹 **Очистка памяти** — освобождает рабочие наборы процессов, список ожидания, изменённые страницы и системный файловый кэш.
- ⚡ **Автоматическая очистка** — по интервалу, по порогу занятой памяти или по обоим условиям сразу.
- 📜 **Журнал очисток** — сохраняет последние операции с указанием времени, причины запуска и объёма освобождённой памяти.
- 🚫 **Исключения процессов** — позволяет исключить процессы, которые нельзя затрагивать при очистке.
- 🚀 **Автозапуск** — запускается вместе с Windows через задачу планировщика с необходимыми правами и сразу сворачивается в трей.
- ⌨️ **Глобальная горячая клавиша** — запуск очистки памяти из любого приложения через `Ctrl+Alt+M`.
- 🌐 **Переключение языка** — системный, английский или русский. По умолчанию язык берётся из системы: для стран СНГ выбирается русский, для остальных регионов английский.

## 🌐 Локализация

Все тексты находятся в отдельных JSON-файлах:

```text
src/MemCleaner/Localization/Lang/en.json
src/MemCleaner/Localization/Lang/ru.json
```

Оба файла встроены в сборку как ресурсы.

В настройках есть переключатель языка. Выбор сохраняется и применяется сразу, без перезапуска программы.

Если файл языка не найден или повреждён, программа автоматически переключается на английский.

## 🔨 Сборка из исходников

**Требования:**

- .NET SDK 10;
- Windows.

### Публикация приложения

```powershell
dotnet publish source/src/MemCleaner/MemCleaner.csproj -c Release -o source/publish
```

В результате получается один `MemCleaner.exe` и четыре нативные библиотеки рядом с ним.

Настройки публикации находятся в `MemCleaner.csproj`:

- Self-contained;
- Single file;
- Без распаковки нативных библиотек во временную папку.

### Сборка установщика

Установщик собирается с помощью Inno Setup 6 из корня папки проекта:

```powershell
iscc source/installer/memcleaner.iss
```

Готовый установщик появится в папке `installer` рядом с этим README.

## 📁 Структура проекта

| Папка | Описание |
|:------|:--------|
| `source/src/MemCleaner` | Исходный код приложения: службы, модели, ViewModel, контролы, тема и локализация. |
| `source/installer` | Проект установщика. |
| `source/tools/make-icon.ps1` | Создание иконки и изображения из исходного логотипа. |
| `installer` | Готовый установщик. |

## ⚠️ Ограничения

- Очистка не увеличивает физическую оперативную память. Она лишь освобождает страницы, которые Windows держала про запас. Поэтому после очистки часть данных снова может считываться с диска.
- Объём освобождённой памяти рассчитывается по разнице доступной памяти и остаётся приблизительным.
- Программа не подписана сертификатом, поэтому SmartScreen может показать предупреждение при первом запуске.

## 📄 Лицензия

MIT.

**Автор:** lordofsunshine.

Полный текст лицензии находится в файле `LICENSE`.

</details>