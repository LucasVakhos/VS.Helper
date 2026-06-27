// Helpers\Classes\ComboTodoItems.cs
// Commands\ComboTodoItems.cs
namespace VS.Helper.Commands;
#nullable disable

internal enum VSHelperComboTodoItems
{
    [VSHelperComboTodo(Name = "Удалить пустые строки...", UseBakup = true)]
    DeleteEmpty,

    [VSHelperComboTodo(Name = "Удалить строки #region #endregion...", UseBakup = true)]
    DeleteRegionRows,

    [VSHelperComboTodo(Name = "Найти и заменить...", UseBakup = true, ShowFind = true, ShowReplace = true)]
    FindAndReplace,

    [VSHelperComboTodo(Name = "Найти Class или значение в Class и добавить в папку проекта...", OperationTypes = VSHelperOperationTypes.ProcessFiles, ShowFind = true, ShowPlace = true)]
    FindValueOrClassAddScaveToProject,

    [VSHelperComboTodo(Name = "Удалить лишние ссылки на namespace...", UseBakup = true)]
    ClearNameSpace,

    [VSHelperComboTodo(Name = "Собрать все namespace проекта...", ShowPlace = true)]
    CollectAllNameSpaces,

    [VSHelperComboTodo(Name = "Собрать нужные using Packages проекта...", ShowPlace = true)]
    CollectUsingPackages,

    [VSHelperComboTodo(Name = "Удалить *.bak-файлы...", OperationTypes = VSHelperOperationTypes.ProcessFiles)]
    DeleteBakFiles,

    [VSHelperComboTodo(Name = "Удалить файлы не входящие в проект...", OperationTypes = VSHelperOperationTypes.ProcessFiles, UseBakup = true, SearchLabel = "Project:", ShowProject = true)]
    DeleteNonProjectFiles,

    [VSHelperComboTodo(Name = "Синхронизировать файл проекта с образцом файла проекта ...", UseBakup = true, SearchLabel = "Project:", PlaceLabel = "Sample project:", ShowProject = true, ShowSampleProject = true)]
    SyncProjectFileWithSample,

    [VSHelperComboTodo(Name = "Конвертировать старый .csproj в SDK-style...", SearchLabel = "Старый Project:", PlaceLabel = "Новый Project:", UseBakup = true, ShowProject = true, ShowPlace = true)]
    ConvertOldCsprojToSdkStyle,

    [VSHelperComboTodo(Name = "Перевести английский текст на русский в файлах проекта (включая комментарии)...", Pattern = VSHelperPatternType.CS, UseBakup = true)]
    TranslateEnToRu,

    [VSHelperComboTodo(Name = "Нормализовать сигнатуры методов...", OperationTypes = VSHelperOperationTypes.ProcessFiles, UseBakup = true)]
    NormalizeMethodSignatures,

    [VSHelperComboTodo(Name = "Восстановление файлов CSharp из Bak...")]
    RestoreCSharpFilesFromBak,

    [VSHelperComboTodo(Name = "Восстановление using в указанном проекте...", UseBakup = true, SearchLabel = "Recovery project:", PlaceLabel = "Sample project:", ShowProject = true, ShowSampleProject = true)]
    RestoreMissingUsings,

    [VSHelperComboTodo(Name = "Добавить комментарий /*Путь к файлу*/ к файлам .сs в папке...", OperationTypes = VSHelperOperationTypes.ProcessFiles)]
    AddFilePathCommentToCsFiles,

    [VSHelperComboTodo(Name = "Создать VS.Helper.Zip.xml / обновить секцию Git...")]
    CreateVsHelperZipConfig,

    [VSHelperComboTodo(Name = "Собрать ZIP по VS.Helper.Zip.xml...")]
    BuildVsHelperZip,

    [VSHelperComboTodo(Name = "Commit + Pull(Rebase) + Push через TokenProtected...")]
    CommitPullPushWithToken
}
