VS.Helper 40 - REAL Build Zip New Scheme + Root Menu

Что исправлено по факту:
1) Build Zip больше не собирает всю папку проекта через Directory.EnumerateFiles(projectDir, *, AllDirectories).
2) ProjectClosureScanner теперь берёт declared project items из .csproj: Compile/Content/None/Resource/VSCTCompile/etc.
3) Старый формат конфига <Path> теперь поддерживается, но новый формат пишет <File>/<Pattern>.
4) VS.Helper.Zip.xml переведён на новую схему:
   - OutputDir: _zip
   - ArchiveName: $(SolutionName)_$(Date)_$(Time).zip
   - IncludeProjectClosure: true
   - IncludeManifest: true
5) Исключены VSIX/bin/obj/_zip/*.zip/*.vsix/*.pdb, чтобы архив не выглядел как старая тупая упаковка всего подряд.
6) VSCommandTable.vsct теперь создаёт отдельный верхний пункт меню VS.Helper, а не группу внутри Tools.

Проверка глазами:
- В ZIP, который создаёт команда Build Zip, должен быть _VS.Helper.ZipManifest.txt.
- В архив не должны попадать VSIX, bin, obj, старые zip/vsix/pdb.
- Команда должна быть в верхнем меню: VS.Helper -> Build Zip.
