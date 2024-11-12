using Dev.Tools;
using Dev.Tools.Web;
using Dev.Tools.Web.Core;
using Dev.Tools.Web.Services.Layout;
using Dev.Tools.Web.Services.Preferences;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMudServices();
builder.Services.AddSingleton<ILayoutService, LayoutService>();
builder.Services.AddSingleton<IPreferencesService, PreferencesService>();
builder.Services.AddCoreComponents(builder.Configuration);
builder.Services.AddTools();

await builder.Build().RunAsync();
