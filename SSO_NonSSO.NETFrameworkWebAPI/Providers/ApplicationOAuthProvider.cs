using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using SSO_NonSSO.NETFrameworkWebAPI.Models;

namespace SSO_NonSSO.NETFrameworkWebAPI.Providers
{
    public class ApplicationOAuthProvider : OAuthAuthorizationServerProvider
    {
        private readonly string _publicClientId;

        public ApplicationOAuthProvider(string publicClientId)
        {
            if (publicClientId == null)
            {
                throw new ArgumentNullException("publicClientId");
            }

            _publicClientId = publicClientId;
        }

        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            var userManager = context.OwinContext.GetUserManager<ApplicationUserManager>();

            ApplicationUser user;

            if (!string.IsNullOrWhiteSpace(context.Request.Headers["IdentityCode"]))
            {
                user = await userManager.FindByEmailAsync(
                    await GetEmailFromIdentityCodeAsync(context.Request.Headers["IdentityCode"])
                    );
            }
            else
            {
                user = await userManager.FindAsync(context.UserName, context.Password);
            }

            if (user == null)
            {
                context.SetError("invalid_grant", "The user name or password is incorrect.");
                return;
            }

            ClaimsIdentity oAuthIdentity = await user.GenerateUserIdentityAsync(userManager,
               OAuthDefaults.AuthenticationType);
            ClaimsIdentity cookiesIdentity = await user.GenerateUserIdentityAsync(userManager,
                CookieAuthenticationDefaults.AuthenticationType);

            AuthenticationProperties properties = CreateProperties(user.UserName);
            AuthenticationTicket ticket = new AuthenticationTicket(oAuthIdentity, properties);
            context.Validated(ticket);
            context.Request.Context.Authentication.SignIn(cookiesIdentity);
        }

        private async Task<string> GetEmailFromIdentityCodeAsync(string identityCode)
        {
            var clientId = "<Client Id>";
            var clientSecret = "<Client Secret>";
            var tenantId = "<Tenant Id>";
            var scope = "<Scope>";
            var redirectURI = "<Redirect URI>";

            var options = new RestClientOptions("https://login.microsoftonline.com")
            {
                MaxTimeout = -1,
            };
            var client = new RestClient(options);
            var request = new RestRequest($"/{tenantId}/oauth2/v2.0/token", Method.Post);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("client_id", clientId);
            request.AddParameter("scope", scope);
            request.AddParameter("code", identityCode);
            request.AddParameter("redirect_uri", redirectURI);
            request.AddParameter("grant_type", "authorization_code");
            request.AddParameter("client_secret", clientSecret);
            var response = await client.ExecuteAsync(request);
            var accessToken = JObject.Parse(response.Content)["access_token"];

            if (accessToken == null)
            {
                throw new InvalidOperationException("Identity code is expired.");
            }

            return new JwtSecurityToken(accessToken.ToString()).Claims.First(c => c.Type == "upn").Value;
        }

        public override Task TokenEndpoint(OAuthTokenEndpointContext context)
        {
            foreach (KeyValuePair<string, string> property in context.Properties.Dictionary)
            {
                context.AdditionalResponseParameters.Add(property.Key, property.Value);
            }

            return Task.FromResult<object>(null);
        }

        public override Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            // Resource owner password credentials does not provide a client ID.
            if (context.ClientId == null)
            {
                context.Validated();
            }

            return Task.FromResult<object>(null);
        }

        public override Task ValidateClientRedirectUri(OAuthValidateClientRedirectUriContext context)
        {
            if (context.ClientId == _publicClientId)
            {
                Uri expectedRootUri = new Uri(context.Request.Uri, "/");

                if (expectedRootUri.AbsoluteUri == context.RedirectUri)
                {
                    context.Validated();
                }
            }

            return Task.FromResult<object>(null);
        }

        public static AuthenticationProperties CreateProperties(string userName)
        {
            IDictionary<string, string> data = new Dictionary<string, string>
            {
                { "userName", userName }
            };
            return new AuthenticationProperties(data);
        }
    }
}