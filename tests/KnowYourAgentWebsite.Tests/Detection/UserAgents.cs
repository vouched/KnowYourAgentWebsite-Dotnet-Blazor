namespace KnowYourAgentWebsite.Tests.Detection;

/// <summary>Real-world user agents (as published by each operator) used to exercise Checkpoint's detection engine.</summary>
static class UserAgents
{
	public const string ChatGptUser = "Mozilla/5.0 AppleWebKit/537.36 (KHTML, like Gecko); compatible; ChatGPT-User/1.0; +https://openai.com/bot";
	public const string ClaudeUser = "Mozilla/5.0 AppleWebKit/537.36 (KHTML, like Gecko; compatible; Claude-User/1.0; +Claude-User@anthropic.com)";
	public const string PerplexityUser = "Mozilla/5.0 AppleWebKit/537.36 (KHTML, like Gecko; compatible; Perplexity-User/1.0; +https://perplexity.ai/perplexity-user)";
	public const string GptBot = "Mozilla/5.0 AppleWebKit/537.36 (KHTML, like Gecko; compatible; GPTBot/1.2; +https://openai.com/gptbot)";
	public const string ClaudeBot = "Mozilla/5.0 AppleWebKit/537.36 (KHTML, like Gecko; compatible; ClaudeBot/1.0; +claudebot@anthropic.com)";
	public const string PerplexityBot = "Mozilla/5.0 AppleWebKit/537.36 (KHTML, like Gecko; compatible; PerplexityBot/1.0; +https://perplexity.ai/perplexitybot)";
	public const string Googlebot = "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)";
	public const string Curl = "curl/8.7.1";
	public const string PythonRequests = "python-requests/2.32.3";
	public const string Chrome = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36";

	/// <summary>The headers a real Chrome navigation sends alongside its user agent.</summary>
	public static IReadOnlyDictionary<string, string> ChromeNavigationHeaders { get; } = new Dictionary<string, string>
	{
		["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8",
		["Accept-Language"] = "en-US,en;q=0.9",
		["Accept-Encoding"] = "gzip, deflate, br, zstd",
		["Sec-CH-UA"] = "\"Google Chrome\";v=\"141\", \"Chromium\";v=\"141\", \"Not?A_Brand\";v=\"24\"",
		["Sec-CH-UA-Mobile"] = "?0",
		["Sec-CH-UA-Platform"] = "\"macOS\"",
		["Sec-Fetch-Dest"] = "document",
		["Sec-Fetch-Mode"] = "navigate",
		["Sec-Fetch-Site"] = "none",
		["Sec-Fetch-User"] = "?1",
		["Upgrade-Insecure-Requests"] = "1",
	};
}
