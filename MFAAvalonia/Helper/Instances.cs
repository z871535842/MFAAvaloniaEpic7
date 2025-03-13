
using MFAAvalonia.Utilities.Attributes;
using MFAAvalonia.ViewModels.Windows;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Threading;

namespace MFAAvalonia.Helper;

#pragma warning disable CS0169 // The field is never used
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor

[LazyStatic]
public static partial class Instances
{
     #region Core Resolver
     private static readonly ConcurrentDictionary<Type, Lazy<object>> ServiceCache = new();

     /// <summary>
     /// 解析服务（自动缓存 + 循环依赖检测）
     /// </summary>
     private static T Resolve<T>() where T : new()
     {
          var serviceType = typeof(T);
          var lazy = ServiceCache.GetOrAdd(serviceType, _ =>
               new Lazy<object>(
                    () =>
                    {
                         try { return new T(); }
                         catch (InvalidOperationException ex)
                         {
                              throw new InvalidOperationException(
                                   $"Failed to resolve service {typeof(T).Name}. Possible causes: 1. Service not registered; 2. Circular dependency detected; 3. Thread contention during initialization.", ex);
                         }
                    },
                    LazyThreadSafetyMode.ExecutionAndPublication
               ));
          return (T)lazy.Value;
     }
     #endregion
     
     private static RootViewModel _rootViewModel;
}
