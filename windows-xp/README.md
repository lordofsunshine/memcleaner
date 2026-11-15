<div align="center">

<img src="https://i.postimg.cc/8Pr1ThnQ/image.png" alt="MemCleaner Banner" width="100%">

# 🧹 MemCleaner — Windows XP

**A lightweight memory cleaner for Windows XP SP3.**

[⬇️ Download from GitHub Releases](https://github.com/lordofsunshine/memcleaner/releases)

[English](#english) · [Русский](#russian)

<sub>Version 1.0.0 · Created by lordofsunshine · MIT License</sub>

</div>

---

<a id="english"></a>

## 🇬🇧 English

The oldest and most limited MemCleaner build.

Windows XP does not support memory list cleanup and system file cache management in the same way as Windows Vista and newer systems. Therefore, this version focuses on the essential and supported functionality: process working-set cleanup.

## ⚙️ Requirements

- Windows XP with Service Pack 3 (SP3), 32-bit edition;
- .NET Framework 3.5 with Service Pack 1. It is not included with Windows XP and must be installed separately;
- Administrator privileges;
- At least 512 MB of RAM. Otherwise, there may be very little memory to free.

The application will not start without .NET Framework 3.5.

The Microsoft .NET Framework 3.5 installer requires Windows XP SP3 and can only be installed after the service pack is installed.

## 📥 Installation

Download the installer from [GitHub Releases](https://github.com/lordofsunshine/memcleaner/releases).

Run:

```text
installer/MemCleaner-Legacy-XP-Setup-1.0.0.0.exe
```

The installer is built using Inno Setup 5 because Inno Setup 6 cannot create installers for Windows XP.

It checks the Windows version and prevents installation on Windows Vista and newer systems.

Both the installer and the application support English and Russian interface languages.

## ✨ Available Features

- 🧹 **Process working-set cleanup** — uses `EmptyWorkingSet`, the only memory cleanup area supported by this Windows XP build.
- 🚫 **Process exclusions** — exclude individual processes to avoid affecting important applications.
- ⚡ **Automatic cleanup** — based on a time interval, memory usage threshold, or both conditions, with a configurable delay between cleanup operations.
- 📜 **Cleanup history** — keeps a record of the last six cleanup operations.
- 🔔 **System tray and notifications** — system tray integration, notifications, and automatic startup through the current user's `Run` registry key.
- 🌐 **Language selection** — system language, English, or Russian.

## 🚫 Unavailable Features and Why

Windows XP has several limitations that prevent certain memory cleanup operations and application features from working:

- **Standby list and modified pages:** unavailable because `NtSetSystemInformation` commands for these operations were introduced in Windows Vista.
- **System file cache:** unavailable because `SetSystemFileCacheSize` is supported on Windows XP only in the 64-bit edition, while this build is 32-bit.
- **Elevated Task Scheduler startup:** unavailable because Windows XP does not support running scheduled tasks with the highest privileges. Automatic startup therefore uses the current user's `Run` registry key.
- **Modern WPF interface and memory chart:** not included because this type of build would be too demanding for Windows XP.
- **Global `Ctrl+Alt+M` hotkey:** unavailable. This feature is only included in the Windows 10 / 11 version.

Unavailable memory cleanup areas are marked as unavailable and disabled in the interface. They cannot be enabled, preventing the application from offering functionality that the operating system cannot perform.

## 🔨 Building from Source

The .NET SDK is required. The .NET Framework 3.5 reference assemblies package is restored automatically during the build.

### Building the Application

```powershell
dotnet build source/MemCleanerLegacy/MemCleanerLegacy.csproj -c Release
```

### Building the Installer

The installer is built using Inno Setup 5 from the project directory:

```powershell
iscc source/installer/legacy-xp.iss
```

**Important:** Inno Setup 6 is not suitable for Windows XP because it cannot create installers that run on this operating system.

The resulting installer will appear in the `installer` directory.

## ⚠️ Limitations

- The interface uses classic WinForms styling, without rounded corners or transparency.
- The amount of freed memory is approximate and is calculated from the difference in available memory.
- The application is not signed with a code-signing certificate.
- On very old computers, working-set cleanup may noticeably slow down returning to applications that have been minimized for a long time, as their data needs to be read from disk again.

## 📄 License

MIT License.

**Author:** lordofsunshine.

The full license text is available in the `LICENSE` file.

---

<a id="russian"></a>

<details>
<summary><strong>🇷🇺 Русская версия</strong></summary>

# 🧹 MemCleaner для Windows XP

Самая старая и самая урезанная сборка.

Windows XP не умеет очищать списки памяти и системный файловый кэш так, как это делают Vista и более новые системы, поэтому здесь остаётся главное и безопасное: очистка рабочих наборов процессов.

## ⚙️ Требования

- Windows XP с пакетом обновления SP3, 32-битная редакция;
- .NET Framework 3.5 с пакетом обновления SP1. В XP его нет, поэтому его нужно установить отдельно;
- права администратора;
- не менее 512 МБ оперативной памяти, иначе освобождать будет почти нечего.

Без .NET Framework 3.5 программа не запустится.

Установщик Microsoft для .NET Framework 3.5 требует Windows XP SP3 и устанавливается только после него.

## 📥 Установка

Скачать установщик можно на странице [GitHub Releases](https://github.com/lordofsunshine/memcleaner/releases).

Запустите:

```text
installer/MemCleaner-Legacy-XP-Setup-1.0.0.0.exe
```

Установщик собран в Inno Setup 5, потому что Inno Setup 6 не создаёт установщики для Windows XP.

Он проверяет версию системы и не даст поставить программу на Windows Vista и новее.

Установщик и сама программа поддерживают русский и английский языки интерфейса.

## ✨ Что доступно

- 🧹 **Очистка рабочих наборов процессов** — через `EmptyWorkingSet`, единственная область очистки памяти, которую поддерживает эта сборка для Windows XP.
- 🚫 **Исключения процессов** — позволяют не затрагивать нужные приложения.
- ⚡ **Автоматическая очистка** — по интервалу, порогу памяти или обоим условиям, с паузой между срабатываниями.
- 📜 **Журнал очисток** — последние шесть операций очистки.
- 🔔 **Трей и уведомления** — работа в системном трее, уведомления и автозапуск через ключ `Run` текущего пользователя.
- 🌐 **Переключение языка** — системный, английский или русский.

## 🚫 Чего нет и почему

В Windows XP есть ограничения, из-за которых некоторые операции очистки памяти и функции программы недоступны:

- **Список ожидания и изменённые страницы:** отсутствуют, поскольку команды `NtSetSystemInformation` для этих операций появились только в Windows Vista.
- **Системный файловый кэш:** недоступен, поскольку `SetSystemFileCacheSize` работает в XP только в 64-битной редакции, а эта сборка 32-битная.
- **Автозапуск через задачу планировщика с повышенными правами:** недоступен, поскольку XP не умеет запускать задачи с наивысшими правами. Поэтому автозапуск идёт через ключ `Run` текущего пользователя.
- **Современный интерфейс WPF и диаграмма памяти:** отсутствуют, поскольку такая сборка слишком тяжёлая для Windows XP.
- **Глобальная горячая клавиша `Ctrl+Alt+M`:** отсутствует, она доступна только в версии для Windows 10 и 11.

В интерфейсе недоступные области помечены как недоступные и выключены. Включить их нельзя, чтобы программа не обещала того, чего система не сможет выполнить.

## 🔨 Сборка из исходников

Нужен .NET SDK. Пакет ссылочных сборок .NET Framework 3.5 устанавливается автоматически при сборке.

### Сборка приложения

```powershell
dotnet build source/MemCleanerLegacy/MemCleanerLegacy.csproj -c Release
```

### Сборка установщика

Установщик собирается с помощью Inno Setup 5 из корня папки проекта:

```powershell
iscc source/installer/legacy-xp.iss
```

**Важно:** Inno Setup 6 для XP не подходит, поскольку не создаёт установщики, работающие на этой системе.

Готовый установщик появится в папке `installer`.

## ⚠️ Ограничения

- Оформление классическое WinForms, без скруглений и прозрачности.
- Освобождённый объём приблизительный и рассчитывается по разнице доступной памяти.
- Программа не подписана сертификатом.
- На очень старых компьютерах очистка рабочих наборов может заметно замедлить возврат к давно свёрнутым программам, поскольку их данные снова считываются с диска.

## 📄 Лицензия

MIT.

**Автор:** lordofsunshine.

Полный текст лицензии находится в файле `LICENSE`.

</details>