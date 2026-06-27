# VS.Helper 46 — исправление установки VSIX

Что изменено:

- `CreateVsixContainer` включён: `True`.
- Версия синхронизирована до `2026.2.1.13`.
- `Build Zip` теперь обновляет не только корневой `source.extension.vsixmanifest`, но и вложенные `source.extension.vsixmanifest`, `source.extension*.cs`, `.csproj`, `AssemblyInfo.cs`.
- Из `VSCommandTable.vsct` убран самовключающий include `VSCommandTable.vsct`.
- В README добавлена русская инструкция по обновлению VSIX.

Важно: после инкремента версии нужно пересобрать проект, потому что VSIX installer устанавливает именно собранный `.vsix`, а не исходный manifest из ZIP.
