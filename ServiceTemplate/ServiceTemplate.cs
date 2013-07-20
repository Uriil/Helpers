using System;
using System.ServiceModel;

namespace ServiceTemplate
{
    public abstract class ServiceTemplate<TService, TInterface>
        where TService : System.ServiceModel.ClientBase<TInterface>
        where TInterface : class
    {
        private TService Service
        {
            get
            {
                return Activator.CreateInstance<TService>();
            }
        }

        public TResult MethodTemplate<TResult>(Func<TService, TResult> method)
        {
            var service = Service;
            try
            {
                var result = method(service);
                service.Close();
                return result;
            }
            finally
            {
                if (service.State != CommunicationState.Closed)
                    service.Abort();
            }
        }

        public void MethodTemplate(Action<TService> method)
        {
            var service = Service;
            try
            {
                method(service);
                service.Close();
            }
            finally
            {
                if (service.State != CommunicationState.Closed)
                    service.Abort();
            }
        }
    }
}
