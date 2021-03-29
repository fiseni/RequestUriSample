using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

const string Scheme = "http";
const string Host = "host.com";
const int Port = 5000;
const string PathPart1 = "pathpart1";
const string PathPart2 = "pathpart2";
const string FieldName1 = "fieldname1";
const string FieldName2 = "FieldName2";
const string FieldValue1 = "field<>Value1";
const string FieldValue2 = "field<>Value2";
const string FieldValueEncoded1 = "field%3C%3EValue1";
const string FieldValueEncoded2 = "field%3C%3EValue2";
const string Fragment = "frag";
const string Username = "username";
const string Password = "password";

// Regardeless how complex is our internal structure, the usage should be simple.
// The users don't care if there are some sub types. 
// The users just wants to provide the value that matters to them (e.g. PathPart1, FieldName, etc).
// They should have the ability to define the types explicitly, but they shouldn't be enforced to do so.
var uriString = new AbsoluteRequestUri(Host, Scheme, Port)
                    .WithPath(PathPart1, PathPart2)
                    .WithQuery(
                        (FieldName1, FieldValue1),
                        (FieldName2, FieldValue2))
                    .WithFragments(Fragment)
                    .WithUser(Username, Password)
                    .ToUriString();

var expected = $"{Scheme}://{Username}:{Password}@{Host}:{Port}/{PathPart1}/{PathPart2}?" +
            $"{FieldName1}={FieldValueEncoded1}&{FieldName2}={FieldValueEncoded2}#{Fragment}";

Console.WriteLine($"Result are equal: {expected.Equals(uriString)}");



// Definitions
// ---------------------------------------------
public static class RequestUriExtensions
{
    public static AbsoluteRequestUri WithPath(this AbsoluteRequestUri source, RequestUriPath path)
    {
        _ = path ?? throw new ArgumentNullException(nameof(path));

        return new AbsoluteRequestUri(source.Host, new RelativeRequestUri(path, source.RequestUri?.Query, source.RequestUri?.Fragment), source.Scheme, source.Port, source.UserInfo);
    }

    public static AbsoluteRequestUri WithPath(this AbsoluteRequestUri source, IEnumerable<string> pathElements)
    {
        _ = pathElements ?? throw new ArgumentNullException(nameof(pathElements));

        return new AbsoluteRequestUri(source.Host, new RelativeRequestUri(new RequestUriPath(pathElements), source.RequestUri?.Query, source.RequestUri?.Fragment), source.Scheme, source.Port, source.UserInfo);
    }

    public static AbsoluteRequestUri WithPath(this AbsoluteRequestUri source, params string[] pathElements)
    {
        _ = pathElements ?? throw new ArgumentNullException(nameof(pathElements));

        return new AbsoluteRequestUri(source.Host, new RelativeRequestUri(new RequestUriPath(pathElements), source.RequestUri?.Query, source.RequestUri?.Fragment), source.Scheme, source.Port, source.UserInfo);
    }

    public static AbsoluteRequestUri WithQuery(this AbsoluteRequestUri source, Query query)
    {
        _ = query ?? throw new ArgumentNullException(nameof(query));

        return new AbsoluteRequestUri(source.Host, new RelativeRequestUri(source.RequestUri?.Path, query, source.RequestUri?.Fragment), source.Scheme, source.Port, source.UserInfo);
    }

    public static AbsoluteRequestUri WithQuery(this AbsoluteRequestUri source, IEnumerable<QueryParameter> queryParameters)
    {
        _ = queryParameters ?? throw new ArgumentNullException(nameof(queryParameters));

        return new AbsoluteRequestUri(source.Host, new RelativeRequestUri(source.RequestUri?.Path, new Query(queryParameters), source.RequestUri?.Fragment), source.Scheme, source.Port, source.UserInfo);
    }

    public static AbsoluteRequestUri WithQuery(this AbsoluteRequestUri source, params QueryParameter[] queryParameters)
    {
        _ = queryParameters ?? throw new ArgumentNullException(nameof(queryParameters));

        return new AbsoluteRequestUri(source.Host, new RelativeRequestUri(source.RequestUri?.Path, new Query(queryParameters), source.RequestUri?.Fragment), source.Scheme, source.Port, source.UserInfo);
    }

    public static AbsoluteRequestUri WithQuery(this AbsoluteRequestUri source, params (string FieldName, string FieldValue)[] queryParameters)
    {
        _ = queryParameters ?? throw new ArgumentNullException(nameof(queryParameters));

        return new AbsoluteRequestUri(source.Host, new RelativeRequestUri(source.RequestUri?.Path, new Query(queryParameters), source.RequestUri?.Fragment), source.Scheme, source.Port, source.UserInfo);
    }

    public static AbsoluteRequestUri WithFragments(this AbsoluteRequestUri source, string fragment)
    {
        _ = fragment ?? throw new ArgumentNullException(nameof(fragment));

        return new AbsoluteRequestUri(source.Host, new RelativeRequestUri(source.RequestUri?.Path, source.RequestUri?.Query, fragment), source.Scheme, source.Port, source.UserInfo);
    }

