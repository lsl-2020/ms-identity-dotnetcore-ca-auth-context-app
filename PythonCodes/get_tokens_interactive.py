# Given the client ID and tenant ID for an app registered in Azure,
# provide a <ms-entra-id> access token and a refresh token.

# If the caller is not already signed in to Azure, the caller's
# web browser will prompt the caller to sign in first.

# pip install msal
from msal import PublicClientApplication
import sys

# You can hard-code the registered app's client ID and tenant ID here,
# or you can provide them as command-line arguments to this script.
client_id = '755a4624-3e53-45e8-aaf8-60b580dd3a44'
tenant_id = '633fc03f-56d0-459c-a1b5-ab5083fc35d4'

# Do not modify this variable. It represents the programmatic ID for
# MS Graph along with the default scope of '/.default'.
scopes = ['00000003-0000-0000-c000-000000000000/.default']

# Check for too few or too many command-line arguments.
if (len(sys.argv) > 1) and (len(sys.argv) != 3):
    print("Usage: get-tokens.py <client ID> <tenant ID>")
    exit(1)

# If the registered app's client ID and tenant ID are provided as
# command-line variables, set them here.
if len(sys.argv) > 1:
    client_id = sys.argv[1]
    tenant_id = sys.argv[2]

app = PublicClientApplication(
    client_id=client_id,
    authority="https://login.microsoftonline.com/" + tenant_id,
)

acquire_tokens_result = app.acquire_token_interactive(
    # login_hint="lsl-authcontext-assigned-user00@w365testintint01.onmicrosoft.com",
    # login_hint="shileiliu@microsoft.com",
    scopes=scopes,
    port=44321,
    # prompt="login",
    # claims_challenge='''{"id_token": {"acrs": {"essential": true, "values":["c1","c2","c3","c4","c5","c6"]}}}'''
    # claims_challenge='''{"access_token": {"acrs": {"essential": true, "values":["c1","c2","c3","c4","c5","c6"]}}}'''
    claims_challenge='''{"access_token":{"xms_cc":{"values":["cp1"]},"acrs":{"essential":true,"values":["c1","c2"]}}}'''
    # claims_challenge='''{"access_token":{"xms_cc":{"values":["cp1"]},"acrs":{"values":["c1","c2"]}}}'''
    # claims_challenge='''{"access_token":{"xms_cc":{"values":["cp1"]},"acrs":{"essential":true,"values":["c1","c2","c3","c4","c5","c6"]}}}'''
    # claims_challenge='''{"id_token": {"xms_cc":{"values":["cp1"]},"acrs": {"essential": true, "values":["c1","c2","c3","c4","c5","c6"]}}}'''
    # claims_challenge='''{"access_token": {"xms_cc":{"values":["cp1"]},"acrs": {"essential": true, "values":["c1","c2","c3","c4","c5","c6"]}}}'''
    # claims_challenge='''{"id_token": {"xms_cc":{"values":["cp1"]}}}}'''
    # claims_challenge='''{"wrong_token": {"wrong_field":{"wrong_values":["whatever"]}}}'''
    # claims_challenge='''{"id_token": {"xms_cc":{"values":["cp1"]}}}'''
    # claims_challenge='''{"access_token": {"xms_cc":{"values":["cp1"]}}}'''
)

if 'error' in acquire_tokens_result:
    print("Error: " + acquire_tokens_result['error'])
    print("Description: " + acquire_tokens_result['error_description'])
else:
    print("Access token:\n")
    print(acquire_tokens_result['access_token'])
    print("\nRefresh token:\n")
    print(acquire_tokens_result['refresh_token'])