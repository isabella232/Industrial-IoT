﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using RestSharp;
    using RestSharp.Authenticators;
    using TestModels;
    using Xunit;

    internal static class TestHelper {

        /// <summary>
        /// Read the base URL of Industrial IoT Platform from environment variables
        /// </summary>
        /// <returns></returns>
        public static string GetBaseUrl() {
            var baseUrl = Environment.GetEnvironmentVariable(TestConstants.EnvironmentVariablesNames.PCS_SERVICE_URL);
            Assert.True(!string.IsNullOrWhiteSpace(baseUrl), "baseUrl is null");
            return baseUrl;
        }

        /// <summary>
        /// Request OAuth token using Http basic authentication from environment variables
        /// </summary>
        /// <returns>Return content of request token or empty string</returns>
        public static async Task<string> GetTokenAsync() {
            return await GetTokenAsync(
                Environment.GetEnvironmentVariable(TestConstants.EnvironmentVariablesNames.PCS_AUTH_TENANT),
                Environment.GetEnvironmentVariable(TestConstants.EnvironmentVariablesNames.PCS_AUTH_CLIENT_APPID),
                Environment.GetEnvironmentVariable(TestConstants.EnvironmentVariablesNames.PCS_AUTH_CLIENT_SECRET),
                Environment.GetEnvironmentVariable(TestConstants.EnvironmentVariablesNames.ApplicationName)
            );
        }

        /// <summary>
        /// Request OAuth token using Http basic authentication
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="clientId">User name for HTTP basic authentication</param>
        /// <param name="clientSecret">Password for HTTP basic authentication</param>
        /// <param name="applicationName">Name of deployed Industrial IoT</param>
        /// <returns>Return content of request token or empty string</returns>
        public static async Task<string> GetTokenAsync(string tenantId, string clientId, string clientSecret, string applicationName) {
            
            Assert.True(!string.IsNullOrWhiteSpace(tenantId), "tenantId is null");
            Assert.True(!string.IsNullOrWhiteSpace(clientId), "clientId is null");
            Assert.True(!string.IsNullOrWhiteSpace(clientSecret), "clientSecret is null");
            Assert.True(!string.IsNullOrWhiteSpace(applicationName), "applicationName is null");

            var client = new RestClient($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token") {
                Timeout = 30000, 
                Authenticator = new HttpBasicAuthenticator(clientId, clientSecret)
            };

            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("grant_type", "client_credentials");
            request.AddParameter("scope", $"https://{tenantId}/{applicationName}-service/.default");
            
            var response = await client.ExecuteAsync(request);
            Assert.True(response.IsSuccessful, $"Request OAuth2.0 failed, Status {response.StatusCode}, ErrorMessage: {response.ErrorMessage}");
            dynamic json = JsonConvert.DeserializeObject(response.Content);
            return $"{json.token_type} {json.access_token}";
        }

        /// <summary>
        /// Read PublishedNodes json from OPC-PLC and provide the data to the tests
        /// </summary>
        /// <returns>Dictionary with URL of PLC-PLC as key and Content of Published Nodes files as value</returns>
        public static async Task<IDictionary<string, PublishedNodesEntryModel>> GetSimulatedOpcUaNodesAsync() {
            var result = new Dictionary<string, PublishedNodesEntryModel>();

            var plcUrls = Environment.GetEnvironmentVariable(TestConstants.EnvironmentVariablesNames.PLC_SIMULATION_URLS);
            Assert.NotNull(plcUrls);
            
            var listOfUrls = plcUrls.Split(TestConstants.SimulationUrlsSeparator);
            
            foreach (var url in listOfUrls.Where(s => !string.IsNullOrWhiteSpace(s))) {
                using (var client = new HttpClient()) {
                    var ub = new UriBuilder {Host = url};
                    var baseAddress = ub.Uri;

                    client.BaseAddress = baseAddress;

                    using (var response = await client.GetAsync(TestConstants.OpcSimulation.PublishedNodesFile)) {
                        Assert.NotNull(response);
                        var json = await response.Content.ReadAsStringAsync();
                        Assert.NotEmpty(json);
                        var entryModels = JsonConvert.DeserializeObject<PublishedNodesEntryModel[]>(json);
                        
                        Assert.NotNull(entryModels);
                        Assert.NotEmpty(entryModels);
                        Assert.NotNull(entryModels[0].OpcNodes);
                        Assert.NotEmpty(entryModels[0].OpcNodes);
                        
                        result.Add(url, entryModels[0]);
                    }
                }
            }
            
            return result;
        }
    }
}