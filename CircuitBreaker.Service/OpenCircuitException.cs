using System;

namespace CircuitBreaker.Service
{
    public class OpenCircuitException : Exception
    {
        public OpenCircuitException(string message) : base(message)
        {

        }
    }
}
