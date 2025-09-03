using System.Reflection;
using Flume;
using Flume.Samples;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CA1303 // Localization not needed for console output

// Create a simple example to demonstrate Flume
var services = new ServiceCollection();

// Register Flume
// Use the assembly where your handlers are located
services.AddFlume(Assembly.GetExecutingAssembly());

var serviceProvider = services.BuildServiceProvider();
var mediator = serviceProvider.GetRequiredService<IMediator>();

// Send a request with response
var response = await mediator.Send(new Ping());
Console.WriteLine($"Ping response: {response}");

// Send a request without response
var pongResult = await mediator.Send(new Pong());
Console.WriteLine($"Pong sent, result: {pongResult}");

// Publish a notification
await mediator.Publish(new Pinged());

Console.WriteLine("Notification published");