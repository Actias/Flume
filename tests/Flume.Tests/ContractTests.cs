using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Flume.Internal;
using Flume.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Flume.Tests;

public sealed class ContractTests
{
    [Fact]
    public async Task FirstRegisteredBehaviorIsOutermost()
    {
        var log = new StepLog();
        var services = new ServiceCollection();
        services.AddSingleton(log);
        services.AddFlume(cfg =>
        {
            cfg.TypeEvaluator = type => type == typeof(OrderHandler);
            cfg.RegisterServicesFromAssembly(typeof(ContractTests).Assembly);
            cfg.AddBehavior<OuterBehavior>();
            cfg.AddBehavior<InnerBehavior>();
        });

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new OrderRequest());

        Assert.Equal("handler", response);
        Assert.Equal(["outer-before", "inner-before", "handler", "inner-after", "outer-after"], log.Steps);
    }

    [Fact]
    public async Task VoidRequestRunsUnitPipelineBehavior()
    {
        var log = new StepLog();
        var services = new ServiceCollection();
        services.AddSingleton(log);
        services.AddFlume(cfg =>
        {
            cfg.TypeEvaluator = type => type == typeof(VoidHandler);
            cfg.RegisterServicesFromAssembly(typeof(ContractTests).Assembly);
            cfg.AddBehavior<UnitBehavior>();
        });

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new VoidRequest());

        Assert.Equal(["unit-behavior", "void-handler"], log.Steps);
        Assert.True(typeof(IRequest<Unit>).IsAssignableFrom(typeof(VoidRequest)));
    }

    [Fact]
    public async Task NotificationHandlersWithoutOrderAttributeRunInRegistrationOrder()
    {
        var log = new StepLog();
        var services = new ServiceCollection();
        services.AddSingleton(log);
        services.AddFlume(cfg =>
        {
            cfg.TypeEvaluator = _ => false;
            cfg.RegisterServicesFromAssembly(typeof(ContractTests).Assembly);
        });
        services.AddTransient<INotificationHandler<PlainNote>, FirstPlainHandler>();
        services.AddTransient<INotificationHandler<PlainNote>, SecondPlainHandler>();

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Publish(new PlainNote());

        Assert.Equal(["first", "second"], log.Steps);
    }

    [Fact]
    public async Task OrderAttributeSortsNotificationHandlersAroundRegistrationOrder()
    {
        var log = new StepLog();
        var services = new ServiceCollection();
        services.AddSingleton(log);
        services.AddFlume(cfg =>
        {
            cfg.TypeEvaluator = _ => false;
            cfg.RegisterServicesFromAssembly(typeof(ContractTests).Assembly);
        });
        services.AddTransient<INotificationHandler<OrderedNote>, UnattributedFirstHandler>();
        services.AddTransient<INotificationHandler<OrderedNote>, OrderTwoHandler>();
        services.AddTransient<INotificationHandler<OrderedNote>, OrderOneHandler>();
        services.AddTransient<INotificationHandler<OrderedNote>, UnattributedSecondHandler>();

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Publish(new OrderedNote());

        Assert.Equal(["order-1", "order-2", "unattributed-first", "unattributed-second"], log.Steps);
    }

    [Fact]
    public async Task StreamBehaviorRunsForGenericAndObjectCreateStream()
    {
        CountingStreamBehavior.Calls = 0;
        var services = new ServiceCollection();
        services.AddFlume(cfg =>
        {
            cfg.TypeEvaluator = type => type == typeof(NumberStreamHandler);
            cfg.RegisterServicesFromAssembly(typeof(ContractTests).Assembly);
            cfg.AddStreamBehavior<CountingStreamBehavior>();
        });

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var request = new NumberStream();

        var generic = new List<int>();
        await foreach (var item in mediator.CreateStream(request))
        {
            generic.Add(item);
        }

        var untyped = new List<object?>();
        await foreach (var item in mediator.CreateStream((object)request))
        {
            untyped.Add(item);
        }

        Assert.Equal([2, 3], generic);
        Assert.Equal(new object?[] { 2, 3 }, untyped);
        Assert.Equal(2, CountingStreamBehavior.Calls);
        Assert.False(typeof(IRequest).IsAssignableFrom(typeof(NumberStream)));
    }

    [Fact]
    public async Task ExceptionHandlerSetsHandledAndRunsOnce()
    {
        var counter = new CallCounter();
        var services = new ServiceCollection();
        services.AddSingleton(counter);
        services.AddFlume(cfg =>
        {
            cfg.TypeEvaluator = type => type == typeof(BoomHandler) || type == typeof(OnceExceptionHandler);
            cfg.RegisterServicesFromAssembly(typeof(ContractTests).Assembly);
        });

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new BoomRequest());

        Assert.Equal("handled", response);
        Assert.Equal(1, counter.Value);
    }

    [Fact]
    public async Task PreProcessorRunsOnce()
    {
        var counter = new CallCounter();
        var services = new ServiceCollection();
        services.AddSingleton(counter);
        services.AddFlume(cfg =>
        {
            cfg.TypeEvaluator = type => type == typeof(PingHandler);
            cfg.RegisterServicesFromAssembly(typeof(ContractTests).Assembly);
            cfg.AddRequestPreProcessor<CountingPreProcessor>();
        });

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new Ping());

        Assert.Equal("pong", response);
        Assert.Equal(1, counter.Value);
    }

    [Fact]
    public async Task ScopedHandlerAndBehaviorAreNotReusedAcrossScopes()
    {
        var services = new ServiceCollection();
        services.AddFlume(cfg =>
        {
            cfg.TypeEvaluator = _ => false;
            cfg.RegisterServicesFromAssembly(typeof(ContractTests).Assembly);
            cfg.AddBehavior<ScopedBehavior>(ServiceLifetime.Scoped);
        });
        services.AddScoped<IRequestHandler<ScopedRequest, ScopedResponse>, ScopedHandler>();

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var scope1 = provider.CreateAsyncScope();
        await using var scope2 = provider.CreateAsyncScope();
        var mediator1 = scope1.ServiceProvider.GetRequiredService<IMediator>();
        var mediator2 = scope2.ServiceProvider.GetRequiredService<IMediator>();

        var first = await mediator1.Send(new ScopedRequest());
        var firstAgain = await mediator1.Send(new ScopedRequest());
        var second = await mediator2.Send(new ScopedRequest());

        Assert.Equal(first, firstAgain);
        Assert.NotEqual(first.HandlerId, second.HandlerId);
        Assert.NotEqual(first.BehaviorId, second.BehaviorId);
        Assert.NotEqual(Guid.Empty, first.BehaviorId);
    }

    private sealed class StepLog
    {
        public List<string> Steps { get; } = [];
    }

    private sealed class CallCounter
    {
        public int Value { get; set; }
    }

    private sealed record OrderRequest : IRequest<string>;

    private sealed class OrderHandler(StepLog log) : IRequestHandler<OrderRequest, string>
    {
        public Task<string> Handle(OrderRequest request, CancellationToken cancellationToken = default)
        {
            log.Steps.Add("handler");
            return Task.FromResult("handler");
        }
    }

    private sealed class OuterBehavior(StepLog log) : IPipelineBehavior<OrderRequest, string>
    {
        public async Task<string> Handle(OrderRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            log.Steps.Add("outer-before");
            var response = await next(cancellationToken);
            log.Steps.Add("outer-after");
            return response;
        }
    }

    private sealed class InnerBehavior(StepLog log) : IPipelineBehavior<OrderRequest, string>
    {
        public async Task<string> Handle(OrderRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            log.Steps.Add("inner-before");
            var response = await next(cancellationToken);
            log.Steps.Add("inner-after");
            return response;
        }
    }

    private sealed record VoidRequest : IRequest;

    private sealed class VoidHandler(StepLog log) : IRequestHandler<VoidRequest>
    {
        public Task Handle(VoidRequest request, CancellationToken cancellationToken = default)
        {
            log.Steps.Add("void-handler");
            return Task.CompletedTask;
        }
    }

    private sealed class UnitBehavior(StepLog log) : IPipelineBehavior<VoidRequest, Unit>
    {
        public async Task<Unit> Handle(VoidRequest request, RequestHandlerDelegate<Unit> next, CancellationToken cancellationToken)
        {
            log.Steps.Add("unit-behavior");
            return await next(cancellationToken);
        }
    }

    private sealed record PlainNote : INotification;

    private sealed class FirstPlainHandler(StepLog log) : INotificationHandler<PlainNote>
    {
        public Task Handle(PlainNote notification, CancellationToken cancellationToken = default)
        {
            log.Steps.Add("first");
            return Task.CompletedTask;
        }
    }

    private sealed class SecondPlainHandler(StepLog log) : INotificationHandler<PlainNote>
    {
        public Task Handle(PlainNote notification, CancellationToken cancellationToken = default)
        {
            log.Steps.Add("second");
            return Task.CompletedTask;
        }
    }

    private sealed record OrderedNote : INotification;

    private sealed class UnattributedFirstHandler(StepLog log) : INotificationHandler<OrderedNote>
    {
        public Task Handle(OrderedNote notification, CancellationToken cancellationToken = default)
        {
            log.Steps.Add("unattributed-first");
            return Task.CompletedTask;
        }
    }

    [Order(2)]
    private sealed class OrderTwoHandler(StepLog log) : INotificationHandler<OrderedNote>
    {
        public Task Handle(OrderedNote notification, CancellationToken cancellationToken = default)
        {
            log.Steps.Add("order-2");
            return Task.CompletedTask;
        }
    }

    [Order(1)]
    private sealed class OrderOneHandler(StepLog log) : INotificationHandler<OrderedNote>
    {
        public Task Handle(OrderedNote notification, CancellationToken cancellationToken = default)
        {
            log.Steps.Add("order-1");
            return Task.CompletedTask;
        }
    }

    private sealed class UnattributedSecondHandler(StepLog log) : INotificationHandler<OrderedNote>
    {
        public Task Handle(OrderedNote notification, CancellationToken cancellationToken = default)
        {
            log.Steps.Add("unattributed-second");
            return Task.CompletedTask;
        }
    }

    private sealed record NumberStream : IStreamRequest<int>;

    private sealed class NumberStreamHandler : IStreamRequestHandler<NumberStream, int>
    {
        public async IAsyncEnumerable<int> Handle(
            NumberStream request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return 1;
            yield return 2;
            await Task.CompletedTask;
        }
    }

    private sealed class CountingStreamBehavior : IStreamPipelineBehavior<NumberStream, int>
    {
        public static int Calls { get; set; }

        public async IAsyncEnumerable<int> Handle(
            NumberStream request,
            StreamHandlerDelegate<int> next,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Calls++;
            await foreach (var item in next(cancellationToken))
            {
                yield return item + 1;
            }
        }
    }

    private sealed record BoomRequest : IRequest<string>;

    private sealed class BoomHandler : IRequestHandler<BoomRequest, string>
    {
        public Task<string> Handle(BoomRequest request, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("boom");
    }

    private sealed class OnceExceptionHandler(CallCounter counter) : IRequestExceptionHandler<BoomRequest, string, InvalidOperationException>
    {
        public Task Handle(
            BoomRequest request,
            InvalidOperationException exception,
            RequestExceptionHandlerState<string> state,
            CancellationToken cancellationToken)
        {
            counter.Value++;
            state.SetHandled("handled");
            return Task.CompletedTask;
        }
    }

    private sealed record Ping : IRequest<string>;

    private sealed class PingHandler : IRequestHandler<Ping, string>
    {
        public Task<string> Handle(Ping request, CancellationToken cancellationToken = default) =>
            Task.FromResult("pong");
    }

    private sealed class CountingPreProcessor(CallCounter counter) : IRequestPreProcessor<Ping>
    {
        public Task Process(Ping request, CancellationToken cancellationToken)
        {
            counter.Value++;
            return Task.CompletedTask;
        }
    }

    private sealed record ScopedRequest : IRequest<ScopedResponse>;

    private sealed record ScopedResponse(Guid HandlerId, Guid BehaviorId);

    private sealed class ScopedHandler : IRequestHandler<ScopedRequest, ScopedResponse>
    {
        public Guid Id { get; } = Guid.NewGuid();

        public Task<ScopedResponse> Handle(ScopedRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ScopedResponse(Id, Guid.Empty));
    }

    private sealed class ScopedBehavior : IPipelineBehavior<ScopedRequest, ScopedResponse>
    {
        public Guid Id { get; } = Guid.NewGuid();

        public async Task<ScopedResponse> Handle(
            ScopedRequest request,
            RequestHandlerDelegate<ScopedResponse> next,
            CancellationToken cancellationToken)
        {
            var response = await next(cancellationToken);
            return response with { BehaviorId = Id };
        }
    }
}