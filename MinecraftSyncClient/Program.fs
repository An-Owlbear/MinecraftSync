open System
open System.IO
open System.IO.Compression
open System.Net.Http
open System.Threading.Tasks

let mutable url = ""
let mutable guid = ""
let client = new HttpClient()

// Sets the server state
let setState state token address =
    async {
        let parameters = dict["state", state; "token", token; "address", address]
        let formParameters = new FormUrlEncodedContent(parameters)
        let! response = client.PatchAsync((sprintf "%s/SetState" url), formParameters) |> Async.AwaitTask
        return response
    }
    
// Downloads the world file
let downloadFile =
    async {
        let! response = client.GetAsync((sprintf "%s/Download" url)) |> Async.AwaitTask
        do! Task.Delay(5000) |> Async.AwaitTask
        if response.IsSuccessStatusCode then
            let! responseStream = response.Content.ReadAsStreamAsync() |> Async.AwaitTask
            File.Delete("world.zip")
            let fileStream = new FileStream("world.zip", FileMode.Create)
            do! responseStream.CopyToAsync(fileStream) |> Async.AwaitTask
            fileStream.Close()
            
            if Directory.Exists("world") then
                Directory.Delete("world", true)
                
            Directory.CreateDirectory("world") |> ignore
            ZipFile.ExtractToDirectory("world.zip", "world")
        return response
    }

// Uploads the world file
let uploadFile =
    async {
        File.Delete("world.zip")
        ZipFile.CreateFromDirectory("world", "world.zip")
        let multipart = new MultipartFormDataContent()
        let! fileBytes = File.ReadAllBytesAsync("world.zip") |> Async.AwaitTask
        let content = new ByteArrayContent(fileBytes)
        multipart.Add(content, "File", "world.zip")
        
        let! response = client.PutAsync((sprintf "%s/Upload" url), multipart) |> Async.AwaitTask
        return response
    }

// Tests a response and throws an exception if required
let testResponse (response : HttpResponseMessage) =
    if not response.IsSuccessStatusCode then
        printfn "An Error occurred: %s\nPress enter to exit the program" (response.Content.ReadAsStringAsync() |> Async.AwaitTask |> Async.RunSynchronously)
        Console.ReadLine() |> ignore
        Environment.Exit(0)

[<EntryPoint>]
let main _ =
    #if DEBUG
    printfn "Press enter to start"
    Console.ReadLine() |> ignore
    #endif
    
    // Sets variables and file data
    let content = File.ReadAllLines("Config.txt")
    url <- content.[0]
    guid <- content.[1]
    if guid = "none" then
        guid <- string (Guid.NewGuid())
        File.WriteAllText("Config.txt", (sprintf "%s\n%s" url guid))
    
    client.DefaultRequestHeaders.Add("Authorization", guid)
    
    // Sends requests
    printfn "Updating server state"
    setState (string true) guid "placeholder" |> Async.RunSynchronously |> testResponse
    printfn "Downloading world file"
    downloadFile |> Async.RunSynchronously |> testResponse
    printfn "Press enter to upload world and exit the program"
    Console.ReadLine() |> ignore
    printfn "Uploading world file"
    uploadFile |> Async.RunSynchronously |> testResponse
    printfn "Updating server state"
    setState (string false) guid "placeholder" |> Async.RunSynchronously |> testResponse
    0