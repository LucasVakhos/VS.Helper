# VS.Helper 59 Menu Registration Fix

Исправлено:

- `PackageIds.ProjectGenomeCommand` добавлен в `VSCommandTable.cs`.
- `ProjectGenomeCommand` уже есть в `VSCommandTable.vsct` и теперь совпадает с C# id `0x0490`.
- Версия VSIX поднята до `2026.2.1.59`, чтобы Visual Studio не оставляла старое меню.
- Удалён второй package entry point `VSExtensionPackage.cs`.
- Удалены snapshot/dump-папки `Resources/**`, `Helpers/Helpers/**`, `Helpers/Commands/**`, `Helpers/AI/**`, `Helpers/Core/**`.
- Добавлен реальный `Resources/Icon32.png`, потому что VSCT ссылался на него.
- В `.csproj` добавлены страховочные исключения от повторного попадания мусорных `.cs`.

После установки:

1. Закрыть Visual Studio.
2. Удалить `%LOCALAPPDATA%\Microsoft\VisualStudio\18.0_*\ComponentModelCache`.
3. Установить новый VSIX.
4. Запустить Visual Studio.

Ожидаемый пункт меню: `VS.Helper -> Project Genome`.
