using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Web;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using AuthenticationContextClassReference = Microsoft.Graph.Models.AuthenticationContextClassReference;

namespace TodoListClient.Common;

/// <summary>
/// Uses the Graph SDK to read and write authentication context via MS Graph
/// </summary>
public class AuthenticationContextClassReferencesOperations
{
    private readonly GraphServiceClient _graphServiceClient;
    private readonly MicrosoftIdentityConsentAndConditionalAccessHandler _consentHandler;

    public AuthenticationContextClassReferencesOperations(GraphServiceClient graphServiceClient, MicrosoftIdentityConsentAndConditionalAccessHandler consentHandler)
    {
        _graphServiceClient = graphServiceClient;
        _consentHandler = consentHandler;
    }

    public async Task<List<AuthenticationContextClassReference>> ListAuthenticationContextClassReferencesAsync()
    {
        List<AuthenticationContextClassReference> allAuthenticationContextClassReferences = [];

        try
        {
            var authenticationContextClassreferences =
                await _graphServiceClient.Identity.ConditionalAccess.AuthenticationContextClassReferences.GetAsync();

            if (authenticationContextClassreferences != null)
            {
                allAuthenticationContextClassReferences = await ProcessIAuthenticationContextClassReferenceRootPoliciesCollectionPage(authenticationContextClassreferences);
            }
        }
        catch (ServiceException e)
        {
            Console.WriteLine($"We could not retrieve the existing ACRs: {e}");
            if (e.InnerException != null)
            {
                var exp = (MicrosoftIdentityWebChallengeUserException)e.InnerException;
                throw exp;
            }
            throw;
        }

        return allAuthenticationContextClassReferences;
    }

    public async Task<AuthenticationContextClassReference> GetAuthenticationContextClassReferenceByIdAsync(string ACRId)
    {
        try
        {
            AuthenticationContextClassReference ACRObject = await _graphServiceClient.Identity.ConditionalAccess.AuthenticationContextClassReferences[ACRId].GetAsync();

            return ACRObject;
        }
        catch (ServiceException gex)
        {
            if (gex.ResponseStatusCode != (int)System.Net.HttpStatusCode.NotFound)
            {
                throw;
            }
        }
        return null;
    }

    public async Task<AuthenticationContextClassReference> CreateAuthenticationContextClassReferenceAsync(string id, string displayName, string description, bool IsAvailable)
    {
        AuthenticationContextClassReference newACRObject = null;

        try
        {
            newACRObject = await _graphServiceClient.Identity.ConditionalAccess.AuthenticationContextClassReferences.PostAsync(new AuthenticationContextClassReference
            {
                Id = id,
                DisplayName = displayName,
                Description = description,
                IsAvailable = IsAvailable
            });
        }
        catch (ServiceException e)
        {
            Console.WriteLine("We could not add a new ACR: " + e.Message);
            return null;
        }

        return newACRObject;
    }

    public async Task<AuthenticationContextClassReference> UpdateAuthenticationContextClassReferenceAsync(string ACRId, bool IsAvailable, string displayName = null, string description = null)
    {
        AuthenticationContextClassReference ACRObjectToUpdate = await GetAuthenticationContextClassReferenceByIdAsync(ACRId);

        if (ACRObjectToUpdate == null)
        {
            throw new ArgumentNullException(nameof(ACRId), $"No ACR matching '{ACRId}' exists");
        }

        try
        {
            ACRObjectToUpdate = await _graphServiceClient.Identity.ConditionalAccess.AuthenticationContextClassReferences[ACRId].PatchAsync(new AuthenticationContextClassReference
            {
                Id = ACRId,
                DisplayName = displayName ?? ACRObjectToUpdate.DisplayName,
                Description = description ?? ACRObjectToUpdate.Description,
                IsAvailable = IsAvailable
            });
        }
        catch (ServiceException e)
        {
            Console.WriteLine("We could not update the ACR: " + e.Message);
            return null;
        }

        return ACRObjectToUpdate;
    }

    public async Task DeleteAuthenticationContextClassReferenceAsync(string ACRId)
    {
        try
        {
            await _graphServiceClient.Identity.ConditionalAccess.AuthenticationContextClassReferences[ACRId].DeleteAsync();
        }
        catch (ServiceException e)
        {
            Console.WriteLine($"We could not delete the ACR with Id-{ACRId}: {e}");
        }
    }

    private async Task<List<AuthenticationContextClassReference>> ProcessIAuthenticationContextClassReferenceRootPoliciesCollectionPage(
        AuthenticationContextClassReferenceCollectionResponse authenticationContextClassreferencesCollectionResponse)
    {
        List<AuthenticationContextClassReference> allAuthenticationContextClassReferences = new List<AuthenticationContextClassReference>();

        try
        {
            if (authenticationContextClassreferencesCollectionResponse != null)
            {
                // create a page iterator to iterate over the collection and add all authenticationContextClassreferences to the allAuthenticationContextClassReferences.
                var pageIterator = PageIterator<AuthenticationContextClassReference, AuthenticationContextClassReferenceCollectionResponse>.CreatePageIterator(
                    _graphServiceClient, authenticationContextClassreferencesCollectionResponse, (authenticationContextClassreference) =>
                {
                    Console.WriteLine(PrintAuthenticationContextClassReference(authenticationContextClassreference));
                    allAuthenticationContextClassReferences.Add(authenticationContextClassreference);
                    return true;
                });

                await pageIterator.IterateAsync();

                while (pageIterator.State != PagingState.Complete)
                {
                    await pageIterator.ResumeAsync();
                }
            }
        }
        catch (ServiceException e)
        {
            Console.WriteLine($"We could not process the authentication context class references list: {e}");
            return null;
        }

        return allAuthenticationContextClassReferences;
    }

    public async Task<string> PrintAuthenticationContextClassReference(AuthenticationContextClassReference authenticationContextClassReference, bool verbose = false)
    {
        string toPrint = string.Empty;
        StringBuilder more = new();

        if (authenticationContextClassReference != null)
        {
            toPrint = $"DisplayName-{authenticationContextClassReference.DisplayName}, IsAvailable-{authenticationContextClassReference.IsAvailable}, Id- '{authenticationContextClassReference.Id}'";

            if (verbose)
            {
                more.AppendLine($", Description-'{authenticationContextClassReference.Description}'");
            }
        }
        else
        {
            Console.WriteLine("The provided authenticationContextClassReference is null!");
        }

        return await Task.FromResult(toPrint + more.ToString());
    }
}