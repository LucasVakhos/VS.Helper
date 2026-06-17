// Helpers\Classes\ComboTodoItems.cs
// Commands\ComboTodoItems.cs
namespace VS.Helper.Commands;
#nullable disable

internal enum ComboTodoItems
{
    [ComboTodo(Name = "Удалить пустые строки...", UseBakup = true)]
    DeleteEmpty,

    [ComboTodo(Name = "Удалить строки #region #endregion...", UseBakup = true)]
    DeleteRegionRows,

    [ComboTodo(Name = "Найти и заменить...", UseBakup = true, ShowFind = true, ShowReplace = true)]
    FindAndReplace,

    [ComboTodo(Name = "Найти Class или значение в Class и добавить в папку проекта...", OperationTypes = OperationTypes.ProcessFiles, ShowFind = true, ShowPlace = true)]
    FindValueOrClassAddScaveToProject,

    [ComboTodo(Name = "Удалить лишние ссылки на namespace...", UseBakup = true)]
    ClearNameSpace,

    [ComboTodo(Name = "Собрать все namespace проекта...", ShowPlace = true)]
    CollectAllNameSpaces,

    [ComboTodo(Name = "Собрать нужные using Packages проекта...", ShowPlace = true)]
    CollectUsingPackages,

    [ComboTodo(Name = "Удалить *.bak-файлы...", OperationTypes = OperationTypes.ProcessFiles)]
    DeleteBakFiles,

    [ComboTodo(Name = "Удалить файлы не входящие в проект...", OperationTypes = OperationTypes.ProcessFiles, UseBakup = true, SearchLabel = "Project:", ShowProject = true)]
    DeleteNonProjectFiles,

    [ComboTodo(Name = "Синхронизировать файл проекта с образцом файла проекта ...", UseBakup = true, SearchLabel = "Project:", PlaceLabel = "Sample project:", ShowProject = true, ShowSampleProject = true)]
    SyncProjectFileWithSample,

    [ComboTodo(Name = "Конвертировать старый .csproj в SDK-style...", SearchLabel = "Старый Project:", PlaceLabel = "Новый Project:", UseBakup = true, ShowProject = true, ShowPlace = true)]
    ConvertOldCsprojToSdkStyle,

    [ComboTodo(Name = "Перевести английский текст на русский в файлах проекта (включая комментарии)...", Pattern = PatternType.CS, UseBakup = true)]
    TranslateEnToRu,

    [ComboTodo(Name = "Нормализовать сигнатуры методов...", OperationTypes = OperationTypes.ProcessFiles, UseBakup = true)]
    NormalizeMethodSignatures,

    [ComboTodo(Name = "Восстановление файлов CSharp из Bak...")]
    RestoreCSharpFilesFromBak,

    [ComboTodo(Name = "Восстановление using в указанном проекте...", UseBakup = true, SearchLabel = "Recovery project:", PlaceLabel = "Sample project:", ShowProject = true, ShowSampleProject = true)]
    RestoreMissingUsings,

    [ComboTodo(Name = "Добавить комментарий /*Путь к файлу*/ к файлам .сs в папке...", OperationTypes = OperationTypes.ProcessFiles)]
    AddFilePathCommentToCsFiles,

    [ComboTodo(Name = "Создать VS.Helper.Zip.xml / обновить секцию Git...")]
    CreateVsHelperZipConfig,

    [ComboTodo(Name = "Собрать ZIP по VS.Helper.Zip.xml...")]
    BuildVsHelperZip,

    [ComboTodo(Name = "Commit + Pull(Rebase) + Push через TokenProtected...")]
    CommitPullPushWithToken
}
