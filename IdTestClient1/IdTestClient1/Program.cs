﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ConsoleTestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            const string urlBase = "http://localhost:55509";

            //authorization server parameters owned from the client
            //this values are issued from the authorization server to the client through a separate process (registration, etc...)
            Uri authorizationServerTokenIssuerUri = new Uri(urlBase + "/connect/token");
            string clientId = "ClientIdThatCanOnlyRead";
            string clientSecret = "secret1";
            string scope = "scope.readaccess";

            //access token request
            string rawJwtToken = RequestTokenToAuthorizationServer(
                 authorizationServerTokenIssuerUri,
                 clientId,
                 scope,
                 clientSecret)
                .GetAwaiter()
                .GetResult();

            AuthorizationServerAnswer authorizationServerToken;
            authorizationServerToken = Newtonsoft.Json.JsonConvert.DeserializeObject<AuthorizationServerAnswer>(rawJwtToken);

            Console.WriteLine("Token acquired from Authorization Server:");
            Console.WriteLine(authorizationServerToken.access_token);

            //secured web api request
            string response = RequestValuesToSecuredWebApi(authorizationServerToken)
                .GetAwaiter()
                .GetResult();

            Console.WriteLine("Response received from WebAPI:");
            Console.WriteLine(response);
            Console.ReadKey();
        }

        private static async Task<string> RequestTokenToAuthorizationServer(Uri uriAuthorizationServer, string clientId, string scope, string clientSecret)
        {
            HttpResponseMessage responseMessage;
            using (HttpClient client = new HttpClient())
            {
                HttpRequestMessage tokenRequest = new HttpRequestMessage(HttpMethod.Post, uriAuthorizationServer);
                HttpContent httpContent = new FormUrlEncodedContent(
                    new[]
                    {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("scope", scope),
                    new KeyValuePair<string, string>("client_secret", clientSecret)
                    });
                tokenRequest.Content = httpContent;
                responseMessage = await client.SendAsync(tokenRequest);
            }
            return await responseMessage.Content.ReadAsStringAsync();
        }

        private static async Task<string> RequestValuesToSecuredWebApi(AuthorizationServerAnswer authorizationServerToken)
        {
            HttpResponseMessage responseMessage;
            using (HttpClient httpClient = new HttpClient())
            {
                const string urlBase = "http://localhost:55509";
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authorizationServerToken.access_token);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, urlBase + "/api/values");
                responseMessage = await httpClient.SendAsync(request);
            }

            return await responseMessage.Content.ReadAsStringAsync();
        }

        private class AuthorizationServerAnswer
        {
            public string access_token { get; set; }
            public string expires_in { get; set; }
            public string token_type { get; set; }

        }
    }
}