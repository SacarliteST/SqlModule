namespace SQLModule.Web.Common.Auth;

/// <summary>Имена политик авторизации.</summary>
public static class Policies
{
    /// <summary>Управление справочниками СУБД (только Admin).</summary>
    public const string Admin = "Admin";

    /// <summary>Авторинг учебного контента (Teacher ИЛИ Admin).</summary>
    public const string ContentAuthor = "ContentAuthor";

    /// <summary>Отправка попыток (только Student).</summary>
    public const string Student = "Student";
}

/// <summary>Названия ролей — совпадают с ролями, выпускаемыми Identity-сервисом.</summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Teacher = "Teacher";
    public const string Student = "Student";
}
