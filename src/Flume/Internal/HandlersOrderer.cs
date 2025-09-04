using System;
using System.Collections.Generic;
using System.Linq;

namespace Flume.Internal;

internal static class HandlersOrderer
{
    public static IReadOnlyList<object> Prioritize<TRequest>(IReadOnlyList<object> handlers, TRequest request)
        where TRequest : notnull
    {
        if (handlers.Count < 2)
        {
            return handlers;
        }

        var requestObjectDetails = new ObjectDetails(request);
        var handlerObjectsDetails = handlers.Select(static s => new ObjectDetails(s)).ToList();

        var uniqueHandlers = RemoveOverridden(handlerObjectsDetails).ToArray();

        Array.Sort(uniqueHandlers, requestObjectDetails);

        return [.. uniqueHandlers.Select(static s => s.Value)];
    }

    private static IEnumerable<ObjectDetails> RemoveOverridden(List<ObjectDetails> handlersData)
    {
        for (var i = 0; i < handlersData.Count - 1; i++)
        {
            for (var j = i + 1; j < handlersData.Count; j++)
            {
                if (handlersData[i].IsOverridden || handlersData[j].IsOverridden)
                {
                    continue;
                }

                if (handlersData[i].Type.IsAssignableFrom(handlersData[j].Type))
                {
                    handlersData[i].IsOverridden = true;
                }
                else if (handlersData[j].Type.IsAssignableFrom(handlersData[i].Type))
                {
                    handlersData[j].IsOverridden = true;
                }
            }
        }

        return handlersData.Where(static w => !w.IsOverridden);
    }
}