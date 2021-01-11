module MinecraftSync.App

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http.Features
open Microsoft.AspNetCore.HttpOverrides
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Endpoints
open Services

// ---------------------------------
// Web app
// ---------------------------------

let parsingErrorHandler err = RequestErrors.badRequest (json {| error = err |})
let parseRequest<'T> handler = tryBindForm<'T> parsingErrorHandler None handler

let webApp =
    AuthHandler >=>
    choose [
        GET >=>
            choose [
                route "/Download" >=> worldFile
            ]
        PATCH >=>
            choose [
                route "/SetState" >=> parseRequest<StateRequest> changeState
            ]
        PUT >=>
            choose [
                route "/Upload" >=> uploadFile
            ]
        setStatusCode 404 >=> text "Not Found" ]

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder : CorsPolicyBuilder) =
    builder
        .WithOrigins(
            "http://localhost:5100",
            "https://localhost:5101")
       .AllowAnyMethod()
       .AllowAnyHeader()
       |> ignore

let configureApp (app : IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IWebHostEnvironment>()
    (match env.IsDevelopment() with
    | true  ->
        app.UseDeveloperExceptionPage()
    | false ->
        app.UseGiraffeErrorHandler(errorHandler))
        .UseCors(configureCors)
        .UseForwardedHeaders()
        .UseStaticFiles()
        .UseGiraffe(webApp)

let configureServices (services : IServiceCollection) =
    services.AddCors()    |> ignore
    services.AddGiraffe() |> ignore
    services.AddSingleton<FileHandler>() |> ignore
    
    services.Configure<FormOptions>(fun (options : FormOptions) ->
        options.ValueLengthLimit <- Int32.MaxValue
        options.MultipartBodyLengthLimit <- Int64.MaxValue
    ) |> ignore
    
    services.Configure<ForwardedHeadersOptions>(fun (options : ForwardedHeadersOptions) ->
        options.ForwardedHeaders <- ForwardedHeaders.XForwardedFor ||| ForwardedHeaders.XForwardedProto
    ) |> ignore

let configureLogging (builder : ILoggingBuilder) =
    builder.AddConsole()
           .AddDebug() |> ignore

[<EntryPoint>]
let main args =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot     = Path.Combine(contentRoot, "WebRoot")
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(
            fun webHostBuilder ->
                webHostBuilder
                    .UseContentRoot(contentRoot)
                    .UseWebRoot(webRoot)
                    .UseKestrel(fun options ->
                        options.Limits.MaxRequestBodySize <- Nullable()
                    )
                    .UseUrls("http://localhost:5100")
                    .Configure(Action<IApplicationBuilder> configureApp)
                    .ConfigureServices(configureServices)
                    .ConfigureLogging(configureLogging)
                    |> ignore)
        .Build()
        .Run()
    0