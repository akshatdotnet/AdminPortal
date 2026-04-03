using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleUI.Problems.Multithreading
{
    /* 
     * 1 async await example
       2 Task.WhenAll example
       3 Parallel.For
       4 Thread creation
       5 Producer consumer
       6 ConcurrentDictionary example
       7 Semaphore usage
       8 Deadlock example
       9 Thread-safe counter
       10 Background worker
       11 CancellationToken example
       12 Task.Run usage
       13 Parallel API calls
       14 Async file read
       15 Async database simulation
       16 ThreadPool example
       17 Lock keyword example
       18 Monitor example
       19 Lazy initialization
       20 Performance comparison
     */

    

    public class ThreadingProblems
    {

        #region MULTITHREADING
        private int counter = 0;
        private readonly object lockObj = new();

        

        /* 1 */
        public async Task AsyncAwaitExample()
        {
            Console.WriteLine("1 Async Await Example");
            await Task.Delay(500);
            Console.WriteLine("Async Task Completed");
        }

        /* 2 */
        public async Task TaskWhenAllExample()
        {
            Console.WriteLine("2 Task.WhenAll Example");

            var t1 = Task.Delay(300);
            var t2 = Task.Delay(300);

            await Task.WhenAll(t1, t2);

            Console.WriteLine("Both Tasks Finished");
        }

        /* 3 */
        public void ParallelForExample()
        {
            Console.WriteLine("3 Parallel.For Example");

            Parallel.For(0, 5, i =>
            {
                Console.WriteLine($"Parallel {i}");
            });
        }

        /* 4 */
        public void ThreadCreation()
        {
            Console.WriteLine("4 Thread Creation");

            Thread thread = new Thread(() =>
            {
                Console.WriteLine("Thread Running");
            });

            thread.Start();
            thread.Join();
        }

        /* 5 */
        public void ProducerConsumer()
        {
            Console.WriteLine("5 Producer Consumer");

            var queue = new BlockingCollection<int>();

            Task producer = Task.Run(() =>
            {
                for (int i = 0; i < 5; i++)
                {
                    Console.WriteLine($"Produced {i}");
                    queue.Add(i);
                }

                queue.CompleteAdding();
            });

            Task consumer = Task.Run(() =>
            {
                foreach (var item in queue.GetConsumingEnumerable())
                {
                    Console.WriteLine($"Consumed {item}");
                }
            });

            Task.WaitAll(producer, consumer);
        }

        /* 6 */
        public void ConcurrentDictionaryExample()
        {
            Console.WriteLine("6 ConcurrentDictionary Example");

            var dict = new ConcurrentDictionary<int, string>();

            Parallel.For(0, 5, i =>
            {
                dict.TryAdd(i, $"Value {i}");
            });

            foreach (var item in dict)
                Console.WriteLine($"{item.Key}:{item.Value}");
        }

        /* 7 */
        public async Task SemaphoreExample()
        {
            Console.WriteLine("7 Semaphore Example");

            SemaphoreSlim semaphore = new SemaphoreSlim(2);

            var tasks = new Task[4];

            for (int i = 0; i < 4; i++)
            {
                int taskId = i;

                tasks[i] = Task.Run(async () =>
                {
                    await semaphore.WaitAsync();

                    Console.WriteLine($"Task {taskId} entered");

                    await Task.Delay(500);

                    Console.WriteLine($"Task {taskId} leaving");

                    semaphore.Release();
                });
            }

            await Task.WhenAll(tasks);
        }

        /* 8 */
        public void DeadlockExample()
        {
            Console.WriteLine("8 Deadlock Example (Simulation)");

            object lock1 = new();
            object lock2 = new();

            Task.Run(() =>
            {
                lock (lock1)
                {
                    Thread.Sleep(100);
                    lock (lock2)
                    {
                        Console.WriteLine("Thread 1 acquired both locks");
                    }
                }
            });

            Task.Run(() =>
            {
                lock (lock2)
                {
                    Thread.Sleep(100);
                    lock (lock1)
                    {
                        Console.WriteLine("Thread 2 acquired both locks");
                    }
                }
            });
        }

        /* 9 */
        public void ThreadSafeCounter()
        {
            Console.WriteLine("9 Thread Safe Counter");

            Parallel.For(0, 1000, i =>
            {
                Interlocked.Increment(ref counter);
            });

            Console.WriteLine(counter);
        }

        /* 10 */
        public void BackgroundWorkerExample()
        {
            Console.WriteLine("10 Background Worker Simulation");

            Task.Run(() =>
            {
                Console.WriteLine("Background job running...");
            });

            Thread.Sleep(300);
        }

        /* 11 */
        public async Task CancellationTokenExample()
        {
            Console.WriteLine("11 CancellationToken Example");

            var cts = new CancellationTokenSource();

            var task = Task.Run(async () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    cts.Token.ThrowIfCancellationRequested();

                    Console.WriteLine($"Processing {i}");

                    await Task.Delay(200);
                }
            }, cts.Token);

            cts.CancelAfter(600);

            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Task Cancelled Safely");
            }
        }

        /* 12 */
        public async Task TaskRunExample()
        {
            Console.WriteLine("12 Task.Run Example");

            await Task.Run(() =>
            {
                Console.WriteLine("Running background task");
            });
        }

        /* 13 */
        public async Task ParallelApiCalls()
        {
            Console.WriteLine("13 Parallel API Calls");

            var api1 = Task.Delay(300);
            var api2 = Task.Delay(400);
            var api3 = Task.Delay(500);

            await Task.WhenAll(api1, api2, api3);

            Console.WriteLine("All API calls finished");
        }

        /* 14 */
        public async Task AsyncFileRead()
        {
            Console.WriteLine("14 Async File Read");

            string path = "test.txt";

            await File.WriteAllTextAsync(path, "Hello .NET");

            string text = await File.ReadAllTextAsync(path);

            Console.WriteLine(text);
        }

        /* 15 */
        public async Task AsyncDatabaseSimulation()
        {
            Console.WriteLine("15 Async Database Simulation");

            await Task.Delay(500);

            Console.WriteLine("Database query completed");
        }

        /* 16 */
        public void ThreadPoolExample()
        {
            Console.WriteLine("16 ThreadPool Example");

            ThreadPool.QueueUserWorkItem(_ =>
            {
                Console.WriteLine("ThreadPool task executed");
            });

            Thread.Sleep(300);
        }

        /* 17 */
        public void LockExample()
        {
            Console.WriteLine("17 Lock Keyword Example");

            lock (lockObj)
            {
                Console.WriteLine("Inside critical section");
            }
        }

        /* 18 */
        public void MonitorExample()
        {
            Console.WriteLine("18 Monitor Example");

            bool lockTaken = false;

            try
            {
                Monitor.Enter(lockObj, ref lockTaken);

                Console.WriteLine("Inside Monitor Lock");
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(lockObj);
            }
        }

        /* 19 */
        public void LazyInitialization()
        {
            Console.WriteLine("19 Lazy Initialization");

            Lazy<int> lazyValue = new Lazy<int>(() =>
            {
                Console.WriteLine("Initializing value...");
                return 100;
            });

            Console.WriteLine(lazyValue.Value);
        }

        /* 20 */
        public void PerformanceComparison()
        {
            Console.WriteLine("20 Performance Comparison");

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < 1000000; i++) { }

            sw.Stop();

            Console.WriteLine($"Sequential Time: {sw.ElapsedMilliseconds} ms");
        }


        #endregion

    }

}