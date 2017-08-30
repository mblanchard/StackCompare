using StackCompare.Representations.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using StackCompare.Representations.Models;
using System.Net.Http;
using AngleSharp.Parser.Html;
using AngleSharp.Dom.Html;
using System.Linq;
using StackCompare.Representations.Configuration;
using System.Net.Http.Headers;
using AngleSharp.Dom;

namespace StackCompare.StackShareIntegration
{
    public class StackShareClient : IStackShareClient
    {
        private const string BASE_URL = "https://stackshare.io";
        public IEnumerable<Organization> GetMatchingOrgs(List<Tool> tools, GitHubConfig config)
        {
            using (var clientHandler = new HttpClientHandler())
            using (var client = new HttpClient(clientHandler))
            {
                client.BaseAddress = new Uri(BASE_URL);
                var isAuthenticated = IsAlreadyAuthenticated(client) || AuthenticateSession(client, clientHandler, config);
                if (isAuthenticated)
                {
                    IEnumerable<Organization> orgs = null;
                    foreach(var tool in tools)
                    {
                        orgs = orgs == null ? GetOrgsForTool(client, clientHandler, tool): orgs.Intersect(GetOrgsForTool(client, clientHandler, tool));
                    }
                    return orgs;
                }
                return null;
            }
        }

        private Uri GetUriForToolStacks(Tool tool)
        {
            return new Uri($"{BASE_URL}/{tool.Name}/in-stacks");          
        }

        private bool IsAlreadyAuthenticated(HttpClient client)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "");
            var resultTask = client.SendAsync(req);
            resultTask.Wait();
            HttpResponseMessage result = resultTask.Result;
            var parseTask = result.Content.ReadAsStringAsync();
            parseTask.Wait();
            string serializedResponse = parseTask.Result;
            return !serializedResponse.Contains("Sign up / Login");
        }

        private List<Organization> GetOrgsForTool(HttpClient client,HttpClientHandler clientHandler,  Tool tool)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, GetUriForToolStacks(tool));
            var resultTask = client.SendAsync(req);
            resultTask.Wait();
            HttpResponseMessage result = resultTask.Result;
            var parseTask = result.Content.ReadAsStringAsync();
            parseTask.Wait();
            string serializedResponse = parseTask.Result;
            var doc = new HtmlParser().Parse(serializedResponse);


            var companiesSection = doc.GetElementsByClassName("companies-using-service").FirstOrDefault();

            var orgs = companiesSection?.GetElementsByTagName("a")?.Select(parseOrgElement).ToList();



            var loadMoreBtn = doc.GetElementById("service-stacks-load-more");
            if (loadMoreBtn?.Attributes["data-service-id"] != null)
            {
                var cookies = clientHandler.CookieContainer.GetCookies(new Uri(BASE_URL));
                var serviceId = loadMoreBtn.Attributes["data-service-id"].Value;
                var token = cookies["XSRF-TOKEN"]?.Value;
                var hasMore = true;
                int page = 2;
                while (hasMore)
                {
                    var nextPageOrgs = GetOrgPageForTool(client, serviceId, page, System.Net.WebUtility.UrlDecode(token));
                    if (nextPageOrgs.Any()) { orgs.AddRange(nextPageOrgs); }
                    else break;
                    page++;
                }

            }
            return orgs;
        }

        private List<Organization> GetOrgPageForTool(HttpClient client, string toolId, int page, string token)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, " https://stackshare.io/service-stacks-load-more");
            req.Content = new StringContent($"page={page}&service_id={toolId}");
            req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            req.Content.Headers.TryAddWithoutValidation("Pragma", "no-cache");
            req.Content.Headers.TryAddWithoutValidation("Cache-Control", "no-cache");
            req.Content.Headers.TryAddWithoutValidation("X-CSRF-Token", token);
            var resultTask = client.SendAsync(req);
            resultTask.Wait();
            HttpResponseMessage result = resultTask.Result;
            var parseTask = result.Content.ReadAsStringAsync();
            parseTask.Wait();
            string serializedResponse = parseTask.Result;
            var doc = new HtmlParser().Parse(serializedResponse);
            return doc?.GetElementsByTagName("a")?.Select(parseOrgElement).ToList();
        }

        private Organization parseOrgElement(IElement element)
        {
            var relativeUri = ((IHtmlAnchorElement)element)?.Href.Replace("about://", "");
            var uri = new Uri(BASE_URL + relativeUri);
            return new Organization() { Name = element?.Attributes["data-hint"]?.Value, Uri = uri  };
        }

        private bool AuthenticateSession(HttpClient client, HttpClientHandler clientHandler, GitHubConfig config)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, $"{BASE_URL}/users/auth/github");

            var resultTask = client.SendAsync(req);
            resultTask.Wait();
            HttpResponseMessage result = resultTask.Result;
            var parseTask = result.Content.ReadAsStringAsync();
            parseTask.Wait();
            string serializedResponse = parseTask.Result;
            var doc = new HtmlParser().Parse(serializedResponse);
            var el = (IHtmlInputElement) doc.GetElementsByName("authenticity_token").FirstOrDefault();
            if(el != null)
            {
               return GitHubOAuth(client, clientHandler, el.Value, config.Username, config.Password);
            }
            return false;
        }

        private bool GitHubOAuth(HttpClient client, HttpClientHandler clientHandler, string authToken, string username, string password)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "https://github.com/session");
            req.Content = new StringContent($"commit=Sign+in&utf8=%E2%9C%93&authenticity_token={System.Net.WebUtility.UrlEncode(authToken)}&login={username}&password={password}");
            req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            var resultTask = client.SendAsync(req);
            resultTask.Wait();
            HttpResponseMessage result = resultTask.Result;
            var parseTask = result.Content.ReadAsStringAsync();
            parseTask.Wait();
            string serializedResponse = parseTask.Result;

            if(serializedResponse.Contains("This application has made an unusually high number of requests to access your account"))
            {
                //Handle manual reauth
            }
            var doc = new HtmlParser().Parse(serializedResponse);
            var alerts = doc.GetElementsByClassName("alert-notice");
            return alerts.Any(x => x.TextContent.Contains("authenticated"));
        }

    }

}
