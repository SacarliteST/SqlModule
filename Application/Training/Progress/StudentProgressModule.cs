using Microsoft.Extensions.DependencyInjection;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Training.Progress.RestartProgress;
using SQLModule.Application.Training.Progress.StartProgress;

namespace SQLModule.Application.Training.Progress;

public static class StudentProgressModule
{
    public static IServiceCollection AddStudentProgress(this IServiceCollection services) => services
        .AddScoped<IRequestHandler<StartStudentProgressCommand, Result<StudentTaskProgressResponse>>,
            StartStudentProgressHandler>()
        .AddScoped<IRequestHandler<RestartStudentProgressCommand, Result<StudentTaskProgressResponse>>,
            RestartStudentProgressHandler>()
        .AddScoped<IPlatformProgressService, PlatformProgressService>()
        .AddScoped<IProgressFinalizationService, ProgressFinalizationService>()
        .AddScoped<IAttemptReservationService, AttemptReservationService>();
}
