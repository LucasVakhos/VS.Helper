# VS.Helper 59 Adult Merge Clean

Исправления:

- Удалены вложенные дублирующие snapshot-папки `Resources/**` и лишние копии внутри `Helpers/**`.
- Оставлены только рабочие legacy-helper файлы, которые явно подключены в `.csproj`.
- Удалён второй package entry point `VSExtensionPackage.cs`; оставлен `VSHelperPackage.cs`.
- Синхронизированы `VSCommandTable.vsct` и `VSCommandTable.cs`: добавлен `ProjectGenomeCommand`.
- Добавлены жёсткие исключения `Resources/**`, `_zip/**`, `.vshelper/**` в `.csproj`.
- Добавлены excludes в `VS.Helper.Zip.xml`, чтобы Build Zip больше не тащил дубли назад.

Удалено/очищено элементов: 172

Проверка в sandbox: структура очищена; `dotnet build` не запускался, потому что `dotnet` отсутствует в контейнере.
