﻿// Any comments, input: @KevinDockx
// Any issues, requests: https://github.com/KevinDockx/HttpCacheHeaders

using Marvin.Cache.Headers.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marvin.Cache.Headers.Stores
{
    /// <summary>
    /// In-memory implementation of <see cref="IValidatorValueStore"/>.
    /// </summary>
    public class InMemoryValidatorValueStore : IValidatorValueStore
    {
        // store for validatorvalues
        private readonly ConcurrentDictionary<string, ValidatorValue> _store
            = new ConcurrentDictionary<string, ValidatorValue>();

        // store for storekeys - different store to speed up search 
        private readonly ConcurrentDictionary<string, StoreKey> _storeKeyStore
            = new ConcurrentDictionary<string, StoreKey>();

        public Task<ValidatorValue> GetAsync(StoreKey key) => GetAsync(key.ToString());

        private Task<ValidatorValue> GetAsync(string key)
        {
            return _store.ContainsKey(key) && _store[key] is ValidatorValue eTag
                ? Task.FromResult(eTag)
                : Task.FromResult<ValidatorValue>(null);
        }

        /// <summary>
        /// Add an item to the store (or update it)
        /// </summary>
        /// <param name="key">The <see cref="StoreKey"/>.</param>
        /// <param name="eTag">The <see cref="ValidatorValue"/>.</param>
        /// <returns></returns>
        public Task SetAsync(StoreKey key, ValidatorValue eTag)
        {
            // store the validator value
            _store[key.ToString()] = eTag;
            // save the key itself as well, with an easily searchable stringified key
            _storeKeyStore[key.ToString()] = key;
            return Task.FromResult(0);
        }

        /// <summary>
        /// Remove an item from the store
        /// </summary>
        /// <param name="key">The <see cref="StoreKey"/>.</param>
        /// <returns></returns>
        public Task<bool> RemoveAsync(StoreKey key)
        {
            _storeKeyStore.TryRemove(key.ToString(), out _);
            return Task.FromResult(_store.TryRemove(key.ToString(), out _));
        }

        /// <summary>
        /// Find store keys
        /// </summary>
        /// <param name="valueToMatch">The value to match as (part of) the key</param>
        /// <returns></returns>
        public Task<IEnumerable<StoreKey>> FindStoreKeysByKeyPartAsync(string valueToMatch, 
            bool ignoreCase)
        {
            var lstStoreKeysToReturn = new List<StoreKey>();

            // search for keys that contain valueToMatch
            if (ignoreCase)
            {
                valueToMatch = valueToMatch.ToLowerInvariant();

                foreach (var key in _storeKeyStore.Keys
                .Where(k => k.ToLowerInvariant().Contains(valueToMatch)))
                {
                    if (_storeKeyStore.TryGetValue(key, out StoreKey storeKey))
                    {
                        lstStoreKeysToReturn.Add(storeKey);
                    }
                }
            }
            else
            {
                foreach (var key in _storeKeyStore.Keys
                 .Where(k => k.Contains(valueToMatch)))
                {
                    if (_storeKeyStore.TryGetValue(key, out StoreKey storeKey))
                    {
                        lstStoreKeysToReturn.Add(storeKey);
                    }
                }
            }            

            return Task.FromResult(lstStoreKeysToReturn.AsEnumerable());
        }
    }
}
