<div align="center">

<img src="https://i.postimg.cc/8Pr1ThnQ/image.png" alt="MemCleaner Banner" width="100%">

# 🧹 MemCleaner

**A lightweight memory cleaner for Windows with automatic cleanup and memory monitoring.**

[⬇️ Download for Windows XP, Vista, 7, 8.1, 10 and 11](https://github.com/lordofsunshine/memcleaner/releases)

[English](#english) · [Русский](#russian)

<sub>Version 1.0.0 · Created by lordofsunshine · MIT License</sub>

</div>

---

<a id="english"></a>

## 🇬🇧 English

MemCleaner is a Windows memory cleaner with a dark interface, automatic cleanup based on configurable intervals and memory usage thresholds, a cleanup history, and language switching.

**Version:** 1.0.0  
**Author:** lordofsunshine  
**License:** MIT

## ✨ Features

- 📊 **Memory monitoring** — displays RAM usage, available memory, file cache, and committed memory.
- 🧹 **Selective memory cleanup** — cleans selected memory areas: process working sets, the standby list, modified pages, and the system file cache.
- ⚡ **Automatic cleanup** — manually start cleanup or configure automatic cleanup based on a time interval and/or a memory usage threshold.
- 📜 **Cleanup history** — records cleanup results, including the time, trigger reason, and estimated amount of freed memory.
- 🚫 **Process exclusions** — exclude individual processes from cleanup.
- 🖥️ **Windows integration** — supports startup with Windows, system tray minimization, and the global `Ctrl+Alt+M` hotkey.
- 🎨 **Dark interface** — a dark UI with Russian, English, and system language options.

The available memory cleanup areas depend on the Windows version and application build. See **Memory Cleanup Feature Availability** below for details.

## 📁 Project Structure

| Directory | Supported Systems | Technology | Installer |
|:----------|:------------------|:-----------|:----------|
| `windows-10-11` | Windows 10 1809 and newer, Windows 11, x64 only | WPF, .NET 10 | `MemCleaner-Setup-1.0.0.0.exe` |
| `windows-7-8.1` | Windows 7 SP1, Windows 8.1 | WinForms, .NET Framework 3.5 | `MemCleaner-Legacy-7-8.1-Setup-1.0.0.0.exe` |
| `windows-vista` | Windows Vista SP2 | WinForms, .NET Framework 3.5 | `MemCleaner-Legacy-Vista-Setup-1.0.0.0.exe` |
| `windows-xp` | Windows XP SP3, 32-bit | WinForms, .NET Framework 3.5 | `MemCleaner-Legacy-XP-Setup-1.0.0.0.exe` |

The modern version requires .NET 10, which is not available and will not be available on older Windows systems.

That's why separate WinForms builds using .NET Framework 3.5 were created for XP, Vista, Windows 7, and Windows 8.1. They have a simpler interface but can run on systems where WPF on .NET 10 cannot work.

## 🧠 Memory Cleanup Feature Availability

Memory cleanup relies on Windows system capabilities, which differ between operating system versions:

| Memory Area | Windows Support |
|:------------|:----------------|
| Process working sets (`EmptyWorkingSet`) | Available on all supported systems, starting with Windows XP |
| Standby list and modified page list (`NtSetSystemInformation`) | Available starting with Windows Vista; not available on Windows XP |
| System file cache (`SetSystemFileCacheSize`) | Supported starting with Windows Vista; on Windows XP, available only in the 64-bit edition |
| Elevated Task Scheduler task | Available on Windows Vista and newer |

Additional details:

- On 32-bit Windows XP, the system file cache cleanup option is hidden.
- On Windows XP, automatic startup is configured through the current user's `Run` registry key instead of an elevated scheduled task.
- Legacy builds do not present unavailable cleanup areas as functional options. They are marked as unavailable and are never enabled.

This prevents users from enabling options that the operating system cannot execute.

## 📥 Installation

All application versions are available on the [GitHub Releases page](https://github.com/lordofsunshine/memcleaner/releases).

Choose the installer corresponding to your Windows version:

| Windows Version | Installer |
|:----------------|:----------|
| Windows 10 1809+ / Windows 11 (x64) | `MemCleaner-Setup-1.0.0.0.exe` |
| Windows 7 SP1 / Windows 8.1 | `MemCleaner-Legacy-7-8.1-Setup-1.0.0.0.exe` |
| Windows Vista SP2 | `MemCleaner-Legacy-Vista-Setup-1.0.0.0.exe` |
| Windows XP SP3 (32-bit) | `MemCleaner-Legacy-XP-Setup-1.0.0.0.exe` |

1. Open [GitHub Releases](https://github.com/lordofsunshine/memcleaner/releases).
2. Download the installer corresponding to your Windows version.
3. Run the installer.

The installer automatically checks the minimum supported Windows version and prevents installation on an incompatible system.

**.NET Framework 3.5 requirement for legacy builds:**

- Windows 7 and Windows 8.1: .NET Framework 3.5 is included with the system.
- Windows Vista and Windows XP: .NET Framework 3.5 must be installed separately.

Additional installation instructions are available in the README file inside each version directory.

## 🔨 Building from Source

### Modern Version

```powershell
dotnet publish windows-10-11/source/src/MemCleaner/MemCleaner.csproj -c Release -o publish
```

### Legacy Version

The same legacy source is used for Windows XP, Vista, Windows 7, and Windows 8.1.

```powershell
dotnet build windows-xp/source/MemCleanerLegacy/MemCleanerLegacy.csproj -c Release
```

### Build Requirements and Notes

- The legacy build requires the `Microsoft.NETFramework.ReferenceAssemblies.net35` package. It is restored automatically.
- Installers are built using Inno Setup.
- The modern version and the Windows 7 / 8.1 installer use Inno Setup 6.
- Windows XP and Vista installers use Inno Setup 5 because version 6 cannot create installers that run on those systems.

## 📄 License

MIT License.

**Author:** lordofsunshine

The full license text is available in the `LICENSE` file next to this README and inside each version directory.

---

<a id="russian"></a>

<details>
<summary><strong>🇷🇺 Русская версия</strong></summary>

<div align="center">

# 🧹 MemCleaner

**Очиститель оперативной памяти для Windows.**

</div>

## ✨ Возможности программы

- 📊 **Мониторинг памяти** — показывает использование оперативной памяти, доступную память, файловый кэш и выделенную память.
- 🧹 **Выборочная очистка памяти** — очищает выбранные области памяти: рабочие наборы процессов, список ожидания, изменённые страницы и системный файловый кэш.
- ⚡ **Автоматическая очистка** — позволяет запускать очистку вручную или автоматически через заданный интервал и/или при достижении порога занятой памяти.
- 📜 **Журнал очисток** — сохраняет результаты очистки с указанием времени, причины запуска и оценки освобождённой памяти.
- 🚫 **Исключения процессов** — позволяет исключить отдельные процессы из очистки.
- 🖥️ **Интеграция с Windows** — поддерживает запуск вместе с Windows, сворачивание в системный трей и глобальную горячую клавишу `Ctrl+Alt+M`.
- 🎨 **Тёмный интерфейс** — предлагает тёмную тему и русский, английский или системный язык.

Набор доступных областей очистки зависит от версии Windows и сборки. Подробнее — в разделе **«Доступность функций очистки памяти»** ниже.

## 📁 Структура проекта

| Папка | Поддерживаемые системы | Технология | Установщик |
|:------|:-----------------------|:-----------|:-----------|
| `windows-10-11` | Windows 10 1809 и новее, Windows 11, только x64 | WPF, .NET 10 | `MemCleaner-Setup-1.0.0.0.exe` |
| `windows-7-8.1` | Windows 7 SP1, Windows 8.1 | WinForms, .NET Framework 3.5 | `MemCleaner-Legacy-7-8.1-Setup-1.0.0.0.exe` |
| `windows-vista` | Windows Vista SP2 | WinForms, .NET Framework 3.5 | `MemCleaner-Legacy-Vista-Setup-1.0.0.0.exe` |
| `windows-xp` | Windows XP SP3, 32-бит | WinForms, .NET Framework 3.5 | `MemCleaner-Legacy-XP-Setup-1.0.0.0.exe` |

Современная версия требует .NET 10, которого нет и не будет на старых системах.

Поэтому для XP, Vista, Windows 7 и 8.1 сделаны отдельные варианты на WinForms под .NET Framework 3.5. Они выглядят проще, зато запускаются там, где WPF на .NET 10 физически не работает.

## 🧠 Доступность функций очистки памяти

Очистка памяти опирается на возможности самой Windows, которые отличаются в зависимости от версии системы:

| Область памяти | Поддержка Windows |
|:---------------|:------------------|
| Рабочие наборы процессов (`EmptyWorkingSet`) | Доступны на всех поддерживаемых системах, начиная с Windows XP |
| Список ожидания и список изменённых страниц (`NtSetSystemInformation`) | Появились в Windows Vista, в Windows XP отсутствуют |
| Системный файловый кэш (`SetSystemFileCacheSize`) | Поддерживается начиная с Windows Vista; в Windows XP доступен только в 64-битной редакции |
| Задача планировщика с повышенными правами | Доступна в Windows Vista и новее |

Дополнительные особенности:

- В 32-битной сборке для Windows XP область очистки системного файлового кэша скрыта.
- В Windows XP автозапуск сделан через ключ `Run` текущего пользователя вместо задачи планировщика с повышенными правами.
- В legacy-сборках недоступные области не показываются как рабочие функции. Они помечены как недоступные и никогда не включаются.

Так пользователь не сможет включить то, что система всё равно не выполнит.

## 📥 Установка

Все версии программы доступны на странице [GitHub Releases](https://github.com/lordofsunshine/memcleaner/releases).

Выберите установщик, соответствующий вашей версии Windows:

| Версия Windows | Установщик |
|:---------------|:-----------|
| Windows 10 1809+ / Windows 11 (x64) | `MemCleaner-Setup-1.0.0.0.exe` |
| Windows 7 SP1 / Windows 8.1 | `MemCleaner-Legacy-7-8.1-Setup-1.0.0.0.exe` |
| Windows Vista SP2 | `MemCleaner-Legacy-Vista-Setup-1.0.0.0.exe` |
| Windows XP SP3 (32-бит) | `MemCleaner-Legacy-XP-Setup-1.0.0.0.exe` |

1. Перейдите на страницу [GitHub Releases](https://github.com/lordofsunshine/memcleaner/releases).
2. Скачайте установщик для своей версии Windows.
3. Запустите установщик.

Установщик автоматически проверит минимальную версию системы и не позволит установить программу на несовместимую систему.

**Требование .NET Framework 3.5 для legacy-сборок:**

- Windows 7 и Windows 8.1: .NET Framework 3.5 входит в состав системы.
- Windows Vista и Windows XP: .NET Framework 3.5 необходимо установить отдельно.

Дополнительные инструкции по установке приведены в README каждой папки версии.

## 🔨 Сборка из исходников

### Современная версия

```powershell
dotnet publish windows-10-11/source/src/MemCleaner/MemCleaner.csproj -c Release -o publish
```

### Legacy-версия

Для Windows XP, Vista, Windows 7 и Windows 8.1 используется один и тот же исходный код legacy-версии.

```powershell
dotnet build windows-xp/source/MemCleanerLegacy/MemCleanerLegacy.csproj -c Release
```

### Требования и особенности сборки

- Для legacy-сборки требуется пакет `Microsoft.NETFramework.ReferenceAssemblies.net35`. Он подтягивается автоматически.
- Установщики собираются с помощью Inno Setup.
- Для современной версии и установщика Windows 7 / 8.1 используется Inno Setup 6.
- Для Windows XP и Vista используется Inno Setup 5, поскольку шестая версия не создаёт установщики, работающие на этих системах.

## 📄 Лицензия

MIT.

**Автор:** lordofsunshine.

Полный текст лицензии находится в файле `LICENSE` рядом с этим README и в каждой папке версии.

</details>
