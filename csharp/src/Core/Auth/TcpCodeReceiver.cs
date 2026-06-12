namespace Scripts.Core.Auth;

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;

internal sealed class TcpCodeReceiver(string closePageResponse = "<html><body><h2>Authentication complete. You may close this window.</h2></body></html>") : ICodeReceiver
{
	private readonly string _closePageResponse = closePageResponse;

	public string RedirectUri { get; } = $"http://localhost:{GetRandomUnusedPort()}/authorize/";

	public async Task<AuthorizationCodeResponseUrl> ReceiveCodeAsync(
		AuthorizationCodeRequestUrl url,
		CancellationToken taskCancellationToken)
	{
		var authorizationUrl = url.Build().AbsoluteUri;
		Console.WriteLine($"[TRACE] TcpCodeReceiver: Opening browser to {authorizationUrl}");

		try
		{
			var process = Process.Start(new ProcessStartInfo(authorizationUrl) { UseShellExecute = true });
			if (process == null)
			{
				Console.WriteLine($"[TRACE] TcpCodeReceiver: Process.Start returned null - browser may not have launched");
			}
			else
			{
				Console.WriteLine($"[TRACE] TcpCodeReceiver: Browser process launched, PID={process.Id}");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[TRACE] TcpCodeReceiver: Process.Start FAILED: {ex.GetType().Name}: {ex.Message}");
			throw;
		}

		var listener = new TcpListener(IPAddress.Loopback, GetPortFromUri(RedirectUri));

		using (taskCancellationToken.Register(() => listener.Stop()))
		{
			listener.Start();
			Console.WriteLine($"[TRACE] TcpCodeReceiver: Listening on {RedirectUri}");

			using var client = await listener.AcceptTcpClientAsync(taskCancellationToken);
			using var stream = client.GetStream();

			var buffer = new byte[4096];
			var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), taskCancellationToken);
			var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

			Console.WriteLine($"[TRACE] TcpCodeReceiver: Received request");

			var requestLine = request.Split('\r', '\n')[0];
			var parts = requestLine.Split(' ');
			if (parts.Length < 2)
			{
				throw new InvalidOperationException($"Invalid HTTP request: {requestLine}");
			}

			var requestPath = parts[1];
			var queryIndex = requestPath.IndexOf('?');
			var queryString = queryIndex >= 0 ? requestPath[(queryIndex + 1)..] : "";

			var responseContent = $"HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {_closePageResponse.Length}\r\nConnection: close\r\n\r\n{_closePageResponse}";
			var responseBytes = Encoding.UTF8.GetBytes(responseContent);
			await stream.WriteAsync(responseBytes.AsMemory(0, responseBytes.Length), taskCancellationToken);

			Console.WriteLine($"[TRACE] TcpCodeReceiver: Sent response, parsing query string: {queryString}");

			var parameters = System.Web.HttpUtility.ParseQueryString(queryString);
			var dict = new Dictionary<string, string>();
			foreach (string? key in parameters.AllKeys)
			{
				if (key != null)
				{
					dict[key] = parameters[key] ?? "";
				}
			}

			return new AuthorizationCodeResponseUrl(dict);
		}
	}

	private static int GetRandomUnusedPort()
	{
		var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		var port = ((IPEndPoint)listener.LocalEndpoint).Port;
		listener.Stop();
		return port;
	}

	private static int GetPortFromUri(string uri) => new Uri(uri).Port;
}
