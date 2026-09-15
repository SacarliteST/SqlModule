using Microsoft.Extensions.DependencyInjection;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Web.Common.Cqrs;
using SQLModule.Web.Features.Training.Progress.RestartProgress;
using SQLModule.Web.Features.Training.Progress.StartProgress;

namespace SQLModule.Web.Features.Training.Progress;

internal static class StudentProgressModule
{
    internal static IServiceCollection AddStudentProgress(this IServiceCollection services) => services
        .AddScoped<IRequestHandler<StartStudentProgressCommand, Result<StudentTaskProgressResponse>>,
            StartStudentProgressHandler>()
        .AddScoped<IRequestHandler<RestartStudentProgressCommand, Result<StudentTaskProgressResponse>>,
            RestartStudentProgressHandler>()
        .AddScoped<IPlatformProgressService, PlatformProgressService>()
        .AddScoped<IAttemptReservationService, AttemptReservationService>();
}
