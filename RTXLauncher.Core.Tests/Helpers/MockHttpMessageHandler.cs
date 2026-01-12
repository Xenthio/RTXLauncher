using System.Net;

namespace RTXLauncher.Core.Tests.Helpers;

/// <summary>
/// Mock HTTP message handler for testing HTTP requests
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
	private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _sendAsync;

	public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
	{
		_sendAsync = sendAsync;
	}

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		return _sendAsync(request, cancellationToken);
	}

	/// <summary>
	/// Creates a mock handler that returns successful responses with specified content
	/// </summary>
	public static MockHttpMessageHandler CreateSuccess(byte[] content, HttpStatusCode statusCode = HttpStatusCode.OK)
	{
		return new MockHttpMessageHandler((request, cancellationToken) =>
		{
			var response = new HttpResponseMessage(statusCode)
			{
				Content = new ByteArrayContent(content)
			};
			response.Content.Headers.ContentLength = content.Length;
			return Task.FromResult(response);
		});
	}

	/// <summary>
	/// Creates a mock handler that supports range requests (for resume testing)
	/// </summary>
	public static MockHttpMessageHandler CreateWithRangeSupport(byte[] fullContent)
	{
		return new MockHttpMessageHandler((request, cancellationToken) =>
		{
			if (request.Headers.Range != null)
			{
				// Handle range request
				var range = request.Headers.Range.Ranges.First();
				var startByte = (int)(range.From ?? 0);
				var endByte = (int)(range.To ?? fullContent.Length - 1);
				var rangeLength = endByte - startByte + 1;

				var rangeContent = new byte[rangeLength];
				Array.Copy(fullContent, startByte, rangeContent, 0, rangeLength);

				var response = new HttpResponseMessage(HttpStatusCode.PartialContent)
				{
					Content = new ByteArrayContent(rangeContent)
				};
				response.Content.Headers.ContentLength = rangeContent.Length;
				response.Content.Headers.ContentRange = new System.Net.Http.Headers.ContentRangeHeaderValue(startByte, endByte, fullContent.Length);
				return Task.FromResult(response);
			}
			else
			{
				// Handle normal request
				var response = new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new ByteArrayContent(fullContent)
				};
				response.Content.Headers.ContentLength = fullContent.Length;
				return Task.FromResult(response);
			}
		});
	}

	/// <summary>
	/// Creates a mock handler that fails with specified status code
	/// </summary>
	public static MockHttpMessageHandler CreateFailure(HttpStatusCode statusCode, string? message = null)
	{
		return new MockHttpMessageHandler((request, cancellationToken) =>
		{
			var response = new HttpResponseMessage(statusCode);
			if (!string.IsNullOrEmpty(message))
			{
				response.Content = new StringContent(message);
			}
			return Task.FromResult(response);
		});
	}

	/// <summary>
	/// Creates a mock handler that fails N times then succeeds
	/// </summary>
	public static MockHttpMessageHandler CreateFailThenSucceed(int failCount, byte[] successContent, HttpStatusCode failStatusCode = HttpStatusCode.ServiceUnavailable)
	{
		int callCount = 0;
		return new MockHttpMessageHandler((request, cancellationToken) =>
		{
			callCount++;
			if (callCount <= failCount)
			{
				return Task.FromResult(new HttpResponseMessage(failStatusCode));
			}
			else
			{
				var response = new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new ByteArrayContent(successContent)
				};
				response.Content.Headers.ContentLength = successContent.Length;
				return Task.FromResult(response);
			}
		});
	}

	/// <summary>
	/// Creates a mock handler that throws an exception
	/// </summary>
	public static MockHttpMessageHandler CreateThrowsException(Exception exception)
	{
		return new MockHttpMessageHandler((request, cancellationToken) =>
		{
			throw exception;
		});
	}
}
