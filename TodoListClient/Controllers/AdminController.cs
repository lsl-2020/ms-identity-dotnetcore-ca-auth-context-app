﻿using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using TodoListClient.Models;
using TodoListClient.Common;
using Microsoft.Identity.Web;

namespace TodoListClient.Controllers
{
    /// <summary>
    /// Functionality for Admin user account.
    /// Admin will add the Authentication context for the tenant
    /// Save and update data in database.
    /// </summary>
    [Authorize]
    [AuthorizeForScopes(Scopes = ["Policy.ReadWrite.ConditionalAccess"])]
    public class AdminController : Controller
    {
        private readonly AuthenticationContextClassReferencesOperations _authContextClassReferencesOperations;
        private readonly IConfiguration _configuration;

        private readonly string TenantId;

        public AdminController(
            IConfiguration configuration,
            AuthenticationContextClassReferencesOperations authContextClassReferencesOperations)
        {
            _configuration = configuration;
            _authContextClassReferencesOperations = authContextClassReferencesOperations;
            TenantId = _configuration["AzureAd:TenantId"];
        }

        public async Task<IActionResult> Index()
        {
            // Defaults
            IList<SelectListItem> AuthContextValues = [];

            IEnumerable<SelectListItem> Operations = new List<SelectListItem>
                {
                    new() { Text= "Post" },
                    new() { Text= "Delete" }
                };

            // If this tenant already has auth context available, we use those instead.
            var existingAuthContexts = await GetAuthenticationContextValues();

            if (existingAuthContexts.Count > 0)
            {
                AuthContextValues.Clear();

                foreach (var authContext in existingAuthContexts)
                {
                    AuthContextValues.Add(new SelectListItem() { Text = authContext.Value, Value = authContext.Key });
                }
            }

            // Set data to be used in the UI
            TempData["TenantId"] = TenantId;
            TempData["AuthContextValues"] = AuthContextValues;
            TempData["Operations"] = Operations;

            return View();
        }

        // Returns a default set of AuthN context values for the app to work with, either from Graph a or a default hard coded set
        private async Task<Dictionary<string, string>> GetAuthenticationContextValues()
        {
            // Default values, if no values anywhere, this table will be used.
            Dictionary<string, string> dictACRValues = new()
                {
                    {"C1","Require strong authentication" },
                    {"C2","Require compliant devices" },
                    {"C3","Require trusted locations" }
            };

            string sessionKey = "ACRS";

            // If already saved in Session, use it
            if (HttpContext.Session.Get<Dictionary<string, string>>(sessionKey) != default)
            {
                dictACRValues = HttpContext.Session.Get<Dictionary<string, string>>(sessionKey);
            }
            else
            {
                var existingAuthContexts = 
                    await _authContextClassReferencesOperations.ListAuthenticationContextClassReferencesAsync();

                // If this tenant already has auth context available, we use those instead.
                if (existingAuthContexts.Count > 0)
                {
                    dictACRValues.Clear();

                    foreach (var authContext in existingAuthContexts)
                    {
                        dictACRValues.Add(authContext.Id, authContext.DisplayName);
                    }

                    // Save this in session
                    HttpContext.Session.Set(sessionKey, dictACRValues);
                }
            }

            return dictACRValues;
        }

        /// <summary>
        /// Retrieves the authentication context and operation mapping saved in database for the tenant.
        /// </summary>
        /// <returns></returns>
        public IActionResult ViewDetails()
        {
            List<AuthContext> authContexts = [];

            using (var commonDBContext = new CommonDBContext(_configuration))
            {
                authContexts = commonDBContext.AuthContext.Where(x => x.TenantId == TenantId).ToList();
            }

            return View(authContexts);
        }

        public ActionResult Delete(string id)
        {
            AuthContext authContext = null;
            using (var commonDBContext = new CommonDBContext(_configuration))
            {
                authContext = commonDBContext.AuthContext.FirstOrDefault(x => x.Id.ToString() == id);
            }
            return View(authContext);
        }

        /// <summary>
        /// Delete the data from database.
        /// </summary>
        /// <param name="authContext"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Delete([Bind("Id,TenantId,AuthContextId,AuthContextDisplayName,Operation")] AuthContext authContext)
        {
            using (var commonDBContext = new CommonDBContext(_configuration))
            {
                commonDBContext.AuthContext.Remove(authContext);
                commonDBContext.SaveChanges();
            }

            return RedirectToAction("ViewDetails");
        }

        /// <summary>
        /// Checks if AuthenticationContext exists.
        /// If not then create with default values.
        /// </summary>
        /// <returns></returns>
        public async Task<List<Microsoft.Graph.Models.AuthenticationContextClassReference>> CreateOrFetch()
        {
            // Call Graph to check first
            var lstPolicies = await _authContextClassReferencesOperations.ListAuthenticationContextClassReferencesAsync();

            if (lstPolicies?.Count > 0)
            {
                return lstPolicies;
            }
            else
            {
                await CreateAuthContextViaGraph();
            }
            return lstPolicies ?? [];
        }

        /// <summary>
        /// Create Authentication context for the tenant.
        /// </summary>
        /// <returns></returns>
        private async Task CreateAuthContextViaGraph()
        {
            Dictionary<string, string> dictACRValues = await GetAuthenticationContextValues();

            foreach (KeyValuePair<string, string> acr in dictACRValues)
            {
                await _authContextClassReferencesOperations.CreateAuthenticationContextClassReferenceAsync(acr.Key, acr.Value, $"A new Authentication Context Class Reference created at {DateTime.Now}", true);
            }
        }

        /// <summary>
        /// Save the Operation and Auth Context mapping in database.
        /// If an Operation is already mapped with Auth Context then updates the mapping.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="tenantId"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task SaveOrUpdateAuthContextDB(AuthContext authContext)
        {
            Dictionary<string, string> dictACRValues = await GetAuthenticationContextValues();
            authContext.AuthContextDisplayName = dictACRValues.FirstOrDefault(x => x.Key == authContext.AuthContextId).Value;

            using (var commonDBContext = new CommonDBContext(_configuration))
            {
                commonDBContext.AuthContext.Add(authContext);
                await commonDBContext.SaveChangesAsync();
            }
        }
    }
}
