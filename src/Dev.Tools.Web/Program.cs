using System.Globalization;
using Dev.Tools.Cryptography;
using Dev.Tools.Web;
using Dev.Tools.Web.Core;
using Dev.Tools.Web.Core.Cryptography;
using Dev.Tools.Web.Services;
using Dev.Tools.Web.Services.JavaScript;
using Dev.Tools.Web.Services.Layout;
using Dev.Tools.Web.Services.Localization;
using Dev.Tools.Web.Services.Preferences;
using Dev.Tools.Web.Services.Search;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMudServices();
builder.Services.AddSingleton<WebContext>();
builder.Services.AddSingleton<ISearchProvider, ToolsSearchProvider>();
builder.Services.AddSingleton<ILayoutService, LayoutService>();
builder.Services.AddSingleton<ILocalizationProvider, WebLocalizationProvider>();
builder.Services.AddSingleton<IPreferencesService, PreferencesService>();
builder.Services.AddSingleton<IJsServices, JsServices>();
builder.Services.AddCoreComponents(builder.Configuration);
builder.Services.AddSingleton<IMd5Hash, WebAssemblyMd5>();
builder.Services.AddDevTools();
builder.Services.AddDevToolsLocalization(typeof(Program).Assembly);

var app = builder.Build();
await app.Services.GetRequiredService<WebContext>().InitializeAsync(CancellationToken.None);
await app.RunAsync();
