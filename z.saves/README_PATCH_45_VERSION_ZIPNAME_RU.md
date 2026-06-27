# PATCH 45 — README RU / Version++ / VS.Helper.zip

Сделано:

1. `README.md` переписан на русском.
2. `Build Zip` теперь перед упаковкой вызывает `VsixVersionService.IncrementManifestVersion(...)`.
3. `source.extension.vsixmanifest` получает новую версию перед созданием архива.
4. `VS.Helper.csproj` синхронизирует `Version`, `AssemblyVersion`, `FileVersion`, `InformationalVersion`.
5. Имя архива по умолчанию теперь `VS.Helper.zip`.
6. `VS.Helper.Zip.xml` и sample-конфиг переведены на `VS.Helper.zip`.
7. В ресурсы добавлен `Resources/Icon.png`, чтобы путь из манифеста был валидным.

Проверка:

- Запусти `Build Zip`.
- В окне должно быть видно `VSIX version: старая → новая`.
- Архив должен называться `VS.Helper.zip`.
- Внутри архива `source.extension.vsixmanifest` должен быть уже с новой версией.

8. После информационного окна Build Zip открывает браузер на ChatGPT.
