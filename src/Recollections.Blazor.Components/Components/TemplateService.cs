using Microsoft.AspNetCore.Components;
using Neptuo;
using Neptuo.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components;

public class TemplateService(ILog<TemplateService> log)
{
    private readonly Dictionary<string, TemplatePlaceholder> declarations = new Dictionary<string, TemplatePlaceholder>();

    public void DeclarePlaceholder(string name, TemplatePlaceholder component)
    {
        Ensure.NotNull(name, "name");
        Ensure.NotNull(component, "component");
        declarations[name] = component;
    }

    public void DisposePlaceholder(string name, TemplatePlaceholder component)
    {
        Ensure.NotNull(name, "name");
        Ensure.NotNull(component, "component");
        if (declarations.TryGetValue(name, out var current) && current == component)
            declarations.Remove(name);
    }

    public void AddContent(string name, TemplateContent content)
    {
        Ensure.NotNull(name, "name");
        if (declarations.TryGetValue(name, out var placeholder))
            placeholder.AddContent(content);
        else
            log.Info($"Missing placeholder named '{name}'.");
    }

    public void RemoveContent(string name, TemplateContent content)
    {
        Ensure.NotNull(name, "name");
        if (declarations.TryGetValue(name, out var placeholder))
            placeholder.RemoveContent(content);
        else
            log.Info($"Missing placeholder named '{name}'.");
    }
}
