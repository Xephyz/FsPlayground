// Try out how Jose JWT works

#r "nuget: jose-jwt"
open Jose

open System
open System.Text

let privateKeyRS256 = Jwk(
    e="AQAB",
    n="qFZv0pea_jn5Mo4qEUmStuhlulso8n1inXbEotd_zTrQp9K0RK0hf7t0K4BjKVhaiqIam4tVVQvkmYeBeYr1MmnO_0N97dMBz_7fmvyv0hgHaBdQ5mR5u3LTlHo8tjRE7-GzZmGs6jMcyj7HbXobDPQJZpqNy6JjliDVXxW8nWJDetxGBlqmTj1E1fr2RCsZLreDOPSDIedG1upz9RraShsIDzeefOcKibcAaKeeVI3rkAU8_mOauLSXv37hlk0h6sStJb3qZQXyOUkVkjXIkhvNu_ve0v7LiLT4G_OxYGzpOQcCnimKdojzNP6GtVDaMPh-QkSJE32UCos9R3wI2Q",
    p="0qaOkT174vRG3E_67gU3lgOgoT6L3pVHuu7wfrIEoxycPa5_mZVG54SgvQUofGUYEGjR0lavUAjClw9tOzcODHX8RAxkuDntAFntBxgRM-IzAy8QzeRl_cbhgVjBTAhBcxg-3VySv5GdxFyrQaIo8Oy_PPI1L4EFKZHmicBd3ts",
    q="zJPqCDKqaJH9TAGfzt6b4aNt9fpirEcdpAF1bCedFfQmUZM0LG3rMtOAIhjEXgADt5GB8ZNK3BQl8BJyMmKs57oKmbVcODERCtPqjECXXsxH-az9nzxatPvcb7imFW8OlWslwr4IIRKdEjzEYs4syQJz7k2ktqOpYI5_UfYnw1s",
    d="lJhwb0pKlB2ivyDFO6thajotClrMA3nxIiSkIUbvVr-TToFtha36gyF6w6e6YNXQXs4HhMRy1_b-nRQDk8G4_f5urd_q-pOn5u4KfmqN3Xw-lYD3ddi9qF0NLeTVUNVFASeP0FFqbPYfdNwD-LyvwjhtT_ggMOAw3mYvU5cBfz6-3uPdhl3CwQFCTgwOud_BA9p2MPMUHG82wMK_sNO1I0TYpjm7TnwNBwiKbMf-i5CKnuohgoYrEDYLeMg3f32eBljlCFNYaoCtT-mr1Ze0OTJND04vbfLotV-BBKulIpbOOSeVpKG7gJxZHmv7in7PE5_WzaxKFVoHW3wR6v_GzQ",
    dp="KTWmTGmf092AA1euOmRQ5IsfIIxQ5qGDn-FgsRh4acSOGE8L7WrTrTU4EOJyciuA0qz-50xIDbs4_j5pWx1BJVTrnhBin9vNLrVo9mtR6jmFS0ko226kOUpwEVLgtdQjobWLjtiuaMW-_Iw4gKWNptxZ6T1lBD8UWHaPiEFW2-M",
    dq="Jn0lqMkvemENEMG1eUw0c601wPOMoPD4SKTlnKWPTlQS6YISbNF5UKSuFLwoJa9HA8BifDrD-Mfpo1M1HPmnoilEWUrfwMqqdCkOlbiJQhKY8AZ16QGH50kDXhmVVa8BRWdVQWBTUzWXS5kXMaeskVzextTgymPcOAhXN-ph7MU",
    qi="sRAPigJpl8S_vsf1zhJTrHM97xRwuB26R6Tm-J8sKRPb7p5xxNlmOBBFvWmWxdto8dBElNlydSZan373yBLxzW-bZgVp-B2RKT1B3WhTYW_Vo5DLhWi84XMncJxH7avtxtF9yksaeKe0e2n3J6TTan53mDg4KF8U0OEO2ciqO9g"
)

let privateKey = Jwk(
    crv="P-256",
    x="BHId3zoDv6pDgOUh8rKdloUZ0YumRTcaVDCppUPoYgk",
    y="g3QIDhaWEksYtZ9OWjNHn9a6-i_P9o5_NrdISP0VWDU",
    d="KpTnMOHEpskXvuXHFCfiRtGUHUZ9Dq5CCcZQ-19rYs4"
)

type IdTokenPayload = {
    name: string
    given_name: string
    family_name: string
    birthdate: string
    sub_nnin: string
    sub: string
    sub_bankid: string
    originator: string
    // Fields for validation:
    exp: int
    aud: string
    iss: string
}

