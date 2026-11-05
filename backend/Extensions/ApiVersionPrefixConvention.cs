using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace AumoBackend.Extensions;

public sealed class ApiVersionPrefixConvention : IApplicationModelConvention
{
    public const string Prefix = "api/v1";

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            if (controller.ControllerType.IsDefined(typeof(NoApiPrefixAttribute), inherit: true))
            {
                continue;
            }

            foreach (var selector in controller.Selectors)
            {
                var route = selector.AttributeRouteModel;
                if (route is null)
                {
                    continue;
                }

                var template = (route.Template ?? string.Empty).Trim().TrimStart('~', '/').TrimEnd('/');
                if (template.Equals(Prefix, StringComparison.OrdinalIgnoreCase)
                    || template.StartsWith(Prefix + "/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                route.Template = template.Length == 0 ? Prefix : $"{Prefix}/{template}";
            }
        }
    }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class NoApiPrefixAttribute : Attribute
{
}
