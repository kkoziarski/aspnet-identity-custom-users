﻿namespace AspNetIdentity.WebApi.Identity.Providers
{
    using System;
    using System.Collections.Concurrent;
    using System.Security.Cryptography;
    using Microsoft.Owin.Security.DataHandler.Encoder;

    public static class AudiencesStore
    {
        private static ConcurrentDictionary<string, Audience> AudiencesList = new ConcurrentDictionary<string, Audience>();

        public static Audience DefaultAudience
        {
            get
            {
                var audience = new Audience
                {
                    Name = JwtConfigurationProvider.AudienceName,
                    AudienceId = JwtConfigurationProvider.AudienceId,
                    Base64Secret = JwtConfigurationProvider.AudienceBase64Secret
                };

                return audience;
            }
        }

        static AudiencesStore()
        {
            var defaultAudience = DefaultAudience;
            AudiencesList.TryAdd(defaultAudience.AudienceId, defaultAudience);
        }

        public static Audience AddAudience(string name)
        {
            var clientId = Guid.NewGuid().ToString("N");

            var key = new byte[32];
            RNGCryptoServiceProvider.Create().GetBytes(key);
            var base64Secret = TextEncodings.Base64Url.Encode(key);

            Audience newAudience = new Audience { AudienceId = clientId, Base64Secret = base64Secret, Name = name };
            AudiencesList.TryAdd(clientId, newAudience);
            return newAudience;
        }

        public static Audience FindAudience(string clientId)
        {
            Audience audience = null;
            if (AudiencesList.TryGetValue(clientId, out audience))
            {
                return audience;
            }
            return null;
        }
    }

    public class Audience
    {
        public string AudienceId { get; set; }
        public string Base64Secret { get; set; }
        public string Name { get; set; }
    }
}