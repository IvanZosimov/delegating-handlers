using System.Net;

namespace rm.DelegatingHandlers;

public static class HttpStatusCodeIntExtensions
{
	public static bool Is1xx(this int statusCode)
	{
		return ((HttpStatusCode)statusCode).Is1xx();
	}

	public static bool Is2xx(this int statusCode)
	{
		return ((HttpStatusCode)statusCode).Is2xx();
	}

	public static bool Is3xx(this int statusCode)
	{
		return ((HttpStatusCode)statusCode).Is3xx();
	}

	public static bool Is4xx(this int statusCode)
	{
		return ((HttpStatusCode)statusCode).Is4xx();
	}

	public static bool Is5xx(this int statusCode)
	{
		return ((HttpStatusCode)statusCode).Is5xx();
	}

	/// <summary>
	/// Returns true if status code is a client error status code (4xx).
	/// </summary>
	public static bool IsClientErrorStatusCode(this int statusCode)
	{
		return statusCode.Is4xx();
	}

	/// <summary>
	/// Returns true if status code is a server error status code (5xx).
	/// </summary>
	public static bool IsServerErrorStatusCode(this int statusCode)
	{
		return statusCode.Is5xx();
	}

	/// <summary>
	/// Returns true if status code is an error status code (4xx, 5xx).
	/// </summary>
	public static bool IsErrorStatusCode(this int statusCode)
	{
		return statusCode.Is4xx() || statusCode.Is5xx();
	}
}
