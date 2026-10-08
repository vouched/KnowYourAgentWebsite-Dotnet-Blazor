using Checkpoint.AspNetCore.Extensions;
using Checkpoint.Core.Configuration;
using KnowYourAgentWebsite.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCheckpoint(options =>
{
    options.ProjectId = builder.Configuration["Checkpoint:ProjectId"]!;
    options.ApiKey = builder.Configuration["Checkpoint:ApiKey"]!;
    options.OnAgentDetected = DetectedAction.Log;
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.UseCheckpoint();
app.UseRouting();

app.Run();