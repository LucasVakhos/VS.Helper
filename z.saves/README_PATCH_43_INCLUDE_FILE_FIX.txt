VS.Helper patch 43

Fixes Build Zip config parser:
- <Include><File>...</File></Include> is now accepted by the legacy/new mixed engine.
- <Exclude><Pattern>...</Pattern></Exclude> is now accepted.
- Empty Include no longer fails when IncludeProjectClosure is true.
- Directory entries in VS.Helper.Zip.xml were converted to explicit recursive globs.

Причина ошибки была простая: часть живого ZIP pipeline читала только старые теги <Path>, а конфиг уже был в новой схеме <File>/<Pattern>.
