using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.ServiceModel.Dispatcher;

namespace WcfHelpers.Caching
{
    public class CachedOperationInvoker : IOperationInvoker
    {
        private readonly IOperationInvoker _originalInvoker;
        private readonly TimeSpan _cacheDuration;
        private readonly string _uniqueKey;

        public CachedOperationInvoker(IOperationInvoker originalInvoker, TimeSpan cacheDuration, string uniqueKey)
        {
            _originalInvoker = originalInvoker;
            _cacheDuration = cacheDuration;
            _uniqueKey = uniqueKey;
        }

        public object[] AllocateInputs()
        {
            return _originalInvoker.AllocateInputs();
        }

        public object Invoke(object instance, object[] inputs, out object[] outputs)
        {
            var key = CreateHash(inputs);
            
            var value = (CachedResult) MemoryCache.Default.Get(key);
            
            if (value == null)
            {
                var res = _originalInvoker.Invoke(instance, inputs, out outputs);
                value = new CachedResult { Result = res, Outputs = outputs };
                MemoryCache.Default.Add(key, value, new CacheItemPolicy
                {
                    AbsoluteExpiration = DateTimeOffset.Now.Add(_cacheDuration)
                });
            }

            outputs = value.Outputs;
            return value.Result;
        }

        private string CreateHash(IEnumerable<object> inputs)
        {
            var l = new List<object>(inputs.SelectMany(p => new List<object> { p, @"\;" }));
            l.Insert(0, _uniqueKey);
            var serializer = new DataContractSerializer(l.GetType());
            using (var memoryStream = new MemoryStream())
            {
                serializer.WriteObject(memoryStream, l);
                var p = new MD5CryptoServiceProvider();
                p.ComputeHash(memoryStream.ToArray());
                return Convert.ToBase64String(p.Hash);
            }
        }

        private class CachedResult
        {
            public object Result { get; set; }
            public object[] Outputs { get; set; }
        }

        public IAsyncResult InvokeBegin(object instance, object[] inputs, AsyncCallback callback, object state)
        {
            return _originalInvoker.InvokeBegin(instance, inputs, callback, state);
        }

        public object InvokeEnd(object instance, out object[] outputs, IAsyncResult result)
        {
            return _originalInvoker.InvokeEnd(instance, out outputs, result);
        }

        public bool IsSynchronous { get { return _originalInvoker.IsSynchronous; } }

        
    }
}