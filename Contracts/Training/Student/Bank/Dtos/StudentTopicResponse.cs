namespace SQLModule.Contracts.Training.Student;

/// <summary>Узел дерева тем открытого банка с личной статистикой.</summary>
/// <param name="Id">Идентификатор темы.</param>
/// <param name="TopicName">Название темы.</param>
/// <param name="ParentTopicId">Идентификатор родительской темы.</param>
/// <param name="Description">Описание темы.</param>
/// <param name="OpenTasksCount">Число открытых заданий в теме и её потомках.</param>
/// <param name="AttemptedTasksCount">Число заданий с тренировочными попытками.</param>
/// <param name="SolvedTasksCount">Число решённых заданий.</param>
/// <param name="PublishedTasksCount">Число опубликованных заданий, сохранённое для совместимости существующего каталога.</param>
public sealed record StudentTopicResponse(
    Guid Id,
    string TopicName,
    Guid? ParentTopicId,
    string? Description,
    int OpenTasksCount,
    int AttemptedTasksCount,
    int SolvedTasksCount,
    int PublishedTasksCount);
