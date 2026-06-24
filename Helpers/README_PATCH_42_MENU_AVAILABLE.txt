VS.Helper PATCH 42 — MENU AVAILABLE FIX

Что исправлено:
- Добавлен всегда доступный пункт VS.Helper -> Status / About.
- Root menu VS.Helper помечен AlwaysCreate.
- Все пункты меню помечены AlwaysCreate в VSCT.
- Tools больше не отключает всё меню, если Solution не открыт.
- Build Zip остаётся на новом Core/Zip pipeline.

Почему так:
Visual Studio может показывать top-level menu серым/недоступным,
если все дочерние команды disabled/hidden во время QueryStatus.
Теперь в меню всегда есть активная команда Status / About,
поэтому пункт VS.Helper доступен сразу после загрузки расширения.
