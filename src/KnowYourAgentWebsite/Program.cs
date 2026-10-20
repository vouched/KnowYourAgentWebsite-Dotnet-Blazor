using KnowYourAgentWebsite;
using KnowYourAgentWebsite.Hosting;

var restartCoordinator = new ApplicationRestartCoordinator();

// The loop lets the home page apply newly saved Vouched keys by restarting the host in-process; a normal shutdown (Ctrl+C) exits it
do
{
	await using var app = WebsiteApplication.Build(args, restartCoordinator);
	await app.RunAsync();
}
while (restartCoordinator.TryConsumeRestartRequest());
