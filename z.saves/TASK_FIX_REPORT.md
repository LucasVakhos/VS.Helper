# VS.Helper 59 Task Global Using Fix

## Problem
Visual Studio reported CS0246 for `Task` in many files after the menu registration fix.

## Cause
Several legacy/source files rely on global/common usings. After merge cleanup the project no longer reliably provided `System.Threading.Tasks` everywhere.

## Fix
Added `GlobalUsings.cs` with common imports:

- `System`
- `System.Collections.Generic`
- `System.IO`
- `System.Linq`
- `System.Text`
- `System.Threading`
- `System.Threading.Tasks`

Also set in `VS.Helper.csproj`:

```xml
<ImplicitUsings>enable</ImplicitUsings>
<LangVersion>latest</LangVersion>
```

## Expected result
Errors like `CS0246: Task could not be found` should disappear.
