namespace KnowYourAgentWebsite.Simulation;

/// <summary>Whether a visitor browses on behalf of a person (an AI agent) or on its own (a crawler or script).</summary>
public enum VisitorKind
{
	AiAgent,
	Bot,
}

/// <summary>
/// A real-world AI agent or bot, described by the user agent it actually sends. <c>Token</c> is the part of the user agent
/// that identifies it, shown on the page.
/// </summary>
public sealed record SimulatedVisitor(string Id, string Name, string Operator, string Description, VisitorKind Kind, string Token, string UserAgent)
{
	public static IReadOnlyList<SimulatedVisitor> All { get; } =
	[
		new("chatgpt", "ChatGPT", "OpenAI", "Fetches a page for a ChatGPT user", VisitorKind.AiAgent, "ChatGPT-User/1.0",
			"Mozilla/5.0 AppleWebKit/537.36 (KHTML, like Gecko); compatible; ChatGPT-User/1.0; +https://openai.com/bot"),
		new("claude", "Claude", "Anthropic", "Fetches a page for a Claude user", VisitorKind.AiAgent, "Claude-User/1.0",
			"Mozilla/5.0 AppleWebKit/537.36 (KHTML, like Gecko; compatible; Claude-User/1.0; +Claude-User@anthropic.com)"),
		new("perplexity", "Perplexity", "Perplexity", "Fetches a page for a Perplexity user", VisitorKind.AiAgent, "Perplexity-User/1.0",
			"Mozilla/5.0 AppleWebKit/537.36 (KHTML, like Gecko; compatible; Perplexity-User/1.0; +https://perplexity.ai/perplexity-user)"),
		new("gptbot", "GPTBot", "OpenAI", "Crawls the web to train OpenAI models", VisitorKind.Bot, "GPTBot/1.2",
			"Mozilla/5.0 AppleWebKit/537.36 (KHTML, like Gecko; compatible; GPTBot/1.2; +https://openai.com/gptbot)"),
		new("claudebot", "ClaudeBot", "Anthropic", "Crawls the web to train Claude models", VisitorKind.Bot, "ClaudeBot/1.0",
			"Mozilla/5.0 AppleWebKit/537.36 (KHTML, like Gecko; compatible; ClaudeBot/1.0; +claudebot@anthropic.com)"),
		new("googlebot", "Googlebot", "Google", "Crawls the web for Google Search", VisitorKind.Bot, "Googlebot/2.1",
			"Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)"),
		new("headless-chrome", "Headless Chrome", "Anyone", "Browser automation, like Puppeteer or Playwright", VisitorKind.Bot, "HeadlessChrome/141.0",
			"Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/141.0.0.0 Safari/537.36"),
		new("curl", "curl", "Anyone", "A command-line HTTP client", VisitorKind.Bot, "curl/8.7.1",
			"curl/8.7.1"),
		new("python-requests", "Python script", "Anyone", "A script using the requests library", VisitorKind.Bot, "python-requests/2.32.3",
			"python-requests/2.32.3"),
	];
}
