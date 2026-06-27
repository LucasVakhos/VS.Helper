# VS.Helper v61 Green Line

## Цель

Стабилизировать сборку после v60 и зафиксировать правило чистой сборки перед каждым релизом.

## Исправлено

- `ToolWindow/ZipToolWindowControl.xaml.cs`: базовый класс `UserControl` указан полностью как `System.Windows.Controls.UserControl`, чтобы убрать конфликт с `System.Windows.Forms.UserControl`.
- Добавлен `clean-build.cmd`.
- Добавлен `clean-build.ps1`.

## Стандарт сборки

Перед проверкой релиза запускать:

```cmd
clean-build.cmd
```

или:

```powershell
.\clean-build.ps1
```

## Важно

Предупреждения `NU1701` по Visual Studio SDK-пакетам пока не блокируют сборку. Главная цель этого этапа — 0 ошибок.
