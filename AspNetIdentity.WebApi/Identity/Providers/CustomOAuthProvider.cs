﻿namespace AspNetIdentity.WebApi.Identity.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Threading.Tasks;

    using AspNetIdentity.WebApi.Models;
    using AspNetIdentity.WebApi.Models.Auth.Identity;
    using AspNetIdentity.WebApi.Services;
    using Autofac;
    using Autofac.Integration.Owin;
    using Microsoft.Owin.Security;
    using Microsoft.Owin.Security.OAuth;

    public class CustomOAuthProvider : OAuthAuthorizationServerProvider
    {
        private IUserService userService;

        public override Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            var clientId = string.Empty;
            var clientSecret = string.Empty;

            if (!context.TryGetBasicCredentials(out clientId, out clientSecret))
            {
                context.TryGetFormCredentials(out clientId, out clientSecret);
            }

            if (context.ClientId == null)
            {
                context.SetError("invalid_clientId", "client_Id is not set");
                return Task.FromResult<object>(null);
            }

            var audience = AudiencesStore.FindAudience(context.ClientId);

            if (audience == null)
            {
                context.SetError("invalid_clientId", $"Invalid client_id '{context.ClientId}'");
                return Task.FromResult<object>(null);
            }

            context.Validated();
            return Task.FromResult<object>(null);
        }

        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            var userManager = GetUserManager(context);
            XUser user = await userManager.FindAsync(context.UserName, context.Password);

            if (user == null)
            {
                context.SetError("invalid_grant", "The user name or password is incorrect.");
                return;
            }

            // TODO: Check if user is active (not locked)
            //if (!user.IsActive)
            //{
            //    context.SetError("account_locked", "Your account has been locked");
            //    return;
            //}

            user.SecurityStamp = Guid.NewGuid().ToString();


            ClaimsIdentity oAuthIdentity = await user.GenerateUserIdentityAsync(userManager, OAuthDefaults.AuthenticationType);
            oAuthIdentity.AddClaims(ExtendedClaimsProvider.GetClaims(user));
            oAuthIdentity.AddClaims(ExtendedClaimsProvider.CreateRolesBasedOnClaims(oAuthIdentity));

            var props = new AuthenticationProperties(new Dictionary<string, string>
            {
                {
                    "audience", (context.ClientId == null) ? string.Empty : context.ClientId
                }
            });

            var ticket = new AuthenticationTicket(oAuthIdentity, props);
            context.Validated(ticket);
        }

        public override Task GrantRefreshToken(OAuthGrantRefreshTokenContext context)
        {
            var originalClient = context.Ticket.Properties.Dictionary["audience"];
            var currentClient = context.ClientId;

            // enforce client binding of refresh token
            if (originalClient != currentClient)
            {
                context.SetError("invalid_clientId", "Refresh token is issued to a different clientId.");
                return Task.FromResult<object>(null);
            }

            // chance to change authentication ticket for refresh token requests
            var newIdentity = new ClaimsIdentity(context.Ticket.Identity);

            var newTicket = new AuthenticationTicket(newIdentity, context.Ticket.Properties);
            context.Validated(newTicket);

            return Task.FromResult<object>(null);
        }

        private static XUserManager GetUserManager(OAuthGrantResourceOwnerCredentialsContext context)
        {
            return context.OwinContext.GetAutofacLifetimeScope().Resolve<XUserManager>();
        }
    }
}