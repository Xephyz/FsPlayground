// Testing out how it would feel to make some basic types of how an api works
// And also using FSharp.UMX

#r "nuget: FSharp.UMX"

open System
open System.Text.Json
open FSharp.UMX

[<Measure>]type issuer
[<Measure>]type endpoint
[<Measure>]type clientId
[<Measure>]type clientSecret
[<Measure>]type scope
[<Measure>]type base64
[<Measure>]type base64url
[<Measure>]type basic
[<Measure>]type scopes
[<Measure>]type tokenType
[<Measure>]type seconds
[<Measure>]type idToken
[<Measure>]type signingAlg
[<Measure>]type nnin
[<Measure>]type permStmt
[<Measure>]type bearer

let toScopes (xs: string<scope> seq): string<scopes> = UMX.tag<scopes> (System.String.Join(' ', xs))
let toBase64 (s: string): string<base64> = UMX.tag<base64> (Convert.ToBase64String (Text.Encoding.UTF8.GetBytes s))
let toBase64Url (b64: string<base64>): string<base64url> =
    string b64
    |> _.Replace('+', '-')
    |> _.Replace('/', '_')
    |> _.Replace("=", "")
    |> UMX.tag<base64url>
let toBasic (b64: string<base64>): string<basic> = UMX.tag<basic> $"basic {b64}"
let toPermStmt (s: string): string<permStmt> =
    toBase64 s
    |> toBase64Url
    |> string
    |> UMX.tag<permStmt>
let toBearer (s: string<idToken>): string<bearer> = UMX.tag<bearer> $"Bearer {s}"

[<Literal>]
let bankIdOidcConfigUrl = "https://auth.bankid.no/auth/realms/prod/.well-known/openid-configuration"

[<Literal>]
let bankIdBioOidcConfigUrl = "https://app.bankid.no/.well-known/openid-configuration"

type BankIdOidcConfigResponse = {
    issuer: string<issuer>
    jwks_uri: string<endpoint>
    token_endpoint: string<endpoint>
}

type BankIdBioOidcConfigResponse = {
    issuer: string<issuer>
    jwks_uri: string<endpoint>
    token_endpoint: string<endpoint>
    id_token_signing_alg_value_supported: string<signingAlg> array
    backchannel_authentication_endpoint: string<endpoint>
    permissions_endpoint: string<endpoint>
}

type AccessTokenRequest = {
    authorization: string<basic>
    grant_type: string
    scope: string<scopes>
}

type AccessTokenResponse = {
    token_type: string<tokenType>
    expires_in: uint<seconds>
    id_token: string<idToken>
}

type LoginHint = {
    scheme: string
    value: string<nnin>
}
type UserExistsRequest = {
    loginHint: LoginHint list
}

type UserExistsResponse = {
    exists: bool
}

[<Measure>]type permType
[<Measure>]type unixTime
[<Measure>]type intent
type PermissionRequest = {
    ``type``: string<permType>
    iat: uint<unixTime>
    exp: uint<unixTime>
    permission: string<permStmt>
    intents: string<intent> list
    loginHint: LoginHint list
}

[<Measure>]type permId;
[<Measure>]type permToken;
[<Measure>]type bindingMsg;
type PermissionResponse = {
    id: string<permId>
    permissionToken: string<permToken>
    bindingMessage: string<bindingMsg>
}

type BackchannelBearerRequest = {
    bearer: string<bearer>
    login_hint_token: string<permToken>
    scope: string<scopes>
}

[<Measure>]type authReqId;
type BackchannelBearerResponse = {
    auth_req_id: string<authReqId>
    expires_in: uint<seconds>
    interval: uint<seconds>
}

type PollRequest = {
    authorization: string<bearer>
    grant_type: string
    auth_req_id: string<authReqId>
}

type PollResponse = {
    token_type: string<tokenType>
    expires_in: uint<seconds>
    id_token: string<idToken>
}

type BankIdCibaApi = {
    getOidcConfig: unit -> Async<BankIdOidcConfigResponse>
    getBioOidcConfig: unit -> Async<BankIdBioOidcConfigResponse>
    getAccessToken: AccessTokenRequest -> Async<AccessTokenResponse>
    getUserExists: UserExistsRequest -> Async<UserExistsResponse>
    requestPermission: PermissionRequest -> Async<PermissionResponse>
    initBackchannelAuth: BackchannelBearerRequest -> Async<BackchannelBearerResponse>
    poll: PollRequest -> Async<PollResponse>
}
