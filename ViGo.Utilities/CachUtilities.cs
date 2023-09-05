﻿using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Utilities
{
    public static class CachUtilities
    {
        public static async Task SetAsync(this IDistributedCache cache,
            string key, object value, CancellationToken cancellationToken = default)
        {
            if (value is null)
            {
                return;
            }
            string jsonValue = JsonConvert.SerializeObject(
                value, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                });
            byte[] encodedValue = Encoding.UTF8.GetBytes(jsonValue);
            var options = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(DateTime.Now.AddHours(2))
                .SetSlidingExpiration(TimeSpan.FromHours(1.5));
            await cache.SetAsync(key, encodedValue, options, cancellationToken);
        }

        public static async Task<T> GetAsync<T>(this IDistributedCache cache,
            string key, CancellationToken cancellationToken = default)
        {
            byte[] encodedValues = await cache.GetAsync(key, cancellationToken);
            if (encodedValues is null)
            {
                return default;
            }
            string jsonValue = Encoding.UTF8.GetString(encodedValues);
            return JsonConvert.DeserializeObject<T>(jsonValue, new JsonSerializerSettings
            {
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
            });
        }
    }
}