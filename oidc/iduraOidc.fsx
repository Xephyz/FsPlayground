// OIDC Authorization Code + PKCE against Idura (Idura) as broker.
// Signed + encrypted authorize request (JAR via PAR). Encrypted id_token.
// Client auth: private_key_jwt at token endpoint.
//
// Env vars:
//   IDURA_DOMAIN          e.g. "yourtenant-test.idura.id"
//   IDURA_CLIENT_ID
//   IDURA_REDIRECT_URI    default "http://localhost:8765/callback"
//   IDURA_SCOPES          default "openid"
//   IDURA_ACR_VALUES      default "urn:grn:authn:fi:nordea"
//   IDURA_JWKS_PATH       default "./jwks.json"  (private JWKS containing use=sig + use=enc keys)
//
// Run:  dotnet fsi iduraOidc.fsx

#r "nuget: FsHttp"
#r "nuget: FSharp.SystemTextJson, 1.3.13"
#r "nuget: jose-jwt, 5.0.0"
#r "nuget: DotNetEnv"

open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Net
open System.Security.Cryptography
open System.Text
open System.Text.Json
open System.Text.Json.Serialization

open DotNetEnv
open FsHttp
open Jose

// ---------------------------------------------------------------- config

DotNetEnv.Env.TraversePath().Load() |> ignore

let env name fallback =
    match Environment.GetEnvironmentVariable name with
    | null | "" -> fallback
    | v -> v

let domain      = env "IDURA_DOMAIN"        ""
let clientId    = env "IDURA_CLIENT_ID"     ""
let redirectUri = env "IDURA_REDIRECT_URI"  "http://localhost:8765/callback"
let scopes      = env "IDURA_SCOPES"        "openid"
let acrValues   = env "IDURA_ACR_VALUES"    "urn:grn:authn:fi:nordea"
let jwksPath    = env "IDURA_JWKS_PATH"     "./jwks.json"

if String.IsNullOrEmpty domain || String.IsNullOrEmpty clientId then
    eprintfn "IDURA_DOMAIN and IDURA_CLIENT_ID must be set."
    exit 1

let discoveryUrl = $"https://{domain}/.well-known/openid-configuration"

// ---------------------------------------------------------------- json

let deserialize<'T> (s: string) : 'T = JsonSerializer.Deserialize<'T> s

// ---------------------------------------------------------------- jwk helpers

let parseJwks (json: string) : JwkSet = JwkSet.FromJson json

let pickByUse (jwks: JwkSet) (useType: string) : Jwk =
    jwks.Keys
    |> Seq.tryFind (fun k -> k.Use = useType)
    |> Option.defaultWith (fun () -> failwithf "no jwk with use=%s" useType)

let pickByKid (jwks: JwkSet) (kid: string) : Jwk =
    jwks.Keys
    |> Seq.tryFind (fun k -> k.KeyId = kid)
    |> Option.defaultWith (fun () -> failwithf "no jwk for kid %s" kid)

let myJwks = parseJwks (File.ReadAllText jwksPath)
let sigJwk = pickByUse myJwks "sig"
let encJwk = pickByUse myJwks "enc"
printfn $"Loaded sig kid=%s{sigJwk.KeyId}, enc kid=%s{encJwk.KeyId}"

// ---------------------------------------------------------------- pkce / state / nonce

let b64url (bytes: byte[]) =
    Convert.ToBase64String(bytes)
        .Replace('+', '-').Replace('/', '_').TrimEnd('=')

let randomB64url (nBytes: int) =
    let buf = Array.zeroCreate<byte> nBytes
    RandomNumberGenerator.Fill(buf.AsSpan())
    b64url buf

let codeVerifier = randomB64url 32
let codeChallenge =
    use sha = SHA256.Create()
    sha.ComputeHash(Encoding.ASCII.GetBytes codeVerifier) |> b64url
let state = randomB64url 16
let nonce = randomB64url 16

// ---------------------------------------------------------------- discovery

type Discovery = {
    issuer: string
    authorization_endpoint: string
    token_endpoint: string
    jwks_uri: string
    pushed_authorization_request_endpoint: string option
    id_token_signing_alg_values_supported: string[]
}

printfn "Fetching discovery: %s" discoveryUrl
let disco =
    get discoveryUrl
    |> Request.send
    |> Response.toText
    |> deserialize<Discovery>

