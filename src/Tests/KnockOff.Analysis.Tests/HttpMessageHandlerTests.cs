// ============================================================================
// HttpMessageHandlerTests: Real-world edge case test for stubbing
// HttpMessageHandler, which has a protected abstract SendAsync method.
// Inspired by Rocks.Analysis.IntegrationTests.HttpMessageHandlerTests
//
// Key behavior: HttpMessageHandler is a BCL abstract class with:
//   protected abstract Task<HttpResponseMessage> SendAsync(
//       HttpRequestMessage request, CancellationToken cancellationToken);
//
// The stub must override SendAsync, configure a return value, then use
// the stub with HttpClient (which calls SendAsync internally).
//
// Scenarios:
// 1. Configure SendAsync to return a response, create HttpClient with stub,
//    make HTTP request, verify response matches
// 2. Verify SendAsync was called
//
// Applicable patterns (class only — abstract class with protected ctor):
// - Pattern 3 (Standalone Class): [KnockOffBase<HttpMessageHandler>]
// - Pattern 6 (Inline Class): [KnockOff<HttpMessageHandler>]
// ============================================================================

using System.Net;
using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.HttpMessageHandlerTestTypes
{
	// Pattern 3: Standalone class stub for HttpMessageHandler
	[KnockOffBase<HttpMessageHandler>]
	public partial class HttpHandlerStandaloneKnockOff
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.HttpMessageHandlerTestTypes;

	// Pattern 6: Inline class stub for HttpMessageHandler
	[KnockOff<HttpMessageHandler>]
	public partial class HttpMessageHandlerInlineTests
	{
	}

	public class HttpMessageHandlerTests
	{
		// ====================================================================
		// Scenario 1: Configure SendAsync, use with HttpClient, verify response
		// ====================================================================

		#region Pattern 3 (Standalone Class): Full HttpClient integration

		[Fact]
		public async Task SendAsync_StandaloneClass_ReturnsConfiguredResponse()
		{
			using var response = new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
				Content = new StringContent("OK")
			};

			var stub = new HttpHandlerStandaloneKnockOff();

			stub.SendAsync.Return(response);

			using var handler = stub.Object;
			using var client = new HttpClient(handler);
			var getResponse = await client.GetAsync(new Uri("https://localhost.com"));

			Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

			var content = await getResponse.Content.ReadAsStringAsync();
			Assert.Equal("OK", content);
		}

		[Fact]
		public async Task SendAsync_StandaloneClass_VerifyCallTracking()
		{
			using var response = new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
			};

			var stub = new HttpHandlerStandaloneKnockOff();

			stub.SendAsync.Return(response);

			using var handler = stub.Object;
			using var client = new HttpClient(handler);
			await client.GetAsync(new Uri("https://localhost.com"));

			stub.SendAsync.Verify(Called.Once);
		}

		#endregion

		#region Pattern 6 (Inline Class): Full HttpClient integration

		[Fact]
		public async Task SendAsync_InlineClass_ReturnsConfiguredResponse()
		{
			using var response = new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
				Content = new StringContent("OK")
			};

			var stub = new HttpMessageHandlerInlineTests.Stubs.HttpMessageHandler();

			stub.SendAsync.Return(response);

			using var handler = stub.Object;
			using var client = new HttpClient(handler);
			var getResponse = await client.GetAsync(new Uri("https://localhost.com"));

			Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

			var content = await getResponse.Content.ReadAsStringAsync();
			Assert.Equal("OK", content);
		}

		[Fact]
		public async Task SendAsync_InlineClass_VerifyCallTracking()
		{
			using var response = new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
			};

			var stub = new HttpMessageHandlerInlineTests.Stubs.HttpMessageHandler();

			stub.SendAsync.Return(response);

			using var handler = stub.Object;
			using var client = new HttpClient(handler);
			await client.GetAsync(new Uri("https://localhost.com"));

			stub.SendAsync.Verify(Called.Once);
		}

		#endregion

		// ====================================================================
		// Scenario 2: Callback on SendAsync captures request details
		// ====================================================================

		#region Pattern 3 (Standalone Class): Callback captures request

		[Fact]
		public async Task SendAsync_StandaloneClass_CallbackCapturesRequest()
		{
			using var response = new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
			};

			var stub = new HttpHandlerStandaloneKnockOff();
			HttpRequestMessage? capturedRequest = null;

			stub.SendAsync.Call((HttpRequestMessage request, CancellationToken ct) =>
			{
				capturedRequest = request;
				return Task.FromResult(response);
			});

			using var handler = stub.Object;
			using var client = new HttpClient(handler);
			await client.GetAsync(new Uri("https://localhost.com/test"));

			Assert.NotNull(capturedRequest);
			Assert.Equal(HttpMethod.Get, capturedRequest.Method);
		}

		#endregion

		#region Pattern 6 (Inline Class): Callback captures request

		[Fact]
		public async Task SendAsync_InlineClass_CallbackCapturesRequest()
		{
			using var response = new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
			};

			var stub = new HttpMessageHandlerInlineTests.Stubs.HttpMessageHandler();
			HttpRequestMessage? capturedRequest = null;

			stub.SendAsync.Call((HttpRequestMessage request, CancellationToken ct) =>
			{
				capturedRequest = request;
				return Task.FromResult(response);
			});

			using var handler = stub.Object;
			using var client = new HttpClient(handler);
			await client.GetAsync(new Uri("https://localhost.com/test"));

			Assert.NotNull(capturedRequest);
			Assert.Equal(HttpMethod.Get, capturedRequest.Method);
		}

		#endregion
	}
}
