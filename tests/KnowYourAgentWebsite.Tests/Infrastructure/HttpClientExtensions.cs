using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;

namespace KnowYourAgentWebsite.Tests.Infrastructure;

static class HttpClientExtensions
{
	public static async Task<IDocument> GetDocumentAsync(this HttpClient client, string requestUri)
	{
		using var response = await client.GetAsync(new Uri(requestUri, UriKind.Relative));
		return await response.ReadDocumentAsync();
	}

	public static async Task<IDocument> ReadDocumentAsync(this HttpResponseMessage response)
	{
		var html = await response.Content.ReadAsStringAsync();
		return await BrowsingContext.New(AngleSharp.Configuration.Default).OpenAsync(request => request.Content(html));
	}

	/// <summary>Requests <paramref name="requestUri"/> the way a client with <paramref name="userAgent"/> would, like <c>curl -A</c>.</summary>
	public static async Task<HttpResponseMessage> GetAsAsync(this HttpClient client, string userAgent, string requestUri = "/", IReadOnlyDictionary<string, string>? headers = null)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(requestUri, UriKind.Relative));
		request.Headers.TryAddWithoutValidation("User-Agent", userAgent);

		foreach (var (name, value) in headers ?? new Dictionary<string, string>())
			request.Headers.TryAddWithoutValidation(name, value);

		return await client.SendAsync(request);
	}

	/// <summary>The element's text with runs of whitespace (from line breaks in the markup) collapsed to single spaces.</summary>
	public static string? NormalizedText(this IElement? element) =>
		element is null ? null : string.Join(' ', element.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

	/// <summary>Reads one of the <c>KYA-*</c> headers Checkpoint adds to every response.</summary>
	public static string? GetKyaHeader(this HttpResponseMessage response, string name) =>
		response.Headers.TryGetValues(name, out var values) ? string.Join(",", values) : null;

	/// <summary>
	/// Submits the Blazor form named <paramref name="formName"/> on the home page like a browser would: load the page (which sets
	/// the antiforgery cookie), fill in the fields whose element ids are in <paramref name="fieldsById"/>, and post every field.
	/// </summary>
	public static async Task<HttpResponseMessage> SubmitFormAsync(this HttpClient client, string formName, IReadOnlyDictionary<string, string>? fieldsById = null)
	{
		var document = await client.GetDocumentAsync("/");

		var form = document.QuerySelectorAll<IHtmlFormElement>("form")
			.FirstOrDefault(form => form.QuerySelector<IHtmlInputElement>("input[name=_handler]")?.Value == formName)
			?? throw new AssertionException($"The '{formName}' form was not rendered");

		var fields = form.Elements
			.OfType<IHtmlInputElement>()
			.Where(static input => !string.IsNullOrEmpty(input.Name))
			.ToDictionary(static input => input.Name!, static input => input.Value);

		foreach (var (id, value) in fieldsById ?? new Dictionary<string, string>())
		{
			var input = form.QuerySelector<IHtmlInputElement>($"#{id}") ?? throw new AssertionException($"The '{formName}' form has no '#{id}' field");
			fields[input.Name!] = value;
		}

		using var content = new FormUrlEncodedContent(fields);
		return await client.PostAsync(new Uri("/", UriKind.Relative), content);
	}
}