printfn "Issuer: %s" disco.issuer

let iduraJwks =
    http { GET disco.jwks_uri } |> Request.send |> Response.toText |> parseJwks

// ---------------------------------------------------------------- request object (JAR) → JWE

let nowUnix () = DateTimeOffset.UtcNow.ToUnixTimeSeconds()

let requestObjectClaims =
    let d = Dictionary<string, obj>()
    d.["iss"]                   <- clientId
    d.["aud"]                   <- disco.issuer
    d.["response_type"]         <- "code"
    d.["client_id"]             <- clientId
    d.["redirect_uri"]          <- redirectUri
    d.["scope"]                 <- scopes
    d.["state"]                 <- state
    d.["nonce"]                 <- nonce
    d.["acr_values"]            <- acrValues
    d.["code_challenge"]        <- codeChallenge
    d.["code_challenge_method"] <- "S256"
    d.["iat"]                   <- nowUnix ()
    d.["exp"]                   <- nowUnix () + 300L
    d.["jti"]                   <- randomB64url 16
    d

let signedRequestObject =
    let extra = Dictionary<string, obj>()
    extra.["kid"] <- sigJwk.KeyId
    extra.["typ"] <- "oauth-authz-req+jwt"
    JWT.Encode(requestObjectClaims, sigJwk, JwsAlgorithm.RS256, extraHeaders = extra)

// ---------------------------------------------------------------- PAR or fallback request= param

let parEndpoint = disco.pushed_authorization_request_endpoint

type ParResponse = { request_uri: string; expires_in: int }

let authUrl =
    match parEndpoint with
    | Some par ->
        printfn "PAR endpoint: %s" par
        let parResp =
            http {
                POST par
                body
                formUrlEncoded [
                    "client_id", clientId
                    "request",   signedRequestObject
                ]
            }
            |> Request.send
            |> Response.toText
            |> deserialize<ParResponse>
        printfn $"Got request_uri (expires in %d{parResp.expires_in}s)"
        $"{disco.authorization_endpoint}?client_id={Uri.EscapeDataString clientId}&request_uri={Uri.EscapeDataString parResp.request_uri}"
    | None ->
        printfn "No PAR endpoint — using ?request= on /authorize"
        $"{disco.authorization_endpoint}?client_id={Uri.EscapeDataString clientId}&request={Uri.EscapeDataString signedRequestObject}"

// ---------------------------------------------------------------- callback listener

let prefix =
    let u = Uri redirectUri
    $"http://{u.Host}:{u.Port}{u.AbsolutePath.TrimEnd '/'}/"

let listener = new HttpListener()
listener.Prefixes.Add prefix
listener.Start()
printfn $"Listening on %s{prefix}"

let openBrowser (url: string) =
    let psi = ProcessStartInfo(url, UseShellExecute = true)
    Process.Start psi |> ignore

printfn $"Opening browser to authorize (acr_values=%s{acrValues}) ..."
openBrowser authUrl

let ctx = listener.GetContext()
let query = System.Web.HttpUtility.ParseQueryString ctx.Request.Url.Query

let returnedState = query.["state"]
let code          = query.["code"]
let errorParam    = query.["error"]

let html (s: string) =
    let bytes = Encoding.UTF8.GetBytes s
    ctx.Response.ContentType <- "text/html; charset=utf-8"
    ctx.Response.OutputStream.Write(bytes, 0, bytes.Length)
    ctx.Response.OutputStream.Close()

if not (isNull errorParam) then
    html $"""<h1>Auth error</h1><p>{errorParam}: {query["error_description"]}</p>"""
    listener.Stop()
    eprintfn "Authorize error: %s — %s" errorParam query["error_description"]
    exit 2

if returnedState <> state then
    html "<h1>State mismatch</h1>"
    listener.Stop()
    eprintfn $"State mismatch. expected=%s{state} got=%s{returnedState}"
    exit 3

html "<h1>OK</h1><p>You can close this tab.</p>"
listener.Stop()
printfn "Got authorization code."

// ---------------------------------------------------------------- token exchange (private_key_jwt)

