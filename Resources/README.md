# VS.Helper

**VS.Helper** — расширение для Visual Studio с командами для быстрой работы с проектом: комментарии с путём файла, сборка ZIP по новой схеме, Git-синхронизация, вспомогательные операции и экспериментальные AI/Swarm-команды.

## Главное меню

После установки расширения в верхнем меню Visual Studio появляется отдельный пункт:

```text
VS.Helper
├─ Build Zip
├─ Create Zip Config
├─ Self Upgrade
├─ Run Swarm
├─ Evolve Swarm
├─ Commit Stamp Sync Git
└─ Tools
```

Команды больше не должны жить россыпью в системном меню Tools.

## Build Zip

`Build Zip` работает через новый ZIP-движок и читает конфиг `VS.Helper.Zip.xml`, который лежит рядом с solution.

Текущая рабочая схема:

```text
Build Zip
├─ найти открытый Solution
├─ НЕ менять версию в source.extension.vsixmanifest
├─ собрать список файлов по VS.Helper.Zip.xml
├─ добавить project-closure из StartProject
├─ применить Exclude
├─ записать _VS.Helper.ZipManifest.txt
├─ создать VS.Helper.zip
├─ скопировать ZIP в буфер как file-drop
└─ открыть браузер / ChatGPT для передачи архива дальше
```

## VS.Helper.Zip.xml

Минимальный рабочий пример:

```xml
<?xml version="1.0" encoding="utf-8"?>
<VSHelperZip>
  <Root>.</Root>
  <OutputDir>_zip</OutputDir>
  <ArchiveName>VS.Helper.zip</ArchiveName>
  <StartProject>VS.Helper.csproj</StartProject>
  <IncludeProjectClosure>true</IncludeProjectClosure>
  <IncludeSolutionFiles>true</IncludeSolutionFiles>
  <IncludeManifest>true</IncludeManifest>
  <Include>
    <File>VS.Helper.slnx</File>
    <File>VS.Helper.csproj</File>
    <File>README.md</File>
    <File>LICENSE.txt</File>
    <File>AI/**/*.*</File>
    <File>Commands/**/*.*</File>
    <File>Core/**/*.*</File>
    <File>Helpers/**/*.*</File>
    <File>Resources/**/*.*</File>
    <File>source.extension.vsixmanifest</File>
    <File>VSCommandTable.vsct</File>
    <File>*.cs</File>
  </Include>
  <Exclude>
    <Pattern>**/bin/**</Pattern>
    <Pattern>**/obj/**</Pattern>
    <Pattern>**/.vs/**</Pattern>
    <Pattern>**/.git/**</Pattern>
    <Pattern>**/_zip/**</Pattern>
    <Pattern>*.zip</Pattern>
    <Pattern>*.vsix</Pattern>
    <Pattern>*.pdb</Pattern>
    <Pattern>*.user</Pattern>
    <Pattern>*.suo</Pattern>
    <Pattern>*.log</Pattern>
  </Exclude>
</VSHelperZip>
```

## Версионирование VSIX

`Build Zip` не меняет версию в `source.extension.vsixmanifest`. Версию инкрементирует только `Self Upgrade`.

Пример:

```text
2026.2.1.11 → 2026.2.1.12
```

Это нужно, чтобы Visual Studio видела новый VSIX как обновление, а не показывала сообщение:

```text
Это расширение уже установлено для всех возможных продуктов.
```

## Create Zip Config

Команда создаёт или перезаписывает `VS.Helper.Zip.xml` с новой схемой. По умолчанию архив называется:

```text
VS.Helper.zip
```

## Commit Stamp Sync Git

Команда выполняет:

```text
git add -A
git commit -m "SolutionName yyyy-MM-dd HH:mm:ss"
git pull --rebase
git push
```

GitHub token хранится в `VS.Helper.Zip.xml`. Обычный `<Token>` после первого использования должен быть перенесён в защищённый `<TokenProtected>` через Windows DPAPI.

## Tools

Вспомогательные операции:

- удаление пустых строк;
- удаление `#region` / `#endregion`;
- поиск и замена;
- сбор namespace / using;
- удаление `.bak`;
- удаление файлов вне `.csproj`;
- синхронизация проекта с образцом;
- конвертация старого `.csproj` в SDK-style;
- добавление комментария с относительным путём файла.

Перед разрушительными операциями используй Dry Run.

## Если VSIX не обновляется

1. Убедись, что версия в `source.extension.vsixmanifest` реально выросла.
2. Закрой Visual Studio.
3. Удали старую версию через `Extensions → Manage Extensions`.
4. Очисти `bin`, `obj`, `.vs`.
5. Пересобери VSIX.
6. Установи новый VSIX.

Если версия не менялась, Visual Studio справедливо считает пакет уже установленным.

## Установка VSIX и обновление версии

Если установщик VSIX пишет: **«Это расширение уже установлено для всех возможных продуктов»**, значит Visual Studio видит тот же `Identity Id` и ту же или не пересобранную версию пакета.

Правильный цикл:

```text
1. VS.Helper → Self Upgrade, если нужно поднять версию и собрать новый VSIX
2. Проверить строку VSIX version: старая → новая
3. Закрыть все окна Visual Studio
4. Установить свежий .vsix из bin\Debug или bin\Release

Для передачи проекта в ChatGPT отдельно используй `VS.Helper → Build Zip`: он создаёт `VS.Helper.zip`, но версию не трогает.
```

В проекте `CreateVsixContainer` должен быть `True`, иначе новый VSIX-контейнер не будет создаваться при сборке.



## Patch 46
- Build Zip: больше не открывает новую вкладку ChatGPT по URL, только поднимает браузер.
- Create Zip Config: после создания сразу открывает VS.Helper.Zip.xml для редактирования.
- Self Upgrade: инкрементирует версию, сохраняет файлы и запускает сборку без блокировки UI.


## Patch 52
- Self Upgrade now bumps only VSIX Identity Version, not InstallationTarget/Prerequisite versions.
- Self Upgrade builds detached and launches VSIXInstaller.exe for the produced .vsix.
- Build Zip and Create Zip Config behavior preserved.


## VS.Helper 53 Self Upgrade VSIX selection fix

- Fixed `VersionBumpEngine.ReplaceVsixIdentityVersion`: replacement now uses a MatchEvaluator, so `$1` is not accidentally parsed together with the new numeric version.
- Self Upgrade now deletes old VSIX files from `bin`, `obj`, and solution root before building.
- Self Upgrade now selects the freshly built VSIX only from `bin`, avoiding stale packages from old archives/folders.
- Installer launch remains detached and non-blocking for Visual Studio.

## Self Upgrade v56

Self Upgrade работает по схеме install-only:

1. Инкрементирует только `Identity Version` в `source.extension.vsixmanifest`.
2. Чистит старые `.vsix` в `bin/obj`.
3. Собирает свежий `.vsix`.
4. Проверяет `Identity Version` внутри готового `.vsix`.
5. Ждёт закрытия `devenv.exe`.
6. Запускает `VSIXInstaller.exe` только на свежий пакет.

`/uninstall` не используется.
Лог: `_VS.Helper.SelfUpgrade.log`.
