module MinecraftSync.Endpoints

open System
open System.IO
open System.IO.Compression
open FSharp.Control.Tasks
open Giraffe
open Microsoft.AspNetCore.Http
open Services

[<CLIMutable>]
type StateRequest =
    {
        State : bool
        Token : string
        Address : string
    }
    
[<CLIMutable>]
type UploadRequest =
    {
        File : Stream
    }

// Changes the state of the server
let changeState (request : StateRequest) =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let fileHandler = ctx.GetService<FileHandler>()
            fileHandler.WriteFile (request.State.ToString()) request.Token request.Address
            return! Successful.ok (text "") next ctx
        }
        
// Uploads the world file
let uploadFile =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            File.Delete("Files/world.zip")
            let fileStream = new FileStream("Files/world.zip", FileMode.Create)
            do! ctx.Request.Form.Files.[0].CopyToAsync(fileStream)
            fileStream.Close()
            
            if Directory.Exists("Files/world") then
                ZipFile.CreateFromDirectory("Files/world", sprintf "Files/world-%s-%s.zip" (DateTime.Now.ToShortDateString().Replace("/", "")) (DateTime.Now.ToShortTimeString().Replace(":", "")))
                Directory.Delete("Files/world", true)
            ZipFile.ExtractToDirectory("Files/world.zip", "Files/world")
            
            return! Successful.ok (text "") next ctx
        }
        
// Downloads the world file
let worldFile =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            File.Delete("Files/world.zip")
            if Directory.Exists("Files/world") then
                ZipFile.CreateFromDirectory("Files/world", "Files/world.zip")
                return! Successful.ok (streamFile true "Files/world.zip" None None) next ctx
            else
                return! RequestErrors.notFound (text "World not found") next ctx
        }