    public static AbsoluteRequestUri WithUser(this AbsoluteRequestUri source, UserInfo userInfo)
    {
        _ = userInfo ?? throw new ArgumentNullException(nameof(userInfo));

        return new AbsoluteRequestUri(source.Host, source.RequestUri, source.Scheme, source.Port, userInfo);
    }

    public static AbsoluteRequestUri WithUser(this AbsoluteRequestUri source, string username, string password)
    {
        if (string.IsNullOrEmpty(username)) throw new ArgumentNullException(nameof(username));
        if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));

        return new AbsoluteRequestUri(source.Host, source.RequestUri, source.Scheme, source.Port, new UserInfo(username, password));
    }

    public static string ToUriString(this AbsoluteRequestUri absoluteRequestUri)
    {
        _ = absoluteRequestUri ?? throw new ArgumentNullException(nameof(absoluteRequestUri));

        return
            $"{(string.IsNullOrEmpty(absoluteRequestUri.Scheme) ? "http" : absoluteRequestUri.Scheme)}://" +
            $"{(absoluteRequestUri.UserInfo != null ? $"{absoluteRequestUri.UserInfo.Username}:{absoluteRequestUri.UserInfo.Password}@" : "")}" +
            $"{absoluteRequestUri.Host}" +
            (absoluteRequestUri.Port.HasValue ? $":{absoluteRequestUri.Port.Value}" : "") +
            absoluteRequestUri.RequestUri.ToUriString();
    }

    public static string ToUriString(this RelativeRequestUri relativeRequestUri)
    {
        _ = relativeRequestUri ?? throw new ArgumentNullException(nameof(relativeRequestUri));

        return (relativeRequestUri.Path.Elements.Count() > 0 ? $"/{string.Join("/", relativeRequestUri.Path.Elements)}" : "") +
                (relativeRequestUri.Query.Elements.Count() > 0 ? $"?{string.Join("&", relativeRequestUri.Query.Elements.Select(e => $"{e.FieldName}={WebUtility.UrlEncode(e.Value)}"))}" : "") +
                (!string.IsNullOrEmpty(relativeRequestUri.Fragment) ? $"#{relativeRequestUri.Fragment}" : "");
    }
}

public class RelativeRequestUri
{
    public RequestUriPath Path { get; }
    public Query Query { get; }
    public string Fragment { get; }

    internal RelativeRequestUri(
        RequestUriPath path = null,
        Query query = null,
        string fragment = null)
    {
        this.Path = path;
        this.Query = query;
        this.Fragment = fragment;
    }
}

public class AbsoluteRequestUri
{
    public string Host { get; }
    public RelativeRequestUri RequestUri { get; }
    public string Scheme { get; }
    public int? Port { get; }
    public UserInfo UserInfo { get; set; }

    public AbsoluteRequestUri(
        string host,
        string scheme = null,
        int? port = null)
    {
        if (string.IsNullOrEmpty(host)) throw new ArgumentNullException(nameof(host));

        this.Host = host;
        this.Scheme = scheme;
        this.Port = port;
    }

    internal AbsoluteRequestUri(
        string host,
        RelativeRequestUri requestUri = null,
        string scheme = null,
        int? port = null,
        UserInfo userInfo = null)
        : this(host, scheme, port)
    {
        this.RequestUri = requestUri;
        this.UserInfo = userInfo;
    }
}

public class RequestUriPath
{
    private readonly List<string> _elements = new List<string>();
    public IEnumerable<string> Elements => _elements.AsReadOnly();

    public RequestUriPath(IEnumerable<string> elements)
    {
        _ = elements ?? throw new ArgumentNullException(nameof(elements));

        _elements.AddRange(elements);
    }

}

public class Query
{
    private readonly List<QueryParameter> _elements = new List<QueryParameter>();
    public IEnumerable<QueryParameter> Elements => _elements.AsReadOnly();

    public Query(IEnumerable<QueryParameter> elements)
    {
        _ = elements ?? throw new ArgumentNullException(nameof(elements));

        _elements.AddRange(elements);
    }

    public Query(IEnumerable<(string FieldName, string FieldValue)> elements)
    {
        _ = elements ?? throw new ArgumentNullException(nameof(elements));

        _elements.AddRange(elements.Select(x => new QueryParameter(x.FieldName, x.FieldValue)));
    }

}

public class QueryParameter
{
    public string FieldName { get; }
    public string Value { get; set; }

    public QueryParameter(string fieldName, string value)
    {
        if (string.IsNullOrEmpty(fieldName)) throw new ArgumentNullException(nameof(fieldName));
        if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));

        this.FieldName = fieldName;
        this.Value = value;
    }
}

public class UserInfo
{
    public string Username { get; }
    public string Password { get; set; }

    public UserInfo(string username, string password)
    {
        if (string.IsNullOrEmpty(username)) throw new ArgumentNullException(nameof(username));
        if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));

        this.Username = username;
        this.Password = password;
    }
}
