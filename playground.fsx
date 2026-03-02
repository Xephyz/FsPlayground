#r "nuget: FSharp.SystemTextJson"

open System
open System.Text.Json
open System.Text.Json.Serialization

type PermissionPaymentBase = {
    paymentId: string
    // TODO: Should we validate, or let nobid validate?
    amount: string
    currency: string
    creditorName: string
}

type PermissionType =
    | Authentication = 0
    | Payment = 1
    | CardPayment = 2
    | ApproveCard = 3
    | ApproveAccount = 4
    | CustomerServiceCall = 5
    | CustomerServiceChat = 6

type PermissionRendition = {
    [<JsonConverter(typeof<JsonStringEnumConverter>)>]
    ``type``: PermissionType
    // TODO: Wrap extra data in a field or just have them all besides the type?

    id: string option
    payments: PermissionPaymentBase array option
    lastCardDigits: string option
    merchantName: string option
    accountNumber: string option
}

let jsonString = """
    {
        "type": "Payment",
        "id": "1234567890",
        "payments": [
            {
                "paymentId": "1234567890",
                "amount": "100.00",
                "currency": "USD",
                "creditorName": "John Doe"
            }
        ]
    }
    """

let deserialized = JsonSerializer.Deserialize<PermissionRendition>(jsonString)

printfn $"%A{deserialized}"