let clientAssertion =
    let claims = Dictionary<string, obj>()
    claims.["iss"] <- clientId
    claims.["sub"] <- clientId
    claims.["aud"] <- disco.token_endpoint
    claims.["jti"] <- randomB64url 16
    claims.["iat"] <- nowUnix ()
    claims.["exp"] <- nowUnix () + 120L
    let extra = Dictionary<string, obj>()
    extra.["kid"] <- sigJwk.KeyId
    extra.["typ"] <- "JWT"
    JWT.Encode(claims, sigJwk, JwsAlgorithm.RS256, extraHeaders = extra)

type TokenResponse = {
    access_token:  string
    token_type:    string
    expires_in:    JsonElement
    id_token:      string
    refresh_token: string option
    scope:         string option
}

let tokenResp =
    http {
        POST disco.token_endpoint
        body
        formUrlEncoded [
            "grant_type",            "authorization_code"
            "code",                  code
            "redirect_uri",          redirectUri
            "code_verifier",         codeVerifier
            "client_id",             clientId
            "client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"
            "client_assertion",      clientAssertion
        ]
    }
    |> Request.send
    |> Response.toText
    |> deserialize<TokenResponse>

printfn $"Got id_token (%d{tokenResp.id_token.Length} chars)."

// ---------------------------------------------------------------- id_token: decrypt JWE → verify JWS

type JwtHeader = { alg: string; kid: string; typ: string option; cty: string option; enc: string option }

let b64urlDecodeToString (s: string) =
    let padded = s + String.replicate ((4 - s.Length % 4) % 4) "="
    padded.Replace('-', '+').Replace('_', '/')
    |> Convert.FromBase64String
    |> Encoding.UTF8.GetString

let outerHeader =
    tokenResp.id_token.Split('.').[0]
    |> b64urlDecodeToString
    |> deserialize<JwtHeader>
printfn $"id_token outer header alg=%s{outerHeader.alg} enc=%A{outerHeader.enc} kid=%s{outerHeader.kid}"

// JWE → inner JWS (string)
let innerJws = JWT.Decode(tokenResp.id_token, encJwk)

let innerHeader =
    innerJws.Split('.').[0]
    |> b64urlDecodeToString
    |> deserialize<JwtHeader>
printfn $"id_token inner header alg=%s{innerHeader.alg} kid=%s{innerHeader.kid}"

let iduraSigJwk = pickByKid iduraJwks innerHeader.kid
let payloadJson = JWT.Decode(innerJws, iduraSigJwk)

type IdTokenClaims = {
    iss:   string
    sub:   string
    aud:   JsonElement
    exp:   int64
    iat:   int64
    nonce: string option
    acr:   string option
    amr:   string[] option
    name:        string option
    given_name:  string option
    family_name: string option
    birthdate:   string option
    [<JsonPropertyName "ssn">]
    ssn:         string option
    [<JsonPropertyName "identityscheme">]
    identityScheme: string option
}

let claims = deserialize<IdTokenClaims> payloadJson

let audOk =
    match claims.aud.ValueKind with
    | JsonValueKind.String -> claims.aud.GetString() = clientId
    | JsonValueKind.Array ->
        claims.aud.EnumerateArray() |> Seq.exists (fun e -> e.GetString() = clientId)
    | _ -> false

let now = nowUnix ()
let notExpired = claims.exp > now
let issuerOk = claims.iss = disco.issuer
let nonceOk = claims.nonce = Some nonce

printfn $"iss   ok: %b{issuerOk} (%s{claims.iss})"
printfn $"aud   ok: %b{audOk}"
printfn $"exp   ok: %b{notExpired} (in %d{claims.exp - now}s)"
printfn $"nonce ok: %b{nonceOk}"

if not (issuerOk && audOk && notExpired && nonceOk) then
    eprintfn "id_token validation failed."
    exit 4

printfn ""
printfn $"sub:            %s{claims.sub}"
claims.acr            |> Option.iter (printfn "acr:            %s")
claims.amr            |> Option.iter (fun a -> printfn "amr:            %s" (String.concat "," a))
claims.identityScheme |> Option.iter (printfn "identityscheme: %s")
claims.name           |> Option.iter (printfn "name:           %s")
claims.given_name     |> Option.iter (printfn "given_name:     %s")
claims.family_name    |> Option.iter (printfn "family_name:    %s")
claims.birthdate      |> Option.iter (printfn "birthdate:      %s")
claims.ssn            |> Option.iter (printfn "ssn:            %s")
