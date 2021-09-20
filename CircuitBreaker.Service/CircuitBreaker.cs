using System;
using System.Diagnostics;
using System.Timers;

namespace CircuitBreaker.Service
{
    public class CircuitBreaker : ICircuitBreaker
    {
        private object monitor = new();
        private Timer Timer { get; set; }
        private int FailureCount { get; set; }

        private Action Action { get; set; }


        public event EventHandler StateChanged;

        public int Timeout { get; private set; }
        public int Threshold { get; private set; }

        public CircuitBreakerState State { get; private set; }

        public bool IsClosed => State == CircuitBreakerState.Closed;

        public bool IsOpen => State == CircuitBreakerState.Open;


        public CircuitBreaker(int threshold = 5, int timeout = 6000)
        {
            if (threshold <= 0)
                throw new ArgumentOutOfRangeException($"{threshold} should greater than zero");

            if (timeout <= 0)
                throw new ArgumentOutOfRangeException($"{timeout} should greater than zero");

            Threshold = threshold;
            Timeout = timeout;
            State = CircuitBreakerState.Closed;
            Timer = new Timer(timeout)
            {
                Enabled = false
            };
            Timer.Elapsed += Timer_Elapsed;
        }

        public void Execute(Action action)
        {
            if (State == CircuitBreakerState.Open)
                throw new OpenCircuitException("Circuit breaker is currently open");

            lock (monitor)
            {
                try
                {
                    Action = action;
                    Action();
                }
                catch (Exception ex)
                {
                    if (State == CircuitBreakerState.HalfOpen)
                        Trip();
                    else if (FailureCount <= Threshold)
                    {
                        FailureCount++;
                        if (!Timer.Enabled)
                            Timer.Enabled = true;
                    }
                    else if (FailureCount >= Threshold)
                        Trip();

                    throw new CircuitBreakerOperationException("Operation failed", ex);
                }

                if (State == CircuitBreakerState.HalfOpen)
                    Reset();

                if (FailureCount > 0)
                    FailureCount--;

            }
        }

        public void Reset()
        {
            if (State == CircuitBreakerState.Closed)
            {
                Trace.WriteLine($"Circuit closed");
                ChangeState(CircuitBreakerState.Closed);

                Timer.Stop();
            }
        }
        private void Trip()
        {
            if (State == CircuitBreakerState.Open)
            {
                Trace.WriteLine($"Circuit open");
                ChangeState(CircuitBreakerState.Open);
            }
        }

        private void ChangeState(CircuitBreakerState state)
        {
            State = state;
            OnCircuitBreakerStateChanged(new EventArgs());
        }

        private void OnCircuitBreakerStateChanged(EventArgs e) => StateChanged?.Invoke(this, e);

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (monitor)
            {
                try
                {
                    Trace.WriteLine($"Retry, Execução nº {FailureCount}");
                    Execute(Action);
                    Reset();
                }
                catch
                {
                    if (FailureCount > Threshold)
                    {
                        Trip();
                        Timer.Elapsed -= Timer_Elapsed;
                        Timer.Enabled = false;
                        Timer.Stop();
                    }
                }
            }
        }
    }
}
