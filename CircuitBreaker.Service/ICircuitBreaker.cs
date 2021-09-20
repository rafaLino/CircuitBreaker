using System;

namespace CircuitBreaker.Service
{
    public interface ICircuitBreaker
    {
        public CircuitBreakerState State { get; }
        public bool IsClosed { get; }
        public bool IsOpen { get; }

        void Reset();
        void Execute(Action action);

    }
}
