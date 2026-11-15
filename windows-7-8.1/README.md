<div align="center">

<img src="https://i.postimg.cc/8Pr1ThnQ/image.png" alt="MemCleaner Banner" width="100%">

# 🧹 MemCleaner — Windows 7 / 8.1

**A lightweight memory cleaner for Windows 7 and Windows 8.1.**

[English](#english) · [Русский](#russian)

<sub>Version 1.0.0 · Created by lordofsunshine · MIT License</sub>

</div>

---

<a id="english"></a>

## 🇬🇧 English

A separate WinForms build based on .NET Framework 3.5. The modern .NET 10 version cannot run on these systems, so a lightweight alternative has been created for Windows 7 and 8.1, featuring the same memory cleanup capabilities and language switching.

## ⚙️ Requirements

- Windows 7 SP1 or Windows 8.1;
- .NET Framework 3.5, which is included with these Windows versions and is usually already enabled;
- Administrator privileges.

If Windows asks you to enable the .NET Framework 3.5 component, accept the request. Otherwise, the application will not start.

## 📥 Installation

Run:

```text
installer/MemCleaner-Legacy-7-8.1-Setup-1.0.0.0.exe
```

The installer will not allow installation on Windows 10 or Windows 11. Separate builds for these systems are available in their respective directories.

## ✨ Available Features

- 🧹 **Four memory cleanup areas** — process working sets, the standby list, modified pages, and the system file cache.
- ⚡ **Automatic cleanup** — based on a time interval, memory usage threshold, or both conditions.
- ⏱️ **Cleanup control** — configurable delay between cleanup operations and process exclusions.
- 📜 **Cleanup history** — keeps a record of the last six cleanup operations.
- 🔔 **System tray and notifications** — minimizes to the system tray and displays notifications after automatic cleanup.
- 🚀 **Automatic startup** — uses an elevated Task Scheduler task. The application window does not open when you sign in to Windows.
- 🌐 **Language selection** — system language, English, or Russian, with the selected language saved.

### Differences from the Windows 10 / 11 Version

The following features are not available compared to the modern version:

- Smooth chart displaying memory usage over the last 90 seconds.
- Global `Ctrl+Alt+M` hotkey.
- Modern WPF interface.
- Detailed tooltips.

The numbers displayed in the overview are animated in this version as well.

## 🔨 Building from Source

The .NET SDK is required. The .NET Framework 3.5 reference assemblies package is restored automatically.

```powershell
dotnet build source/MemCleanerLegacy/MemCleanerLegacy.csproj -c Release
```

### Building the Installer

The installer is built using Inno Setup 6 from the project directory:

```powershell
iscc source/installer/legacy-win7.iss
```

The resulting installer will appear in the `installer` directory next to this README.

## 🧰 Project Structure

| File | Description |
|:-----|:------------|
| `Program.cs` | Application startup, single-instance check, and unhandled exception handling. |
| `MainForm.cs` | Interface, settings, history, system tray, and automatic cleanup. |
| `MemoryEngine.cs` | Memory reading, cleanup operations, and privileges. |
| `Platform.cs` | Windows version detection and available feature detection. |
| `L.cs` and `Lang/` | Localization, including `en.ini` and `ru.ini`. |
| `LegacySettings.cs` and `HistoryStore.cs` | Settings and history stored in `%APPDATA%\memcleaner`. |
| `Autostart.cs` | Task Scheduler on Windows Vista and newer, `Run` registry key on Windows XP. |
| `Smooth.cs` and `ProgressBarEx.cs` | Animated values and progress bar. |

## ⚠️ Limitations

- The interface follows the classic WinForms style, without WPF-style transparency and rounded corners.
- The amount of freed memory is estimated from the difference in available memory and remains approximate.
- The application is not signed with a code-signing certificate. SmartScreen may display a warning on the first launch.

## 📄 License

MIT License.

**Author:** lordofsunshine.

The full license text is available in the `LICENSE` file.

---

<a id="russian"></a>

<details>
<summary><strong>🇷🇺 Русская версия</strong></summary>

## 🧹 MemCleaner для Windows 7 и 8.1

Отдельная сборка на WinForms под .NET Framework 3.5. Современная версия на .NET 10 здесь не запустится, поэтому для Windows 7 и 8.1 сделан облегчённый вариант с тем же набором функций очистки и переключением языка.

## ⚙️ Требования

- Windows 7 SP1 или Windows 8.1;
- .NET Framework 3.5, на этих системах он входит в состав Windows и обычно уже включён;
- права администратора.

Если Windows предложит включить компонент .NET Framework 3.5, согласитесь, иначе программа не запустится.

## 📥 Установка

Запустите установщик:

```text
installer/MemCleaner-Legacy-7-8.1-Setup-1.0.0.0.exe
```

Установщик не даст поставить сборку на Windows 10 и Windows 11. Для них есть отдельная папка.

## ✨ Что доступно

- 🧹 **Четыре области очистки памяти** — рабочие наборы процессов, список ожидания, изменённые страницы и системный файловый кэш.
- ⚡ **Автоматическая очистка** — по интервалу, по порогу памяти или по обоим условиям.
- ⏱️ **Управление очисткой** — пауза между срабатываниями и исключения для процессов.
- 📜 **Журнал очисток** — последние шесть операций очистки.
- 🔔 **Системный трей и уведомления** — сворачивание в трей и уведомления после автоматической очистки.
- 🚀 **Автозапуск** — через задачу планировщика с повышенными правами. При входе в систему окно программы не открывается.
- 🌐 **Выбор языка** — системный, английский или русский с сохранением выбора.

### Отличия от версии для Windows 10 / 11

По сравнению с современной версией здесь отсутствуют:

- Плавная диаграмма последних полутора минут.
- Глобальная горячая клавиша `Ctrl+Alt+M`.
- Современное оформление WPF.
- Подробные подсказки.

Цифры в обзоре анимированы и здесь.

## 🔨 Сборка из исходников

Для сборки нужен .NET SDK. Пакет ссылочных сборок .NET Framework 3.5 подтянется автоматически.

```powershell
dotnet build source/MemCleanerLegacy/MemCleanerLegacy.csproj -c Release
```

### Сборка установщика

Установщик собирается с помощью Inno Setup 6 из корня папки проекта:

```powershell
iscc source/installer/legacy-win7.iss
```

Готовый установщик появится в папке `installer` рядом с этим README.

## 🧰 Структура программы

| Файл | Назначение |
|:-----|:----------|
| `Program.cs` | Запуск, проверка единственного экземпляра и обработка необработанных ошибок. |
| `MainForm.cs` | Интерфейс, настройки, журнал, трей и автоматическая очистка. |
| `MemoryEngine.cs` | Чтение памяти, очистка областей и привилегии. |
| `Platform.cs` | Определение версии Windows и доступных возможностей. |
| `L.cs` и `Lang/` | Локализация, файлы `en.ini` и `ru.ini`. |
| `LegacySettings.cs` и `HistoryStore.cs` | Настройки и журнал в `%APPDATA%\memcleaner`. |
| `Autostart.cs` | Задача планировщика на Vista и новее, ключ `Run` на XP. |
| `Smooth.cs` и `ProgressBarEx.cs` | Плавные цифры и полоса заполнения. |

## ⚠️ Ограничения

- Оформление ближе к классическому WinForms, без прозрачности и скруглений WPF.
- Освобождённый объём считается по разнице доступной памяти и остаётся приблизительным.
- Программа не подписана сертификатом, поэтому SmartScreen может предупредить при первом запуске.

## 📄 Лицензия

MIT.

**Автор:** lordofsunshine.

Полный текст лицензии находится в файле `LICENSE`.

</details>