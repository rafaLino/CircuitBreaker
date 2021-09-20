namespace CircuitBreaker.Application
{
    class Program
    {
        static void Main(string[] args)
        {
            var breaker = new Service.CircuitBreaker();

            try
            {
                breaker.Execute(() =>
                {
                    throw new System.Exception();
                });
            }
            catch (Service.CircuitBreakerOperationException ex)
            {
                System.Diagnostics.Trace.Write(ex);
            }
            catch (Service.OpenCircuitException openEx)
            {
                System.Console.Write(breaker.IsOpen);
            }

            System.Console.Write(breaker.IsClosed);
            System.Console.Read();
        }
    }
}
