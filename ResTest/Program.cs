using System.Globalization;
using Microsoft.Extensions.Localization;
using TrackerPlus.Web.Resources;

var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
services.AddLocalization(o => o.ResourcesPath = "Resources");
services.AddSingleton(typeof(SharedResources));
var sp = services.BuildServiceProvider();
var factory = sp.GetRequiredService<IStringLocalizerFactory>();
var localizer = factory.Create(typeof(SharedResources));

CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-TW");
var s = localizer["MyDevices_Title"];
Console.WriteLine($"zh-TW: Name={s.Name} Value={s.Value} ResourceNotFound={s.ResourceNotFound}");

CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-CN");
s = localizer["MyDevices_Title"];
Console.WriteLine($"zh-CN: Name={s.Name} Value={s.Value} ResourceNotFound={s.ResourceNotFound}");

CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
s = localizer["MyDevices_Title"];
Console.WriteLine($"en-US: Name={s.Name} Value={s.Value} ResourceNotFound={s.ResourceNotFound}");
