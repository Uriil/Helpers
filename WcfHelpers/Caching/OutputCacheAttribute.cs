using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace WcfHelpers.Caching
{
    [AttributeUsage(AttributeTargets.Method)]
    public class OutputCacheAttribute : Attribute, IOperationBehavior
    {
        private static readonly Random Random = new Random(Environment.TickCount);

        public int DurationInMinutes { get; set; }

        public void Validate(OperationDescription operationDescription) { }

        public virtual void ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
        {
            dispatchOperation.Invoker = new CachedOperationInvoker(dispatchOperation.Invoker, Duration, GenerateUniqueToken());
        }

        private static string GenerateUniqueToken()
        {
            var buf = new byte[Random.Next(20, 30)];
            Random.NextBytes(buf);
            return BitConverter.ToString(buf);
        }

        private TimeSpan Duration{get { return new TimeSpan(TimeSpan.TicksPerMinute*DurationInMinutes); }}

        public void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation) { }

        public void AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters) { }
    }
}
