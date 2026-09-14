using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using StreamlineTax.Application.Common.Behaviours;
using System.Reflection;

namespace StreamlineTax.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
        });

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}
