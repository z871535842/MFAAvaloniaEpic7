using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MFAAvalonia.Extensions;

public class ServiceProviderExtension: MarkupExtension
{
    public Type ServiceType { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return App.Services.GetRequiredService(ServiceType);
    }
}
