module MinecraftSync.Services

open System
open System.IO
open FSharp.Control.Tasks
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text (sprintf "%s\n%s" ex.Message ex.StackTrace)
    
// Handles reading and writing to the config file
type FileHandler() =
    let mutable contents = File.ReadAllLines("Files/Config.txt")
    member this.State =
        match contents.[0].ToLower() with
        | "true" -> true
        | _ -> false
    member this.Token = contents.[1]
    member this.Address = contents.[2]
    
    member this.WriteFile state token address =
        let content = sprintf "%s\n%s\n%s" state token address
        File.WriteAllText("Files/Config.txt", content)
        contents <- [|state; token; address|]
    
// Ensures requests have the correct token when server is in use
let AuthHandler : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let fileHandler = ctx.GetService<FileHandler>()
            let authHeader = ctx.TryGetRequestHeader "Authorization"
            if fileHandler.State = false then
                return! next ctx
            else
                return!
                    match authHeader with
                    | Some x when x = fileHandler.Token -> next ctx
                    | _ -> RequestErrors.forbidden (text "Authentication error") next ctx
        }