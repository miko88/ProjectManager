using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ProjectManager.Application.Features.Auth.Login;
using ProjectManager.Application.Features.Projects.CreateProject;
using ProjectManager.Application.Features.Projects.DeleteProject;
using ProjectManager.Application.Features.Projects.ListProjects;
using ProjectManager.Application.Features.Projects.UpdateProject;

namespace ProjectManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateProjectValidator>();

        services.AddScoped<ListProjectsHandler>();
        services.AddScoped<CreateProjectHandler>();
        services.AddScoped<UpdateProjectHandler>();
        services.AddScoped<DeleteProjectHandler>();
        services.AddScoped<LoginHandler>();

        return services;
    }
}
