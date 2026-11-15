<div align="center">

<img src="https://i.postimg.cc/8Pr1ThnQ/image.png" alt="MemCleaner Banner" width="100%">

# 🧹 MemCleaner — Windows Vista

**A lightweight memory cleaner for Windows Vista SP2.**

[English](#english) · [Русский](#russian)

<sub>Version 1.0.0 · Created by lordofsunshine · MIT License</sub>

</div>

---

<a id="english"></a>

## 🇬🇧 English

A WinForms build based on .NET Framework 3.5 for Windows Vista SP2.

Windows Vista supports standby lists, modified pages, and the system file cache. Therefore, all four memory cleanup areas are available in this version, although the interface is simpler than the Windows 10 / 11 version.

## ⚙️ Requirements

- Windows Vista with Service Pack 2 (SP2);
- .NET Framework 3.5. Windows Vista only includes .NET Framework 3.0, so version 3.5 must be installed separately;
- Administrator privileges.

.NET Framework 3.5 can be installed through Windows Update or using a separate Microsoft installer.

The application will not start without it because it is specifically built for .NET Framework 3.5.

## 📥 Installation

Run the installer:

```text
installer/MemCleaner-Legacy-Vista-Setup-1.0.0.0.exe
```

The installer is built using Inno Setup 5 because installers created with Inno Setup 6 do not work on Windows Vista.

It checks the operating system version and prevents installation on Windows 7 and newer systems. Separate builds are available for those versions of Windows.

## ✨ Available Features

- 🧹 **Four memory cleanup areas** — process working sets, the standby list, modified pages, and the system file cache.
- ⚡ **Automatic cleanup** — based on a time interval, memory usage threshold, or both conditions, with a configurable delay between cleanup operations.
- 🚫 **Process exclusions** — exclude individual processes from cleanup.
- 📜 **Cleanup history** — keeps a record of the last six cleanup operations.
- 🔔 **System tray and notifications** — system tray integration, notifications, and automatic startup through an elevated Task Scheduler task.
- 🌐 **Language selection** — system language, English, or Russian.

### Differences from the Windows 10 / 11 Version

The following features are not available:

- Modern WPF interface.
- Memory usage chart displaying the last 90 seconds.
- Global `Ctrl+Alt+M` hotkey.

## 🔨 Building from Source

### Building the Application

```powershell
dotnet build source/MemCleanerLegacy/MemCleanerLegacy.csproj -c Release
```

### Building the Installer

The installer is built using Inno Setup 5 from the project directory:

```powershell
iscc source/installer/legacy-vista.iss
```

**Important:** Inno Setup 5 must be used. Version 6 creates installers that cannot run on Windows Vista.

The resulting installer will appear in the `installer` directory.

## 🖥️ Windows Vista Specifics

- 🧠 **Full memory cleanup support** — Windows Vista supports `NtSetSystemInformation` for the standby list and modified pages, as well as `SetSystemFileCacheSize` for the system file cache. Therefore, the interface does not mark any of the four cleanup areas as unavailable.
- 🔐 **User Account Control (UAC)** — UAC is enabled in Windows Vista, so the application requests elevated privileges on startup. Automatic startup through Task Scheduler prevents an unnecessary application window from appearing when signing in.
- 🌐 **Firewall notification** — on the first launch after installation, Windows may ask for permission to access the network. The application does not need network access; this is a standard Windows Firewall prompt for applications with a manifest.

## ⚠️ Limitations

- The interface uses classic WinForms styling without a smooth memory chart.
- The amount of freed memory is approximate and is calculated from the difference in available memory.
- The application is not signed with a code-signing certificate, so Windows may display a security warning.

## 📄 License

MIT License.

**Author:** lordofsunshine.

The full license text is available in the `LICENSE` file.

---

<a id="russian"></a>

<details>
<summary><strong>🇷🇺 Русская версия</strong></summary>

# 🧹 MemCleaner для Windows Vista

Сборка на WinForms под .NET Framework 3.5 для Windows Vista SP2.

Vista умеет работать со списками памяти и системным файловым кэшем, поэтому здесь доступны все четыре области очистки, но интерфейс проще, чем в версии для Windows 10 и 11.

## ⚙️ Требования

- Windows Vista с пакетом обновления SP2;
- .NET Framework 3.5. В Vista встроен только .NET Framework 3.0, поэтому версию 3.5 нужно установить отдельно;
- права администратора.

Установить .NET Framework 3.5 можно через центр обновления Windows или отдельным установщиком Microsoft.

Без него программа не запустится, потому что собрана именно под .NET Framework 3.5.

## 📥 Установка

Запустите установщик:

```text
installer/MemCleaner-Legacy-Vista-Setup-1.0.0.0.exe
```

Установщик собран в Inno Setup 5, потому что установщики Inno Setup 6 не работают на Vista.

Он проверит версию системы и не даст поставить сборку на Windows 7 и новее. Для них есть отдельные папки.

## ✨ Что доступно

- 🧹 **Четыре области очистки памяти** — рабочие наборы процессов, список ожидания, изменённые страницы и системный файловый кэш.
- ⚡ **Автоматическая очистка** — по интервалу, порогу памяти или обоим условиям, с паузой между срабатываниями.
- 🚫 **Исключения процессов** — возможность исключить отдельные процессы из очистки.
- 📜 **Журнал очисток** — последние шесть операций очистки.
- 🔔 **Трей и уведомления** — работа в системном трее, уведомления и автозапуск через задачу планировщика с повышенными правами.
- 🌐 **Переключение языка** — системный, английский или русский.

### Отличия от версии для Windows 10 / 11

Здесь отсутствуют:

- Современный интерфейс WPF.
- Диаграмма использования памяти за последние полторы минуты.
- Глобальная горячая клавиша `Ctrl+Alt+M`.

## 🔨 Сборка из исходников

### Сборка приложения

```powershell
dotnet build source/MemCleanerLegacy/MemCleanerLegacy.csproj -c Release
```

### Сборка установщика

Установщик собирается с помощью Inno Setup 5 из корня папки проекта:

```powershell
iscc source/installer/legacy-vista.iss
```

**Важно:** необходимо использовать именно Inno Setup 5. Шестая версия создаёт установщик, который не запускается на Windows Vista.

Готовый установщик появится в папке `installer`.

## 🖥️ Особенности Windows Vista

- 🧠 **Полная поддержка очистки памяти** — Vista поддерживает `NtSetSystemInformation` для списка ожидания и изменённых страниц, а также `SetSystemFileCacheSize` для системного файлового кэша. Поэтому в интерфейсе нет пометок о недоступных областях очистки.
- 🔐 **Контроль учётных записей (UAC)** — в Windows Vista включён UAC, поэтому при запуске программа запросит повышение прав. Автозапуск через планировщик избавляет от лишнего окна при входе в систему.
- 🌐 **Уведомление брандмауэра** — при первом запуске после установки Windows может запросить разрешение на сетевой доступ. Программе он не нужен, это стандартное окно брандмауэра Windows для приложений с манифестом.

## ⚠️ Ограничения

- Интерфейс выполнен в классическом стиле WinForms, без плавной диаграммы памяти.
- Освобождённый объём приблизительный и рассчитывается по разнице доступной памяти.
- Программа не подписана сертификатом, поэтому Windows может показать предупреждение безопасности.

## 📄 Лицензия

MIT.

**Автор:** lordofsunshine.

Полный текст лицензии находится в файле `LICENSE`.

</details>