let payload = Map.ofList<string, obj> [
    "nbf", 1765788866
    "exp", 1765789226
    "iat", 1765788926
    "iss", "https://current.aletheia-test.idtech.no"
    "aud", "criipto-bankid-current"
    "sub", "J3wEGbnBFo3PTTVS-YuPCX7TGzPEuAbnXmN-OWNAzlQ"
    "azp", "criipto-bankid-current"
    "nonce", "JQiTadr7KuONPl-7NXq_8w"
    "originator", "C=NO,O=Stoe AS,OU=123456789,CN=BankID - Stoe Preprod - CA1 G4;OrginatorId=9980;OriginatorName=BINAS;OriginatorId=9980"
    "typ", "ID"
    "amr", [ "bis-mfa", "bis-hwk" ]
    "acr", "urn:bankid:bis"
    "sid", "je7l0Hb8QaSZn0drGpc-RoJytXxsQ22qHSmPBd1rlKY"
    "name", "John Test"
    "updated_at", 1765545226
    "given_name", "John"
    "family_name", "Test"
    "birthdate", "1955-10-19"
    "proof_key_id", "0_krtjJ9gMduPLiUAK52_zSkDy7ViERultDk7pk9DAo"
    "sub_nnin", "19105542391"
    "sub_bankid", "9578-6000-4-2429800"
]

module Decoding =
    let simple () =
        let token = Jose.JWT.Encode(payload, null, JwsAlgorithm.none)

        let decoded = Jose.JWT.Decode<{|nbf: int|}>(token)
        printfn $"Decoded unsecure token: %A{decoded}"

    let validation () =
        let encoded = Jose.JWT.Encode(payload, privateKey, JwsAlgorithm.ES256)
        // printfn $"Encoded: %s{encoded}"

        // Jose.JWT.Verify(encoded, privateKey) |> printfn "Verify: %s"
        let decoded = Jose.JWT.Decode<IdTokenPayload>(encoded, privateKey, JwsAlgorithm.ES256)
        let expiresAt = DateTimeOffset.FromUnixTimeSeconds decoded.exp
        let isExpired = expiresAt + TimeSpan.FromMinutes(int64 5) < DateTimeOffset.UtcNow
        printfn $"Validate audience: %s{decoded.aud}"
        printfn $"Validate issuer: %s{decoded.iss}"
        printfn $"Validate expiration time: expired = %b{isExpired}"

        0

    validation ()

// module JwksPart =
//     let jwksJson = """{"keys":[{"kty":"EC","alg":"ES256","kid":"w8fjwtw9UgbMuUdnji7B2qdagwH7p0MwzvZIM_iIiLk","use":"sig","crv":"P-256","x":"bmTH_ghoGdxxuFcXBTXeVi3UisyNw6X1ZZsArI2oIQQ","y":"KXzpWBUnTUGhCElyIGwd-z9A9laId-mHxuGxnIZBECE"}]}"""

//     let jwks = Jose.JwkSet.FromJson jwksJson
//     let jwkOpt =
//         jwks.Keys
//         |> Seq.tryFind (fun k -> k.KeyId = "w8fjwtw9UgbMuUdnji7B2qdagwH7p0MwzvZIM_iIiLk")

//     if jwkOpt.IsNone then exit 1
//     let jwk = jwkOpt.Value

//     let what = Jose.Jwk(
//         crv = "P-256",
//         x = "BHId3zoDv6pDgOUh8rKdloUZ0YumRTcaVDCppUPoYgk",
//         y = "g3QIDhaWEksYtZ9OWjNHn9a6-i_P9o5_NrdISP0VWDU",
//         d = "KpTnMOHEpskXvuXHFCfiRtGUHUZ9Dq5CCcZQ-19rYs4")
//     // printfn $"{what.ToJson()}"
//     let encodedJwt = Jose.JWT.Encode(payload, what, Jose.JwsAlgorithm.ES256)
//     // printfn $"encodedJwt: %s{encodedJwt}"
//     // printfn $"{what}"

module Validator =
    let validateToken (token: string) (jwks: JwkSet) kid clientId issuer validAlgs =
        let jwk =
            jwks.Keys
            |> Seq.tryFind (fun k -> k.KeyId = kid)
            |> _.Value

        let payload = Jose.JWT.Verify(token, jwk)
        // let payload =
        //     try
        //         Jose.JWT.Verify(token, jwk) |> Some
        //     with | :? Jose.JoseException -> None

        printfn $"payload"

        0
